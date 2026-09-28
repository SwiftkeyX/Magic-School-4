using UnityEngine;

namespace MagicSchool.Engine
{
    /// <summary>
    /// Logging, wrapped so the plain (non-MonoBehaviour) classes don't call UnityEngine directly.
    /// </summary>
    public static class DebugTool
    {
        public static void DebugLogConsole(string log) => Debug.Log(log);

        public static void LogWarning(string log) => Debug.LogWarning(log);

        public static void LogError(string log) => Debug.LogError(log);
    }
}
