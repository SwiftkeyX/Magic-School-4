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
        private static void RunAll() => Request(string.Empty);

        [MenuItem("Magic School 4/Balance/Run Hero Benchmark (Selected)")]
        private static void RunSelected() => Request(string.Join(";", SelectedHeroGuids()));

        // greyed out until at least one hero is selected
        [MenuItem("Magic School 4/Balance/Run Hero Benchmark (Selected)", true)]
        private static bool CanRunSelected() => SelectedHeroGuids().Any();

        private static IEnumerable<string> SelectedHeroGuids()
            => Selection.objects.OfType<HeroDataSO>()
                .Select(hero => AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(hero)))
                .Where(guid => !string.IsNullOrEmpty(guid));

        private static void Request(string selectedGuids)
        {
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

            string who = only == null ? "every shop hero" : string.Join(", ", only.Select(h => h.Name));
            Debug.Log($"[HeroBenchmark] started - {who}: a 30s damage test, a 30s AoE test (4 dummies) and a tank test each. " +
                      "The screen stays blank until it is done.");

            HeroBenchmark.Run(dummy, only, path =>
            {
                EditorApplication.ExitPlaymode();
                EditorUtility.RevealInFinder(path);
            });
        }
    }
}
