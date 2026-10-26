using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using MagicSchool.Combat.Heroes;
using MagicSchool.DevTools.Benchmark;

namespace MagicSchool.Editor
{
    // Hero Benchmark - run from the Benchmark window (Magic School 4 > Benchmark), which calls RunAll / RunSelected
    // a menu to test how strong each hero was using HeroBenchMark.cs
    //      Run Hero Benchmark              - test every hero
    //      Run Hero Benchmark (Selected)   - test only a hero which his HeroDataSOs selected in the Project window
    // The settings asset is made in Edit mode, as Play mode cannot create an asset. A report is imported to the Google
    // Sheet only once back in Edit mode, because leaving Play mode can reload scripts and drop the callback; the
    // python process is watched from the editor loop, because Unity's Mono does not raise Process.Exited reliably. A
    // failed import only logs; the CSV is still there.
    [InitializeOnLoad]
    internal static class HeroBenchmarkMenu
    {
        private const string PendingKey = "MagicSchool.HeroBenchmark.Pending";
        private const string SelectedKey = "MagicSchool.HeroBenchmark.Selected";
        private const string DummyPath = "Assets/Data/Benchmark/BenchDummy.asset";

        static HeroBenchmarkMenu()
        {
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
        }

        internal static void RunAll() => Request(string.Empty);

        internal static void RunSelected() => Request(string.Join(";", SelectedHeroGuids()));

        internal static bool CanRunSelected() => SelectedHeroGuids().Any();

        private static IEnumerable<string> SelectedHeroGuids()
            => Selection.objects.OfType<HeroDataSO>()
                .Select(hero => AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(hero)))
                .Where(guid => !string.IsNullOrEmpty(guid));

        private static void Request(string selectedGuids)
        {
            BenchmarkSettingsAsset.LoadOrCreate();

            if (EditorApplication.isPlaying)
            {
                Begin(selectedGuids);
                return;
            }

            SessionState.SetBool(PendingKey, true);
            SessionState.SetString(SelectedKey, selectedGuids);
            EditorApplication.EnterPlaymode();
        }

        private static void OnPlayModeChanged(PlayModeStateChange change)
        {
            if (change == PlayModeStateChange.EnteredEditMode) ImportPendingReport();

            if (change != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(PendingKey, false)) return;

            string selected = SessionState.GetString(SelectedKey, string.Empty);
            SessionState.EraseBool(PendingKey);
            SessionState.EraseString(SelectedKey);
            Begin(selected);
        }

        private static void Begin(string selectedGuids)
        {
            var dummy = AssetDatabase.LoadAssetAtPath<HeroDataSO>(DummyPath);
            if (dummy == null)
            {
                Debug.LogError($"[HeroBenchmark] no dummy at {DummyPath}.");
                return;
            }

            List<HeroDataSO> only = string.IsNullOrEmpty(selectedGuids)
                ? null
                : selectedGuids.Split(';')
                    .Select(guid => AssetDatabase.LoadAssetAtPath<HeroDataSO>(AssetDatabase.GUIDToAssetPath(guid)))
                    .Where(hero => hero != null)
                    .ToList();

            BenchmarkSettingsSO settings = BenchmarkSettingsAsset.LoadOrCreate();
            BenchmarkSettingsSO s = settings != null ? settings : BenchmarkSettingsSO.Current;
            string who = only == null ? "every shop hero" : string.Join(", ", only.Select(h => h.Name));
            Debug.Log($"[HeroBenchmark] started - {who}: a {s.longSeconds:0}s damage test, a {s.longSeconds:0}s AoE test " +
                      $"(6 dummies) and a tank test each - alone, then with up to {s.alliesSmallTeam} and {s.alliesBigTeam} " +
                      "of its mark hexes filled. The screen stays blank until it is done.");

            HeroBenchmark.Run(dummy, only, settings, path =>
            {
                ImportWhenBackInEditMode(path);
                EditorApplication.ExitPlaymode();
                EditorUtility.RevealInFinder(path);
            });
        }

        private const string ImportScript = ".claude/scripts/benchmark_import.py";
        private const string ImportKey = "MagicSchool.HeroBenchmark.ImportPath";

        internal static void ImportWhenBackInEditMode(string csvPath) => SessionState.SetString(ImportKey, csvPath);

        private static void ImportPendingReport()
        {
            string path = SessionState.GetString(ImportKey, string.Empty);
            if (string.IsNullOrEmpty(path)) return;

            SessionState.EraseString(ImportKey);
            ImportToSheet(path);
        }

        private static void ImportToSheet(string csvPath)
        {
            var process = new System.Diagnostics.Process
            {
                StartInfo = new System.Diagnostics.ProcessStartInfo("python", $"\"{ImportScript}\" \"{csvPath}\"")
                {
                    WorkingDirectory = System.IO.Directory.GetCurrentDirectory(),
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                },
            };

            try { process.Start(); }
            catch (System.Exception e)
            {
                Debug.LogError($"[HeroBenchmark] could not run python for the sheet import: {e.Message}");
                return;
            }

            EditorApplication.CallbackFunction watch = null;
            watch = () =>
            {
                if (!process.HasExited) return;
                EditorApplication.update -= watch;

                string output = (process.StandardOutput.ReadToEnd() + process.StandardError.ReadToEnd()).Trim();
                if (process.ExitCode == 0) Debug.Log($"[HeroBenchmark] {output}");
                else Debug.LogError($"[HeroBenchmark] sheet import failed (exit {process.ExitCode}): {output}");
                process.Dispose();
            };
            EditorApplication.update += watch;
        }
    }
}
