using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using MagicSchool.Combat.Heroes;
using MagicSchool.Combat.Placements;
using MagicSchool.Core;
using MagicSchool.DevTools;

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
        private const string ItemFolder = "Assets/Data/Items";
        private const string StageFolder = "Assets/Data/BattleSetups/Campaign";

        [SerializeField] private bool _editDeployZone = true;
        [SerializeField] private bool _shopHeroesOpen, _benchEnemiesOpen;
        [SerializeField] private int _newEnemyLevel = HeroLevel.Min;
        [SerializeField] private int _allLevel = HeroLevel.Min;
        [SerializeField] private string _search = "";
        [SerializeField] private int _tab;
        [SerializeField] private int _playerTab;
        [SerializeField] private int _enemyTab;
        private Vector2 _scroll;

        private static readonly GUIContent[] Tabs =
        {
            new GUIContent("Enemy", "Load a stage, add enemies and set their levels, paint the deploy zone, save the stage."),
            new GUIContent("Player", "Load a saved team comp onto the board or save the team on it - and put any hero on the bench " +
                                     "or any item on the loose row."),
        };

        private static readonly GUIContent[] EnemyTabs =
        {
            new GUIContent("Stage", "Load a stage's enemies (and its deploy zone) onto the board."),
            new GUIContent("Add", "Add any hero as an enemy, at a level."),
            new GUIContent("Levels", "Every enemy on the board, with its level."),
        };

        private static readonly GUIContent[] PlayerTabs =
        {
            new GUIContent("Team Comp", "Load a saved team comp onto the board, or save the team on it."),
            new GUIContent("Bench", "Put any hero on your bench, for free (Play mode)."),
            new GUIContent("Item", "Put any item on the loose row, for free (Play mode)."),
        };

        private List<(HeroDataSO hero, string label)> _enemies, _shopHeroes;
        private List<BattlePlacementSO> _comps, _stages;
        private List<(ScriptableObject item, string name, string label)> _items;

        [MenuItem("Magic School 4/Stage Editor")]
        private static void Open() => GetWindow<StageEditorWindow>("Stage Editor");

        private void OnEnable()
        {
            wantsMouseMove = true;
            FindAssets();
        }
        private void OnProjectChange() => FindAssets();
        private void OnInspectorUpdate() => Repaint();

        private void OnGUI()
        {
            if (Event.current.type == EventType.MouseMove) Repaint();

            StageEditor editor = Editor();
            if (editor != null)
            {
                editor.EditsDeployZone = _editDeployZone;
                editor.NewEnemyLevel = _newEnemyLevel;
            }

            WindowStyle.Intro("Edit the stage being played: add enemies, paint the deploy zone and save it - or load and save " +
                              "the player's team comps. Works in Play mode, in Preparation.");
            DrawStatus(editor);
            DrawQuickSave(editor);
            EditorGUILayout.Space(WindowStyle.Gap);
            _tab = WindowStyle.Tabs(_tab, Tabs);
            if (_tab == 0) _enemyTab = WindowStyle.SubTabs(_enemyTab, EnemyTabs);
            else _playerTab = WindowStyle.SubTabs(_playerTab, PlayerTabs);
            _search = WindowStyle.Search(_search);
            EditorGUILayout.Space(WindowStyle.Gap);

            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            bool ready = WhyNot(editor) == null;
            if (_tab == 0)
            {
                if (_enemyTab == 1) DrawAddEnemies(editor, ready);
                else if (_enemyTab == 2) DrawEnemyLevels(editor, ready);
                else DrawStages(editor, ready);
            }
            else if (_playerTab == 1) DrawBench();
            else if (_playerTab == 2) DrawItems();
            else DrawTeamComps(editor);
            EditorGUILayout.EndScrollView();

            if (_tab == 0) DrawEnemySave(editor, ready);
        }

        private void DrawStatus(StageEditor editor)
        {
            GameManager game = Application.isPlaying ? GameManager.Instance : Object.FindAnyObjectByType<GameManager>();
            if (game == null)
            {
                WindowStyle.Status("Open the Board scene - its GameManager is what this edits.");
                return;
            }

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

        private void DrawAddEnemies(StageEditor editor, bool ready)
        {
            _newEnemyLevel = EditorGUILayout.IntSlider(new GUIContent("Add at level",
                "The level an added enemy starts at. Every level past 1 adds 10% to its HP, AD, AP and DF " +
                "(level 11 = double, 30 = 3.9x). Change one already on the board in Levels."), _newEnemyLevel, HeroLevel.Min, HeroLevel.Max);
            if (editor != null) editor.NewEnemyLevel = _newEnemyLevel;
            DrawHeroList(_enemies, AddButton, ready, AddEnemy);

            _shopHeroesOpen = EditorGUILayout.Foldout(_shopHeroesOpen, new GUIContent($"Other heroes ({Filtered(_shopHeroes).Count()})",
                $"Every hero in {ShopHeroFolder}: the shop's, and test units like the Dummy. Any of them can be an enemy."), true);
            if (_shopHeroesOpen)
            {
                using (new EditorGUI.IndentLevelScope()) DrawHeroList(_shopHeroes, AddButton, ready, AddEnemy);
            }
        }

        private void DrawQuickSave(StageEditor editor)
        {
            bool ready = WhyNot(editor) == null;
            string summary = StageEditorMenu.QuickSaveSummary;

            using (new EditorGUI.DisabledScope(!ready))
            using (new EditorGUILayout.HorizontalScope())
            {
                if (WindowStyle.Action(new GUIContent("Quick Save", "Remember the player's heroes on the board (stars, patterns, " +
                        "items), the enemies (levels), the deploy zone and the stage - one slot, overwritten without asking.")))
                    StageEditorMenu.QuickSave();

                using (new EditorGUI.DisabledScope(summary == null))
                {
                    if (WindowStyle.Action(new GUIContent("Quick Load", "Go to the saved stage and put both saved teams back on the " +
                            "board. The bench is left alone; heroes on the board now are removed, not refunded.")))
                        StageEditorMenu.QuickLoad();
                }
            }
            WindowStyle.Hint(summary != null ? $"Quick save: {summary}" : "No quick save yet.");
        }

        private void DrawEnemySave(StageEditor editor, bool ready)
        {
            EditorGUILayout.Space(WindowStyle.Gap);
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
            EditorGUILayout.Space(WindowStyle.Gap);
        }

        private void DrawStages(StageEditor editor, bool ready)
        {
            var stages = (_stages ?? new List<BattlePlacementSO>()).Where(s => s != null && Matches(s.name)).ToList();
            if (stages.Count == 0)
            {
                WindowStyle.Hint(_stages == null || _stages.Count == 0 ? $"No stage in {StageFolder}." : "Nothing matches the search.");
                return;
            }

            BattlePlacementSO current = editor?.CurrentStage;
            string LabelOf(BattlePlacementSO stage) => stage == current ? $"{stage.name}  (current)" : stage.name;
            float nameWidth = WindowStyle.NameWidth(stages.Select(LabelOf));

            for (int i = 0; i < stages.Count; i++)
            {
                BattlePlacementSO stage = stages[i];
                bool inRun = editor == null || editor.IsInRun(stage);
                bool fits = inRun || editor.FitsThisBoard(stage);
                (int count, string levels) = Enemies(stage);

                string board = stage.BoardLayout != null ? stage.BoardLayout.name : "another board";
                string tip = inRun ? $"Go to {stage.name}: its board, enemies, levels and deploy zone, and it becomes the " +
                                     "current stage. Your team and the shop are kept. Asks first."
                           : fits ? $"Not in the run: only swap the enemies on the board for {stage.name}'s" +
                                    (_editDeployZone ? ", and take its deploy zone." : ".") + " Asks first if the board has enemies."
                                  : $"Not in the run, and fought on {board} - play a stage on that board to load it.";

                var click = WindowStyle.ListRow(i, new GUIContent("Load", tip), ready && fits,
                                                new GUIContent(LabelOf(stage), AssetDatabase.GetAssetPath(stage)), nameWidth,
                                                $"{count} enemies, lv {levels}", stage == current ? EditorStyles.boldLabel : null);
                if (click == WindowStyle.RowClick.Button) StageEditorMenu.LoadStage(stage);
                if (click == WindowStyle.RowClick.Select) Reveal(stage);
            }
            WindowStyle.Hint("Loading a stage of the run goes to it: it becomes the current stage, so Save To Current Stage writes to " +
                             "it. A stage outside the run only has its enemies put on the board.");
        }

        private void DrawEnemyLevels(StageEditor editor, bool ready)
        {
            if (!ready)
            {
                WindowStyle.Hint("In Play mode, in Preparation, every enemy on the board is listed here with its level.");
                return;
            }

            List<Hero> enemies = editor.EnemiesOnBoard;
            if (enemies.Count == 0)
            {
                WindowStyle.Hint("No enemy on the board.");
                return;
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                _allLevel = EditorGUILayout.IntSlider(new GUIContent("Set all to", "One level for every enemy on the board."),
                                                      _allLevel, HeroLevel.Min, HeroLevel.Max);
                if (WindowStyle.RowButton(new GUIContent("Set all", "Give every enemy on the board this level.")))
                    foreach (Hero enemy in enemies) editor.SetLevel(enemy, _allLevel);
            }

            foreach (Hero enemy in enemies)
            {
                string name = enemy.Data != null ? enemy.Data.Name : enemy.name;
                var hex = enemy.CurrentHex.Number;
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField(new GUIContent(name, $"Hex {hex.column},{hex.row}"), GUILayout.Width(110));
                    EditorGUILayout.LabelField($"{enemy.MaxHP} HP", EditorStyles.miniLabel, GUILayout.Width(60));
                    int level = EditorGUILayout.IntSlider(enemy.Level, HeroLevel.Min, HeroLevel.Max);
                    if (level != enemy.Level) editor.SetLevel(enemy, level);
                }
            }
            WindowStyle.Hint("A level is saved with the stage. Changing one rebuilds that enemy's stats on the spot.");
        }

        private static readonly System.Func<HeroDataSO, GUIContent> AddButton =
            hero => new GUIContent("Add", $"Put {hero.Name} on the first free enemy hex.");
        private static readonly System.Func<HeroDataSO, GUIContent> BenchButton =
            hero => new GUIContent("Bench", $"Put {hero.Name} on your bench, for free (Play mode).");

        private static void AddEnemy(HeroDataSO hero) => StageEditorMenu.AddEnemies(new[] { hero });

        private void DrawHeroList(List<(HeroDataSO hero, string label)> heroes, System.Func<HeroDataSO, GUIContent> button,
                                  bool enabled, System.Action<HeroDataSO> onClick)
        {
            var shown = Filtered(heroes).ToList();
            if (shown.Count == 0)
            {
                WindowStyle.Hint(heroes == null || heroes.Count == 0 ? "None found." : "Nothing matches the search.");
                return;
            }

            float nameWidth = WindowStyle.NameWidth(shown.Select(entry => entry.hero.Name));
            for (int i = 0; i < shown.Count; i++)
            {
                (HeroDataSO hero, string label) = shown[i];
                var click = WindowStyle.ListRow(i, button(hero), enabled,
                                                new GUIContent(hero.Name, AssetDatabase.GetAssetPath(hero)), nameWidth, label);
                if (click == WindowStyle.RowClick.Button) onClick(hero);
                if (click == WindowStyle.RowClick.Select) Reveal(hero);
            }
        }

        private void DrawTeamComps(StageEditor editor)
        {
            bool ready = WhyNot(editor) == null;

            var comps = (_comps ?? new List<BattlePlacementSO>()).Where(c => c != null && Matches(c.name)).ToList();
            if (comps.Count == 0)
                WindowStyle.Hint(_comps == null || _comps.Count == 0 ? $"No team comp in {TeamCompFolder} yet." : "Nothing matches the search.");

            float nameWidth = WindowStyle.NameWidth(comps.Select(comp => comp.name));
            for (int i = 0; i < comps.Count; i++)
            {
                BattlePlacementSO comp = comps[i];
                string board = comp.BoardLayout != null ? comp.BoardLayout.name : "any board";
                bool fits = editor == null || editor.FitsThisBoard(comp);
                string tip = fits ? $"Swap your heroes on the board for {comp.name}'s. Asks first if the board has heroes."
                                  : $"Saved on {board} - play a stage on that board to load it.";

                var click = WindowStyle.ListRow(i, new GUIContent("Load", tip), ready && fits,
                                                new GUIContent(comp.name, AssetDatabase.GetAssetPath(comp)), nameWidth,
                                                $"{comp.HeroesPlacement.Count} heroes, {board}");
                if (click == WindowStyle.RowClick.Button) StageEditorMenu.LoadTeamComp(comp);
                if (click == WindowStyle.RowClick.Select) Reveal(comp);
            }

            EditorGUILayout.Space(4);
            using (new EditorGUI.DisabledScope(!ready))
            {
                if (WindowStyle.Action(new GUIContent("Save Player Team As...", "Write your heroes on the board, the items they wear and the " +
                                                                               "items on hexes into a team comp.")))
                    StageEditorMenu.SavePlayerTeam();
            }
        }

        private void DrawItems()
        {
            bool canDrop = AddHeroMenu.CanAdd;
            if (!canDrop) WindowStyle.Hint("In Play mode, a Drop button puts that item on the loose row for free - no shop roll needed.");

            var shown = (_items ?? new List<(ScriptableObject, string, string)>())
                .Where(entry => entry.item != null && (Matches(entry.name) || Matches(entry.item.name))).ToList();
            if (shown.Count == 0)
            {
                WindowStyle.Hint(_items == null || _items.Count == 0 ? $"No item in {ItemFolder}." : "Nothing matches the search.");
                return;
            }

            float nameWidth = WindowStyle.NameWidth(shown.Select(entry => entry.name));
            for (int i = 0; i < shown.Count; i++)
            {
                (ScriptableObject item, string name, string label) = shown[i];
                var click = WindowStyle.ListRow(i, new GUIContent("Drop", $"Put {name} on the loose row, for free (Play mode)."), canDrop,
                                                new GUIContent(name, AssetDatabase.GetAssetPath(item)), nameWidth, label);
                if (click == WindowStyle.RowClick.Button) AddHeroMenu.AddItem(item);
                if (click == WindowStyle.RowClick.Select) Reveal(item);
            }
        }

        private void DrawBench()
        {
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

        private void FindAssets()
        {
            _enemies = HeroesIn(EnemyFolder);
            _shopHeroes = HeroesIn(ShopHeroFolder);
            _comps = AssetDatabase.FindAssets("t:BattlePlacementSO", new[] { TeamCompFolder })
                .Select(guid => AssetDatabase.LoadAssetAtPath<BattlePlacementSO>(AssetDatabase.GUIDToAssetPath(guid)))
                .Where(comp => comp != null)
                .OrderBy(comp => comp.name)
                .ToList();
            _items = ItemsIn(ItemFolder);
            _stages = AssetDatabase.FindAssets("t:BattlePlacementSO", new[] { StageFolder })
                .Select(guid => AssetDatabase.LoadAssetAtPath<BattlePlacementSO>(AssetDatabase.GUIDToAssetPath(guid)))
                .Where(stage => stage != null)
                .OrderBy(stage => stage.name)
                .ToList();
            Repaint();
        }

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

        private static (int, string) Enemies(BattlePlacementSO stage)
        {
            SerializedProperty list = new SerializedObject(stage).FindProperty("_heroesPlacement");
            var levels = new List<int>();
            for (int i = 0; list != null && i < list.arraySize; i++)
            {
                SerializedProperty entry = list.GetArrayElementAtIndex(i);
                if (entry.FindPropertyRelative("dataSO").objectReferenceValue == null) continue;
                if (entry.FindPropertyRelative("team").intValue == 0) continue;
                levels.Add(HeroLevel.Clamp(entry.FindPropertyRelative("level").intValue));
            }
            return (levels.Count, levels.Count == 0 ? "-" : string.Join("/", levels.Distinct().OrderBy(l => l)));
        }

        private static List<(ScriptableObject, string, string)> ItemsIn(string folder)
        {
            if (!AssetDatabase.IsValidFolder(folder)) return new List<(ScriptableObject, string, string)>();

            return AssetDatabase.FindAssets("t:ItemDataSO", new[] { folder })
                .Select(guid => AssetDatabase.LoadAssetAtPath<ScriptableObject>(AssetDatabase.GUIDToAssetPath(guid)))
                .Where(item => item != null)
                .Select(item =>
                {
                    var serialized = new SerializedObject(item);
                    string name = serialized.FindProperty("_name")?.stringValue;
                    SerializedProperty price = serialized.FindProperty("_price");
                    string stats = StatsOf(serialized.FindProperty("_description")?.stringValue);
                    string label = (price != null ? $"{price.intValue} gold" : "") + (stats.Length > 0 ? $"    {stats}" : "");
                    return (item, string.IsNullOrEmpty(name) ? item.name : name, label);
                })
                .OrderBy(entry => entry.Item2)
                .ToList();
        }

        private static string StatsOf(string description)
        {
            if (string.IsNullOrEmpty(description)) return "";
            int end = description.IndexOf(". ", System.StringComparison.Ordinal);
            return (end < 0 ? description : description.Substring(0, end)).TrimEnd('.');
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
            => EditorApplication.isPlaying ? StageEditor.Current : null;

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
