using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using MagicSchool.Combat.Heroes;
using MagicSchool.Combat.Placements;
using MagicSchool.Core;

namespace MagicSchool.Editor
{
    // Magic School 4 > Stage Editor
    // The stage editing dev tools, in one place: switch Stage Edit Mode on, add enemies from the hero lists here, save the
    // enemy team, choose whether clicks paint the deploy zone (Enemy tab) - and save or load the player's team comps, and put
    // any hero on the bench for free (Player tab). The buttons call StageEditorMenu and AddHeroMenu.
    internal class StageEditorWindow : EditorWindow
    {
        private const string EnemyFolder = "Assets/Data/Heroes/Stats/Enemy";
        private const string ShopHeroFolder = "Assets/Data/Heroes/Stats/Player";
        private const string TeamCompFolder = "Assets/Data/BattleSetups/Team Comps";

        // remembered by the window and pushed into StageEditor.EditsDeployZone, which a new Play session starts on
        [SerializeField] private bool _editDeployZone = true;
        [SerializeField] private bool _shopHeroesOpen, _benchEnemiesOpen;
        [SerializeField] private string _search = "";
        [SerializeField] private int _tab;      // kept by the window, so it reopens on the same tab
        private Vector2 _scroll;

        private static readonly GUIContent[] Tabs =
        {
            new GUIContent("Enemy", "Add enemies, choose whether clicks paint the deploy zone, save the stage."),
            new GUIContent("Player", "Load a saved team comp onto the board or save the team on it - and put any hero on the bench."),
        };

        // the assets listed, found again whenever the project changes
        private List<(HeroDataSO hero, string label)> _enemies, _shopHeroes;
        private List<BattlePlacementSO> _comps;

        [MenuItem("Magic School 4/Stage Editor")]
        private static void Open() => GetWindow<StageEditorWindow>("Stage Editor");

        private void OnEnable() => FindAssets();
        private void OnProjectChange() => FindAssets();     // a hero or comp added, renamed or deleted
        private void OnInspectorUpdate() => Repaint();      // the status follows the game

        private void OnGUI()
        {
            StageEditor editor = Editor();
            if (editor != null) editor.EditsDeployZone = _editDeployZone;

            // above the tabs: what both need - the mode, what is on the board, the search
            WindowStyle.Intro("Edit the stage being played: add enemies, paint the deploy zone and save it - or load and save " +
                              "the player's team comps. Works in Play mode, in Preparation.");
            DrawStatus(editor);
            EditorGUILayout.Space(WindowStyle.Gap);
            _tab = WindowStyle.Tabs(_tab, Tabs);
            _search = WindowStyle.Search(_search);
            EditorGUILayout.Space(WindowStyle.Gap);

            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            if (_tab == 0) DrawEnemyTeam(editor);
            else DrawPlayerTeam(editor);
            EditorGUILayout.EndScrollView();
        }

        // =================================== status ===================================
        private void DrawStatus(StageEditor editor)
        {
            GameManager game = Application.isPlaying ? GameManager.Instance : Object.FindAnyObjectByType<GameManager>();
            if (game == null)
            {
                WindowStyle.Status("Open the Board scene - its GameManager is what this edits.");
                return;
            }

            // Stage Edit Mode: in Play mode for this session only; outside it, the scene's setting (save the scene to keep it)
            var serialized = new SerializedObject(game);
            SerializedProperty mode = serialized.FindProperty("_stageEditMode");
            serialized.Update();
            bool was = mode.boolValue;
            bool now = EditorGUILayout.ToggleLeft(new GUIContent("Stage Edit Mode",
                "In Preparation, enemies drag between the hexes outside the deploy zone like your heroes do inside it, and " +
                "dropping one on the shop removes it. In Play mode this lasts until Play ends; outside it, it is the scene's " +
                "GameManager setting."), was, EditorStyles.boldLabel);
            if (now != was)
            {
                mode.boolValue = now;
                serialized.ApplyModifiedProperties();
            }

            string why = WhyNot(editor);
            if (why != null)
            {
                WindowStyle.Status(why);
                if (!EditorApplication.isPlayingOrWillChangePlaymode && WindowStyle.Action("Enter Play mode"))
                    EditorApplication.EnterPlaymode();
                return;
            }

            BattlePlacementSO stage = editor.CurrentStage;
            EditorGUILayout.LabelField("Current stage", stage != null ? stage.name : "(none)");
            EditorGUILayout.LabelField("On the board", $"{editor.EnemyCountOnBoard} enemies, deploy zone {editor.ReadDeployZone().Count} hexes" +
                                                      (stage != null ? $" (the stage has {editor.EnemyCountIn(stage)} and {editor.DeployZoneCountIn(stage)})" : ""));
        }

        // =================================== enemy team ===================================
        private void DrawEnemyTeam(StageEditor editor)
        {
            bool ready = WhyNot(editor) == null;

            // every enemy hero, one Add each
            DrawHeroList(_enemies, AddButton, ready, AddEnemy);

            // any hero can stand on the enemy side: every other one (Data/Heroes/Stats/Player - the shop's, test units too), folded away
            _shopHeroesOpen = EditorGUILayout.Foldout(_shopHeroesOpen, new GUIContent($"Other heroes ({Filtered(_shopHeroes).Count()})",
                $"Every hero in {ShopHeroFolder}: the shop's, and test units like the Dummy. Any of them can be an enemy."), true);
            if (_shopHeroesOpen)
            {
                using (new EditorGUI.IndentLevelScope()) DrawHeroList(_shopHeroes, AddButton, ready, AddEnemy);
            }

            EditorGUILayout.Space(6);
            _editDeployZone = EditorGUILayout.ToggleLeft(new GUIContent("Edit deploy zone",
                "On: clicking an empty hex puts it in the deploy zone or takes it out (blue = in, red = out), and saving writes " +
                "the zone. Off: clicks paint nothing, and Save To Current Stage keeps the zone the stage has. A new stage always " +
                "takes the board's zone - a stage cannot have none."), _editDeployZone);
            if (editor != null) editor.EditsDeployZone = _editDeployZone;

            using (new EditorGUI.DisabledScope(!ready))
            using (new EditorGUILayout.HorizontalScope())
            {
                string what = _editDeployZone ? "the enemy team and the deploy zone" : "the enemy team (its zone is kept)";
                if (WindowStyle.Action(new GUIContent("Save To Current Stage", $"Overwrite {what} of the stage being played. Asks first.")))
                    StageEditorMenu.SaveToCurrent();
                if (WindowStyle.Action(new GUIContent("Save As New Stage...", "Write the enemy team and the board's deploy zone into a new " +
                                                                             "stage asset, or over one you pick.")))
                    StageEditorMenu.SaveAsNew();
            }
        }

        private static readonly System.Func<HeroDataSO, GUIContent> AddButton =
            hero => new GUIContent("Add", $"Put {hero.Name} on the first free enemy hex.");
        private static readonly System.Func<HeroDataSO, GUIContent> BenchButton =
            hero => new GUIContent("Bench", $"Put {hero.Name} on your bench, for free (Play mode).");

        private static void AddEnemy(HeroDataSO hero) => StageEditorMenu.AddEnemies(new[] { hero });

        // one row per hero: its name, role and tier, the tab's button, and Select
        private void DrawHeroList(List<(HeroDataSO hero, string label)> heroes, System.Func<HeroDataSO, GUIContent> button,
                                  bool enabled, System.Action<HeroDataSO> onClick)
        {
            var shown = Filtered(heroes).ToList();
            if (shown.Count == 0)
            {
                WindowStyle.Hint(heroes == null || heroes.Count == 0 ? "None found." : "Nothing matches the search.");
                return;
            }

            foreach ((HeroDataSO hero, string label) in shown)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField(new GUIContent(hero.Name, AssetDatabase.GetAssetPath(hero)), GUILayout.MinWidth(110));
                    EditorGUILayout.LabelField(label, EditorStyles.miniLabel, GUILayout.Width(120));
                    using (new EditorGUI.DisabledScope(!enabled))
                    {
                        if (WindowStyle.RowButton(button(hero))) onClick(hero);
                    }
                    if (WindowStyle.RowButton(new GUIContent("Select", "Show it in the Project window.")))
                        Reveal(hero);
                }
            }
        }

        // =================================== player team ===================================
        private void DrawPlayerTeam(StageEditor editor)
        {
            bool ready = WhyNot(editor) == null;

            WindowStyle.Section("Team comps");
            var comps = (_comps ?? new List<BattlePlacementSO>()).Where(c => c != null && Matches(c.name)).ToList();
            if (comps.Count == 0)
                WindowStyle.Hint(_comps == null || _comps.Count == 0 ? $"No team comp in {TeamCompFolder} yet." : "Nothing matches the search.");

            foreach (BattlePlacementSO comp in comps)
            {
                string board = comp.BoardLayout != null ? comp.BoardLayout.name : "any board";
                bool fits = editor == null || editor.FitsThisBoard(comp);
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField(new GUIContent(comp.name, AssetDatabase.GetAssetPath(comp)), GUILayout.MinWidth(110));
                    EditorGUILayout.LabelField($"{comp.HeroesPlacement.Count} heroes, {board}", EditorStyles.miniLabel, GUILayout.Width(150));
                    using (new EditorGUI.DisabledScope(!ready || !fits))
                    {
                        string tip = fits ? $"Swap your heroes on the board for {comp.name}'s. Asks first if the board has heroes."
                                          : $"Saved on {board} - play a stage on that board to load it.";
                        if (WindowStyle.RowButton(new GUIContent("Load", tip)))
                            StageEditorMenu.LoadTeamComp(comp);
                    }
                    if (WindowStyle.RowButton(new GUIContent("Select", "Show it in the Project window.")))
                        Reveal(comp);
                }
            }

            EditorGUILayout.Space(4);
            using (new EditorGUI.DisabledScope(!ready))
            {
                if (WindowStyle.Action(new GUIContent("Save Player Team As...", "Write your heroes on the board, the items they wear and the " +
                                                                               "items on hexes into a team comp.")))
                    StageEditorMenu.SavePlayerTeam();
            }

            DrawBench();
        }

        // any hero onto the bench for free - Play mode only, Stage Edit Mode not needed (See AddHeroMenu)
        private void DrawBench()
        {
            WindowStyle.Section("Add to bench");
            bool canBench = AddHeroMenu.CanAdd;
            if (!canBench) WindowStyle.Hint("In Play mode, a Bench button puts that hero on your bench for free - no shop roll needed.");

            DrawHeroList(_shopHeroes, BenchButton, canBench, hero => AddHeroMenu.Add(hero));

            _benchEnemiesOpen = EditorGUILayout.Foldout(_benchEnemiesOpen, new GUIContent($"Enemy heroes ({Filtered(_enemies).Count()})",
                $"Every hero in {EnemyFolder}. They can be benched too, to try one on your side."), true);
            if (_benchEnemiesOpen)
            {
                using (new EditorGUI.IndentLevelScope()) DrawHeroList(_enemies, BenchButton, canBench, hero => AddHeroMenu.Add(hero));
            }
        }

        // =================================== helpers ===================================
        private void FindAssets()
        {
            _enemies = HeroesIn(EnemyFolder);
            _shopHeroes = HeroesIn(ShopHeroFolder);
            _comps = AssetDatabase.FindAssets("t:BattlePlacementSO", new[] { TeamCompFolder })
                .Select(guid => AssetDatabase.LoadAssetAtPath<BattlePlacementSO>(AssetDatabase.GUIDToAssetPath(guid)))
                .Where(comp => comp != null)
                .OrderBy(comp => comp.name)
                .ToList();
            Repaint();
        }

        // each hero with its role and tier, read off the asset - Editor cannot name the enums (they are in Contracts)
        private static List<(HeroDataSO, string)> HeroesIn(string folder)
        {
            if (!AssetDatabase.IsValidFolder(folder)) return new List<(HeroDataSO, string)>();

            return AssetDatabase.FindAssets("t:HeroDataSO", new[] { folder })
                .Select(guid => AssetDatabase.LoadAssetAtPath<HeroDataSO>(AssetDatabase.GUIDToAssetPath(guid)))
                .Where(hero => hero != null)
                .Select(hero => (hero, Describe(hero)))
                .OrderBy(entry => entry.hero.Name)
                .ToList();
        }

        private static string Describe(HeroDataSO hero)
        {
            var serialized = new SerializedObject(hero);
            string Enum(string field)
            {
                SerializedProperty p = serialized.FindProperty(field);
                return p != null && p.propertyType == SerializedPropertyType.Enum && p.enumValueIndex >= 0 && p.enumValueIndex < p.enumDisplayNames.Length
                    ? p.enumDisplayNames[p.enumValueIndex] : "";
            }
            return string.Join(", ", new[] { Enum("_role"), Enum("_tier") }.Where(s => s.Length > 0));
        }

        private IEnumerable<(HeroDataSO hero, string label)> Filtered(List<(HeroDataSO hero, string label)> heroes)
            => (heroes ?? new List<(HeroDataSO, string)>()).Where(entry => entry.hero != null && (Matches(entry.hero.Name) || Matches(entry.hero.name)));

        private bool Matches(string name)
            => string.IsNullOrWhiteSpace(_search) || (name ?? "").IndexOf(_search.Trim(), System.StringComparison.OrdinalIgnoreCase) >= 0;

        private static void Reveal(Object asset)
        {
            Selection.activeObject = asset;
            EditorGUIUtility.PingObject(asset);
        }

        private static StageEditor Editor()
            => EditorApplication.isPlaying && GameManager.Instance != null ? GameManager.Instance.StageEditor : null;

        // what stops editing right now, in words - or null when nothing does
        private static string WhyNot(StageEditor editor)
        {
            if (!EditorApplication.isPlaying) return "The stage is edited in Play mode, in Preparation.";
            if (GameManager.Instance == null) return "Waiting for the game to start.";
            if (editor == null) return "Tick Stage Edit Mode above.";
            if (!editor.CanEditNow) return "Only in Preparation - once the fight starts the enemies have left their hexes.";
            return null;
        }
    }
}
