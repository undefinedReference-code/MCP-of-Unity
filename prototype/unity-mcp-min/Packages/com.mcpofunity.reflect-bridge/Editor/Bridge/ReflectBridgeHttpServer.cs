using System;
using System.IO;
using System.Net;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using UnityMcpMin.Editor.Tools;

namespace UnityMcpMin.Editor.Bridge
{
    /// <summary>
    /// Minimal localhost HTTP listener for MCP Python bridge: POST /unity_reflect_call, GET /health.
    /// </summary>
    internal static class ReflectBridgeHttpServer
    {
        private static HttpListener Listener;
        private static bool WantRunning;

        public static bool IsListening => Listener != null && Listener.IsListening;

        public static int ListeningPort { get; private set; }

        public static void Start(int port)
        {
            Stop();

            if (port < 1 || port > 65535)
            {
                Debug.LogError($"Reflect Bridge: invalid port {port}");
                return;
            }

            WantRunning = true;
            ListeningPort = port;

            Listener = new HttpListener();
            Listener.Prefixes.Add($"http://127.0.0.1:{port}/");

            try
            {
                Listener.Start();
            }
            catch (Exception ex)
            {
                Debug.LogError($"Reflect Bridge: failed to listen on {port}: {ex.Message}");
                Listener = null;
                WantRunning = false;
                return;
            }

            Listener.BeginGetContext(OnBeginGetContext, null);
            Debug.Log($"[MCP Reflect Bridge] Listening on http://127.0.0.1:{port}/ (POST /unity_reflect_call)");
        }

        public static void Stop()
        {
            WantRunning = false;
            if (Listener == null)
            {
                return;
            }

            try
            {
                Listener.Stop();
                Listener.Close();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Reflect Bridge stop: {ex.Message}");
            }
            finally
            {
                Listener = null;
            }
        }

        private static void OnBeginGetContext(IAsyncResult ar)
        {
            if (!WantRunning || Listener == null || !Listener.IsListening)
            {
                return;
            }

            HttpListenerContext context;
            try
            {
                context = Listener.EndGetContext(ar);
            }
            catch (ObjectDisposedException)
            {
                return;
            }
            catch (HttpListenerException)
            {
                return;
            }

            try
            {
                Listener.BeginGetContext(OnBeginGetContext, null);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Reflect Bridge re-arm: {ex.Message}");
                return;
            }

            try
            {
                HandleContext(context);
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                TryCloseWithError(context.Response, 500, ex.Message);
            }
        }

        private static void HandleContext(HttpListenerContext context)
        {
            var request = context.Request;
            var response = context.Response;

            response.Headers["Access-Control-Allow-Origin"] = "*";

            if (string.Equals(request.HttpMethod, "OPTIONS", StringComparison.OrdinalIgnoreCase))
            {
                response.Headers["Access-Control-Allow-Methods"] = "GET, POST, OPTIONS";
                response.Headers["Access-Control-Allow-Headers"] = "Content-Type";
                response.StatusCode = 204;
                response.Close();
                return;
            }

            var path = request.Url.AbsolutePath.TrimEnd('/');

            if (string.Equals(request.HttpMethod, "GET", StringComparison.OrdinalIgnoreCase) &&
                string.Equals(path, "/health", StringComparison.OrdinalIgnoreCase))
            {
                WriteJson(response, 200, new { ok = true, listening = IsListening, port = ListeningPort });
                return;
            }

            if (string.Equals(request.HttpMethod, "POST", StringComparison.OrdinalIgnoreCase) &&
                string.Equals(path, "/unity_reflect_call", StringComparison.OrdinalIgnoreCase))
            {
                string body;
                using (var reader = new StreamReader(request.InputStream, request.ContentEncoding))
                {
                    body = reader.ReadToEnd();
                }

                object result;
                try
                {
                    var jo = string.IsNullOrWhiteSpace(body)
                        ? null
                        : JObject.Parse(body);

                    result = EditorMainThreadDispatcher.Invoke(() => ReflectCallTool.HandleCommand(jo));
                }
                catch (JsonException jex)
                {
                    result = new { ok = false, message = "Invalid JSON body.", details = jex.Message };
                }
                catch (Exception ex)
                {
                    result = new { ok = false, message = "Bridge execution failed.", details = ex.Message };
                }

                WriteJson(response, 200, result);
                return;
            }

            WriteJson(response, 404, new { ok = false, message = "Not found." });
        }

        private static void TryCloseWithError(HttpListenerResponse response, int code, string message)
        {
            try
            {
                WriteJson(response, code, new { ok = false, message });
            }
            catch
            {
                try
                {
                    response.Close();
                }
                catch
                {
                    // ignored
                }
            }
        }

        private static void WriteJson(HttpListenerResponse response, int statusCode, object payload)
        {
            response.StatusCode = statusCode;
            response.ContentType = "application/json; charset=utf-8";

            var json = JsonConvert.SerializeObject(
                payload,
                new JsonSerializerSettings
                {
                    ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                    NullValueHandling = NullValueHandling.Ignore
                });

            var bytes = Encoding.UTF8.GetBytes(json);
            response.ContentLength64 = bytes.Length;
            response.OutputStream.Write(bytes, 0, bytes.Length);
            response.Close();
        }
    }
}
