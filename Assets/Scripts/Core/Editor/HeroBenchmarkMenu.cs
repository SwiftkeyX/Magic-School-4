using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using MagicSchool.Combat.Heroes;
using MagicSchool.Core.Benchmark;

namespace MagicSchool.Editor
{
    // Magic School 4 > Balance > Run Hero Benchmark
    // a menu to test how strong each hero was using HeroBenchMark.cs
    //      Run Hero Benchmark              - test every hero
    //      Run Hero Benchmark (Selected)   - test only a hero which his HeroDataSOs selected in the Project window
    [InitializeOnLoad]
    internal static class HeroBenchmarkMenu
    {
        private const string PendingKey = "MagicSchool.HeroBenchmark.Pending";
        private const string SelectedKey = "MagicSchool.HeroBenchmark.Selected";   // asset GUIDs, ';'-separated; empty = everyone
        private const string DummyPath = "Assets/Data/Benchmark/BenchDummy.asset";

        static HeroBenchmarkMenu()
        {
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
        }

        [MenuItem("Magic School 4/Balance/Run Hero Benchmark")]
        internal static void RunAll() => Request(string.Empty);

        [MenuItem("Magic School 4/Balance/Run Hero Benchmark (Selected)")]
        internal static void RunSelected() => Request(string.Join(";", SelectedHeroGuids()));

        // greyed out until at least one hero is selected
        [MenuItem("Magic School 4/Balance/Run Hero Benchmark (Selected)", true)]
        internal static bool CanRunSelected() => SelectedHeroGuids().Any();

        private static IEnumerable<string> SelectedHeroGuids()
            => Selection.objects.OfType<HeroDataSO>()
                .Select(hero => AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(hero)))
                .Where(guid => !string.IsNullOrEmpty(guid));

        private static void Request(string selectedGuids)
        {
            BenchmarkSettingsAsset.LoadOrCreate();    // made now, in Edit mode: Play mode cannot create an asset

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

            // null = the whole shop roster
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

        // Every report also goes into the "MagicSchool4 Benchmark" Google Sheet, through .claude/scripts/benchmark_import.py:
        // a hero report into its Runs and Latest tabs, a team comp report into Team Runs and Team Latest (the script tells
        // them apart by file name). In the background: a failed upload only logs, the CSV is still there.
        private const string ImportScript = ".claude/scripts/benchmark_import.py";
        private const string ImportKey = "MagicSchool.HeroBenchmark.ImportPath";

        // imported once back in Edit mode: leaving Play mode can reload scripts, which would drop the import's callback.
        // TeamCompBenchmarkMenu calls this too.
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
                    WorkingDirectory = System.IO.Directory.GetCurrentDirectory(),   // the project root, where the credential file is
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

            // watched from the editor loop rather than Process.Exited, which Unity's Mono does not raise reliably
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
