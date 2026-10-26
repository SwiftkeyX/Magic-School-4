using System.IO;
using UnityEditor;
using MagicSchool.Core.Benchmark;

namespace MagicSchool.Editor
{
    // the one BenchmarkSettings asset both benchmark menus and the Benchmark window use - made with the defaults if missing
    internal static class BenchmarkSettingsAsset
    {
        public static BenchmarkSettingsSO LoadOrCreate()
        {
            var settings = AssetDatabase.LoadAssetAtPath<BenchmarkSettingsSO>(BenchmarkSettingsSO.AssetPath);
            if (settings != null) return settings;

            if (EditorApplication.isPlayingOrWillChangePlaymode) return null;

            Directory.CreateDirectory(Path.GetDirectoryName(BenchmarkSettingsSO.AssetPath));
            settings = UnityEngine.ScriptableObject.CreateInstance<BenchmarkSettingsSO>();
            AssetDatabase.CreateAsset(settings, BenchmarkSettingsSO.AssetPath);
            AssetDatabase.SaveAssets();
            return settings;
        }
    }
}
