using UnityEditor;

namespace UnityMcpMin.Editor.Bridge
{
    /// <summary>
    /// Optionally starts the HTTP bridge when the Editor loads (after scripts compile).
    /// </summary>
    [InitializeOnLoad]
    internal static class ReflectBridgeAutoStart
    {
        static ReflectBridgeAutoStart()
        {
            EditorApplication.delayCall += TryAutoStartOnce;
        }

        private static void TryAutoStartOnce()
        {
            if (!ReflectBridgePreferences.AutoStart)
            {
                return;
            }

            if (ReflectBridgeHttpServer.IsListening)
            {
                return;
            }

            ReflectBridgeHttpServer.Start(ReflectBridgePreferences.Port);
        }
    }
}
