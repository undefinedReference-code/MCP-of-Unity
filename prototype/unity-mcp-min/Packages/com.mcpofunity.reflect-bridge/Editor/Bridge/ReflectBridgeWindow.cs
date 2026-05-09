using UnityEditor;
using UnityEngine;

namespace UnityMcpMin.Editor.Bridge
{
    /// <summary>
    /// Configure port, auto-start, and manually start/stop the localhost bridge.
    /// </summary>
    public sealed class ReflectBridgeWindow : EditorWindow
    {
        private int _port = 7890;
        private bool _autoStart;

        [MenuItem("Window/MCP Reflect Bridge")]
        public static void Open()
        {
            GetWindow<ReflectBridgeWindow>("MCP Reflect Bridge");
        }

        private void OnEnable()
        {
            _port = ReflectBridgePreferences.Port;
            _autoStart = ReflectBridgePreferences.AutoStart;
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Unity ↔ MCP (Python) HTTP bridge", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "POST http://127.0.0.1:<port>/unity_reflect_call\nGET  http://127.0.0.1:<port>/health\n\n" +
                "Match UNITY_BRIDGE_ENDPOINT in Python (default http://127.0.0.1:7890).",
                MessageType.Info);

            EditorGUILayout.Space(6);

            _port = EditorGUILayout.IntField("Listen port", _port);
            _autoStart = EditorGUILayout.ToggleLeft("Auto-start when Unity opens", _autoStart);

            if (GUILayout.Button("Save preferences"))
            {
                ReflectBridgePreferences.Port = Mathf.Clamp(_port, 1, 65535);
                ReflectBridgePreferences.AutoStart = _autoStart;
                _port = ReflectBridgePreferences.Port;
                Debug.Log($"[MCP Reflect Bridge] Saved port {_port}, autoStart={_autoStart}");
            }

            EditorGUILayout.Space(10);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Start server"))
                {
                    ReflectBridgePreferences.Port = Mathf.Clamp(_port, 1, 65535);
                    _port = ReflectBridgePreferences.Port;
                    ReflectBridgeHttpServer.Start(_port);
                }

                if (GUILayout.Button("Stop server"))
                {
                    ReflectBridgeHttpServer.Stop();
                }
            }

            EditorGUILayout.Space(8);

            var listening = ReflectBridgeHttpServer.IsListening;
            EditorGUILayout.LabelField(
                "Status",
                listening ? $"Listening on {_port}" : "Stopped");

            if (listening)
            {
                EditorGUILayout.SelectableLabel($"curl http://127.0.0.1:{_port}/health");
            }
        }
    }
}
