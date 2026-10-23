using System.Linq;
using UnityEditor;
using UnityEngine;
using MagicSchool.Combat.Heroes;
using MagicSchool.Combat.Placements;
using MagicSchool.Core;
using MagicSchool.Core.Benchmark;

namespace MagicSchool.Editor
{
    // Magic School 4 > Balance > Benchmark
    // Every setting of the two benchmarks, with a tooltip on each, and a button to run each one.
    // The settings live in Data/Benchmark/BenchmarkSettings.asset (See BenchmarkSettingsSO), so they are kept, and the
    // Balance menu items run with them too. A run reads them when it starts: changing one mid-run does nothing to it.
    internal class BenchmarkWindow : EditorWindow
    {
        private const string TeamCompFolder = "Assets/Data/BattleSetups/Team Comps";

        // rough real seconds, measured 2026-10-23 on the defaults: one batch of hero lanes through the three 30s tests,
        // and one team comp fight at 32 lanes
        private const float SecondsPerHeroBatch = 9f;
        private const float SecondsPerTeamFight = 0.5f;

        // shown on each tab: what changes what the report says
        private static readonly string[] HeroFields = { "alliesSmallTeam", "alliesBigTeam", "flagBand" };
        private static readonly string[] TeamFields = { "runsAligned", "runsNotAligned", "timeLimit", "winLine" };

        // folded away under Advanced: how the tests are built, and speed - nothing a normal run needs to touch
        private static readonly string[] HeroAdvanced =
        {
            "shortSeconds", "longSeconds", "pressurePerHit", "pressureInterval", "dummyRefillBelow", "heroLanes",
        };
        private static readonly string[] TeamAdvanced = { "teamLanes" };

        private static readonly GUIContent[] Tabs =
        {
            new GUIContent("Hero Benchmark", "Each shop hero alone vs dummies - DPS, AoE and how long it lasts."),
            new GUIContent("Team Comp Benchmark", "Each saved team comp vs every campaign stage - which stages it wins."),
        };

        [SerializeField] private int _tab;      // kept by the window, so it reopens on the same tab
        [SerializeField] private bool _heroAdvancedOpen, _teamAdvancedOpen;
        private BenchmarkSettingsSO _settings;
        private BenchmarkSettingsSO _defaults;  // to say how many advanced settings are not the default
        private SerializedObject _serialized;
        private Vector2 _scroll;

        [MenuItem("Magic School 4/Balance/Benchmark")]
        private static void Open() => GetWindow<BenchmarkWindow>("Benchmark");

        private void OnEnable() => Bind();
        private void OnSelectionChange() => Repaint();      // the "Selected" buttons count the selection
        private void OnInspectorUpdate() => Repaint();      // the buttons follow Play mode

        private void Bind()
        {
            _settings = EditorApplication.isPlayingOrWillChangePlaymode
                ? AssetDatabase.LoadAssetAtPath<BenchmarkSettingsSO>(BenchmarkSettingsSO.AssetPath)
                : BenchmarkSettingsAsset.LoadOrCreate();
            _serialized = _settings != null ? new SerializedObject(_settings) : null;
        }

        private void OnGUI()
        {
            if (_settings == null || _serialized == null || _serialized.targetObject == null) Bind();
            if (_serialized == null)
            {
                EditorGUILayout.HelpBox($"No settings asset yet. Leave Play mode to make {BenchmarkSettingsSO.AssetPath}.", MessageType.Info);
                return;
            }

            _serialized.Update();

            DrawTop();
            EditorGUILayout.Space(6);
            _tab = GUILayout.Toolbar(_tab, Tabs, GUILayout.Height(24));
            EditorGUILayout.Space(6);

            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            if (_tab == 0) DrawHeroBenchmark();
            else DrawTeamCompBenchmark();
            EditorGUILayout.EndScrollView();
            _serialized.ApplyModifiedProperties();
        }

        // =================================== top ===================================
        private void DrawTop()
        {
            EditorGUILayout.HelpBox("Hover a setting to read what it does. A run uses these from the moment it starts; " +
                                    "reports are imported into the MagicSchool4 Benchmark sheet when it ends.", MessageType.None);

            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(true))
                    EditorGUILayout.ObjectField("Settings asset", _settings, typeof(BenchmarkSettingsSO), false);

                if (GUILayout.Button(new GUIContent("Reset to defaults", "Put every setting back to the value it ships with."), GUILayout.Width(120))
                    && EditorUtility.DisplayDialog("Reset benchmark settings", "Put every benchmark setting back to its default?", "Reset", "Cancel"))
                {
                    var defaults = CreateInstance<BenchmarkSettingsSO>();
                    Undo.RecordObject(_settings, "Reset benchmark settings");
                    JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(defaults), _settings);
                    DestroyImmediate(defaults);
                    EditorUtility.SetDirty(_settings);
                    _serialized.Update();
                }
            }
        }

        // =================================== hero benchmark ===================================
        private void DrawHeroBenchmark()
        {
            Section("Hero Benchmark", "Every shop hero alone vs a 2000 HP dummy: a damage test, an AoE test (6 dummies) and a " +
                                      "tank test - then again with its own mark hexes filled, for a smaller and a bigger team.");
            foreach (string field in HeroFields) EditorGUILayout.PropertyField(_serialized.FindProperty(field), true);
            _heroAdvancedOpen = Advanced(_heroAdvancedOpen, HeroAdvanced);

            int roster = ArraySize("_shopRoster");
            int selected = Selection.objects.OfType<HeroDataSO>().Count();
            int lanes = Mathf.Max(1, _settings.heroLanes);
            int passes = 1 + (_settings.alliesSmallTeam > 0 ? 1 : 0) + (_settings.alliesBigTeam > _settings.alliesSmallTeam ? 1 : 0);
            float perBatch = SecondsPerHeroBatch * (_settings.longSeconds / 30f);
            float alone = Mathf.Ceil(roster / (float)lanes) * perBatch;
            EditorGUILayout.HelpBox(
                roster > 0
                    ? $"{roster} shop heroes, {lanes} at a time. Alone takes about {alone:0}s; the {passes - 1} mark pass(es) " +
                      $"re-test only the heroes with orange marks, up to about {alone * passes:0}s in all (rough)."
                    : "Open the Board scene to see how many heroes a run tests.",
                MessageType.Info);

            RunButtons("Run Hero Benchmark", "Test every hero in GameManager's shop roster.", HeroBenchmarkMenu.RunAll,
                       $"Run Selected ({selected})", "Test only the HeroDataSOs selected in the Project window; the others' numbers " +
                       "for the role comparison come from the newest full report.", HeroBenchmarkMenu.RunSelected, selected > 0);
        }

        // =================================== team comp benchmark ===================================
        private void DrawTeamCompBenchmark()
        {
            Section("Team Comp Benchmark", "Every saved team comp vs every campaign stage, with aligning and without, " +
                                           "until a side is dead or the time limit.");
            foreach (string field in TeamFields) EditorGUILayout.PropertyField(_serialized.FindProperty(field), true);
            _teamAdvancedOpen = Advanced(_teamAdvancedOpen, TeamAdvanced);

            int comps = AssetDatabase.FindAssets("t:BattlePlacementSO", new[] { TeamCompFolder }).Length;
            int selected = Selection.objects.OfType<BattlePlacementSO>().Count();
            int stages = ArraySize("_stages");
            int perStage = Mathf.Max(1, _settings.runsAligned) + Mathf.Max(0, _settings.runsNotAligned);
            int fights = comps * stages * perStage;
            // fewer lanes than measured is slower; more than 32 gains little, so it is not counted
            float seconds = fights * SecondsPerTeamFight * (32f / Mathf.Clamp(_settings.teamLanes, 1, 32));
            int needed = Mathf.CeilToInt(_settings.winLine * Mathf.Max(1, _settings.runsAligned) - 0.0001f);
            EditorGUILayout.HelpBox(
                stages > 0
                    ? $"{comps} comps x {stages} stages x {perStage} fights = {fights} fights - about {Minutes(seconds)} (rough).\n" +
                      $"A stage is won at {needed} of {Mathf.Max(1, _settings.runsAligned)} aligned fights."
                    : "Open the Board scene to see how many fights a run plays.",
                MessageType.Info);

            RunButtons("Run Team Comp Benchmark", $"Fight every comp in {TeamCompFolder}.", TeamCompBenchmarkMenu.RunAll,
                       $"Run Selected ({selected})", "Fight only the team comps selected in the Project window.",
                       TeamCompBenchmarkMenu.RunSelected, selected > 0);
        }

        // =================================== helpers ===================================
        // A folded section of settings a normal run never needs. Its label says how many are not the default,
        // so a change cannot sit there unnoticed while folded.
        private bool Advanced(bool open, string[] fields)
        {
            if (_defaults == null) _defaults = CreateInstance<BenchmarkSettingsSO>();
            var defaults = new SerializedObject(_defaults);
            int changed = fields.Count(f => !SerializedProperty.DataEquals(_serialized.FindProperty(f), defaults.FindProperty(f)));

            EditorGUILayout.Space(4);
            string label = changed == 0 ? "Advanced" : $"Advanced  ({changed} changed from default)";
            open = EditorGUILayout.Foldout(open, new GUIContent(label, "How the tests are built, and how fast they run. " +
                                                                       "The defaults are what the numbers were tuned on."), true);
            if (!open) return false;

            using (new EditorGUI.IndentLevelScope())
            {
                foreach (string field in fields) EditorGUILayout.PropertyField(_serialized.FindProperty(field), true);
            }
            return true;
        }

        private void OnDisable()
        {
            if (_defaults != null) DestroyImmediate(_defaults);
        }

        // the tab already names it: only what it does
        private static void Section(string title, string about)
        {
            EditorGUILayout.LabelField(about, EditorStyles.wordWrappedMiniLabel);
            EditorGUILayout.Space(2);
        }

        private static void RunButtons(string all, string allTip, System.Action runAll,
                                       string some, string someTip, System.Action runSome, bool canRunSome)
        {
            // one run at a time: a run enters Play mode, and leaves it when done
            bool busy = EditorApplication.isPlayingOrWillChangePlaymode;
            using (new EditorGUI.DisabledScope(busy))
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button(new GUIContent(all, allTip), GUILayout.Height(26))) runAll();
                using (new EditorGUI.DisabledScope(!canRunSome))
                    if (GUILayout.Button(new GUIContent(some, someTip), GUILayout.Height(26), GUILayout.Width(150))) runSome();
            }
            if (busy) EditorGUILayout.LabelField("Running (or in Play mode) - the buttons come back once it is done.", EditorStyles.miniLabel);
        }

        // how many entries GameManager has in one of its lists, read off the scene - Editor cannot reach its internals
        private static int ArraySize(string field)
        {
            var game = Object.FindFirstObjectByType<GameManager>();
            if (game == null) return 0;

            SerializedProperty list = new SerializedObject(game).FindProperty(field);
            return list != null && list.isArray ? list.arraySize : 0;
        }

        private static string Minutes(float seconds)
            => seconds < 90f ? $"{seconds:0}s" : $"{seconds / 60f:0.0} min";
    }
}
