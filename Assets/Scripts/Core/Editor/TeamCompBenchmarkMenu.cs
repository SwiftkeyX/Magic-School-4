using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using MagicSchool.Combat.Placements;
using MagicSchool.Core.Benchmark;

namespace MagicSchool.Editor
{
    // Team Comp Benchmark - run from the Benchmark window (Magic School 4 > Benchmark), which calls RunAll / RunSelected
    // every team comp vs every campaign stage, using TeamCompBenchmark.cs
    //      Run Team Comp Benchmark              - every comp in Data/BattleSetups/Team Comps/
    //      Run Team Comp Benchmark (Selected)   - only the comps selected in the Project window
    [InitializeOnLoad]
    internal static class TeamCompBenchmarkMenu
    {
        private const string CompFolder = "Assets/Data/BattleSetups/Team Comps";
        private const string PendingKey = "MagicSchool.TeamCompBenchmark.Pending";   // asset GUIDs, ';'-separated

        static TeamCompBenchmarkMenu()
        {
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
        }

        internal static void RunAll() => Request(AllCompGuids());

        internal static void RunSelected() => Request(SelectedCompGuids());

        // greyed out until at least one comp is selected
        internal static bool CanRunSelected() => SelectedCompGuids().Any();

        private static IEnumerable<string> AllCompGuids()
            => AssetDatabase.FindAssets("t:BattlePlacementSO", new[] { CompFolder });

        private static IEnumerable<string> SelectedCompGuids()
            => Selection.objects.OfType<BattlePlacementSO>()
                .Select(comp => AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(comp)))
                .Where(guid => !string.IsNullOrEmpty(guid));

        private static void Request(IEnumerable<string> guids)
        {
            BenchmarkSettingsAsset.LoadOrCreate();    // made now, in Edit mode: Play mode cannot create an asset

            string list = string.Join(";", guids);
            if (list.Length == 0)
            {
                Debug.LogError($"[TeamCompBenchmark] no team comp in {CompFolder}.");
                return;
            }

            if (EditorApplication.isPlaying)
            {
                Begin(list);
                return;
            }

            SessionState.SetString(PendingKey, list);
            EditorApplication.EnterPlaymode();
        }

        private static void OnPlayModeChanged(PlayModeStateChange change)
        {
            if (change != PlayModeStateChange.EnteredPlayMode) return;

            string list = SessionState.GetString(PendingKey, string.Empty);
            if (list.Length == 0) return;

            SessionState.EraseString(PendingKey);
            Begin(list);
        }

        private static void Begin(string guids)
        {
            List<BattlePlacementSO> comps = guids.Split(';')
                .Select(guid => AssetDatabase.LoadAssetAtPath<BattlePlacementSO>(AssetDatabase.GUIDToAssetPath(guid)))
                .Where(comp => comp != null)
                .ToList();

            BenchmarkSettingsSO settings = BenchmarkSettingsAsset.LoadOrCreate();
            BenchmarkSettingsSO s = settings != null ? settings : BenchmarkSettingsSO.Current;
            Debug.Log($"[TeamCompBenchmark] started - {string.Join(", ", comps.Select(c => c.name))} vs every campaign stage, " +
                      $"{s.runsAligned} runs aligned and {s.runsNotAligned} without, a stage won at {s.winLine:0%} of them. " +
                      "The screen stays blank until it is done.");

            TeamCompBenchmark.Run(comps, settings, path =>
            {
                HeroBenchmarkMenu.ImportWhenBackInEditMode(path);   // into the Benchmark sheet's Team Runs / Team Latest tabs
                EditorApplication.ExitPlaymode();
                EditorUtility.RevealInFinder(path);
            });
        }
    }
}
