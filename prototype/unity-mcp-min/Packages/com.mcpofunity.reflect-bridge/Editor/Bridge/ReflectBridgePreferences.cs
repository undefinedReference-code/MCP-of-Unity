using UnityEditor;

namespace UnityMcpMin.Editor.Bridge
{
    internal static class ReflectBridgePreferences
    {
        private const string PortKey = "McpReflectBridge.Port";
        private const string AutoStartKey = "McpReflectBridge.AutoStart";

        public static int Port
        {
            get => EditorPrefs.GetInt(PortKey, 7890);
            set => EditorPrefs.SetInt(PortKey, value);
        }

        public static bool AutoStart
        {
            get => EditorPrefs.GetBool(AutoStartKey, false);
            set => EditorPrefs.SetBool(AutoStartKey, value);
        }
    }
}
