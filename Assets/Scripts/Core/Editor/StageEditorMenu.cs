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
    // What the Stage Editor window's buttons do (StageEditorWindow, Magic School 4 > Stage Editor): everything that needs
    // the Project window or writes an asset - the Editor half of StageEditor.cs. It used to be a menu of its own; the window
    // replaced it, so nothing here is a menu item now.
    //      AddEnemies        - spawn heroes as enemies, on the first free enemy hexes
    //      SaveToCurrent     - overwrite the enemy team (and the deploy zone, when it is being edited) of the current stage
    //      SaveAsNew         - write the enemy team and the deploy zone into a new BattlePlacementSO
    //      SavePlayerTeam    - write the player's heroes on the board, the items they wear and the items on hexes into a
    //                          team comp (Data/BattleSetups/Team Comps)
    //      LoadTeamComp      - swap the player's heroes on the board for a saved comp's
    //      LoadStage         - go to a stage of the run (its board, enemies, deploy zone and number); a stage outside the run
    //                          only has its enemies put on the board (and its deploy zone, when it is being edited)
    //
    // All of them need:
    //      Stage Edit Mode on > Play mode > Preparation state
    // Saving onto an existing stage or comp keeps the asset, so everything pointing at it still does. Teams are read
    // as serialized ints, 0 = the player, because Editor cannot name TeamEnum (Contracts). A stage or comp from
    // another board is refused: its hex numbers would land on the wrong hexes.
    internal static class StageEditorMenu
    {
        private const string DefaultFolder = "Assets/Data/BattleSetups/Campaign";
        private const string TeamCompFolder = "Assets/Data/BattleSetups/Team Comps";

        internal static void AddEnemies(IEnumerable<HeroDataSO> heroes)
        {
            if (!TryGetEditor(out StageEditor editor)) return;

            foreach (HeroDataSO hero in heroes)
            {
                if (editor.TryAddEnemy(hero)) continue;

                Debug.LogWarning($"[StageEditor] no free enemy hex left for {hero.Name}.");
                return;
            }
        }

        internal static void SaveToCurrent()
        {
            if (!TryReadTeam(out StageEditor editor, out List<HeroPlacement> team)) return;

            BattlePlacementSO stage = editor.CurrentStage;
            if (stage == null || !AssetDatabase.Contains(stage))
            {
                Debug.LogWarning("[StageEditor] the current stage is not an asset - use Save As New Stage.");
                return;
            }

            int enemiesBefore = editor.EnemyCountIn(stage);
            int zoneBefore = editor.DeployZoneCountIn(stage);
            int zoneAfter = editor.ReadDeployZone().Count;
            bool writeZone = editor.EditsDeployZone;

            bool confirmed = EditorUtility.DisplayDialog("Overwrite stage",
                (writeZone ? $"Replace the enemy team and the deploy zone of '{stage.name}'?\n\n"
                           : $"Replace the enemy team of '{stage.name}'? Its deploy zone is kept ('Edit deploy zone' is off).\n\n") +
                $"Enemies: {enemiesBefore} now, {team.Count} after." +
                (writeZone ? $"\nDeploy zone: {zoneBefore} hexes now, {zoneAfter} after." : ""), "Overwrite", "Cancel");
            if (!confirmed) return;

            Write(editor, stage, team, writeZone);
        }

        internal static void SaveAsNew()
        {
            if (!TryReadTeam(out StageEditor editor, out List<HeroPlacement> team)) return;

            string path = EditorUtility.SaveFilePanelInProject("Save stage", "New Stage", "asset",
                                                               "Where to save the enemy team and the deploy zone", DefaultFolder);
            if (string.IsNullOrEmpty(path)) return;

            BattlePlacementSO existing = AssetDatabase.LoadAssetAtPath<BattlePlacementSO>(path);
            if (existing != null)
            {
                Write(editor, existing, team, editor.EditsDeployZone);
                return;
            }

            BattlePlacementSO stage = BattlePlacementSO.CreateRuntime(team);
            AssetDatabase.CreateAsset(stage, path);
            Write(editor, stage, team, writeZone: true);
        }

        internal static void SavePlayerTeam()
        {
            if (!TryGetEditor(out StageEditor editor)) return;

            List<HeroPlacement> team = editor.ReadPlayerTeam();
            if (team.Count == 0)
            {
                Debug.LogWarning("[StageEditor] there is no player hero on the board to save.");
                return;
            }

            EnsureFolder(TeamCompFolder);
            string path = EditorUtility.SaveFilePanelInProject("Save team comp", "New Team Comp", "asset",
                                                               "Where to save the player's heroes on the board", TeamCompFolder);
            if (string.IsNullOrEmpty(path)) return;

            BattlePlacementSO comp = AssetDatabase.LoadAssetAtPath<BattlePlacementSO>(path);
            if (comp == null)
            {
                comp = BattlePlacementSO.CreateRuntime(new List<HeroPlacement>());
                AssetDatabase.CreateAsset(comp, path);
            }

            List<ItemPlacement> itemsOnHexes = editor.ReadItemsOnHexes();
            editor.WritePlayerTeam(comp, team, itemsOnHexes);

            EditorUtility.SetDirty(comp);
            AssetDatabase.SaveAssetIfDirty(comp);
            EditorGUIUtility.PingObject(comp);

            int worn = team.Count(placement => placement.item != null);
            Debug.Log($"[StageEditor] saved {team.Count} player heroes ({worn} wearing an item) and {itemsOnHexes.Count} items on hexes to {path}.");
        }

        internal static void LoadTeamComp(BattlePlacementSO comp)
        {
            if (!TryGetEditor(out StageEditor editor)) return;

            int inComp = editor.PlayerCountIn(comp);
            if (inComp == 0)
            {
                Debug.LogWarning($"[StageEditor] '{comp.name}' has no player hero in it.");
                return;
            }

            if (!editor.FitsThisBoard(comp))
            {
                Debug.LogWarning($"[StageEditor] '{comp.name}' was saved on '{comp.BoardLayout.name}' - play a stage on that board to load it.");
                return;
            }

            int onBoard = editor.PlayerCountOnBoard;
            if (onBoard > 0 && !EditorUtility.DisplayDialog("Load team comp",
                    $"Replace the {onBoard} player heroes on the board with the {inComp} in '{comp.name}'?\n\n" +
                    "The heroes on the board now are removed with their items, not refunded. Items lying on hexes go to the loose row. " +
                    "The bench is left alone.", "Load", "Cancel")) return;

            int placed = editor.LoadPlayerTeam(comp);
            Debug.Log($"[StageEditor] loaded {placed} of {inComp} heroes from '{comp.name}'.");
        }

        internal static void LoadStage(BattlePlacementSO stage)
        {
            if (!TryGetEditor(out StageEditor editor)) return;
            if (!editor.IsInRun(stage))
            {
                LoadEnemyTeam(editor, stage);
                return;
            }

            if (!EditorUtility.DisplayDialog("Go to stage",
                    $"Play '{stage.name}' now?\n\nIts enemies, levels, deploy zone and board replace what is on the board, and it " +
                    "becomes the current stage - Save To Current Stage writes to it. Unsaved enemy changes are lost.\n\n" +
                    "Your team stays where it stands (on the spare bench if the board changes). The shop is kept.",
                    "Go", "Cancel")) return;

            if (editor.LoadStage(stage)) Debug.Log($"[StageEditor] went to '{stage.name}'.");
            else Debug.LogWarning($"[StageEditor] could not go to '{stage.name}' - it can only be done in Preparation.");
        }

        private static void LoadEnemyTeam(StageEditor editor, BattlePlacementSO stage)
        {
            int inStage = editor.EnemyCountIn(stage);
            if (inStage == 0)
            {
                Debug.LogWarning($"[StageEditor] '{stage.name}' has no enemy in it.");
                return;
            }

            if (!editor.FitsThisBoard(stage))
            {
                Debug.LogWarning($"[StageEditor] '{stage.name}' is fought on '{stage.BoardLayout.name}' - play a stage on that board to load it.");
                return;
            }

            bool withZone = editor.EditsDeployZone;
            int onBoard = editor.EnemyCountOnBoard;
            if (onBoard > 0 && !EditorUtility.DisplayDialog("Load enemy team",
                    $"Replace the {onBoard} enemies on the board with the {inStage} in '{stage.name}'?" +
                    (withZone ? $"\n\nThe deploy zone becomes the stage's too ({editor.DeployZoneCountIn(stage)} hexes)."
                              : "\n\nThe deploy zone is kept ('Edit deploy zone' is off).") +
                    $"\n\n'{stage.name}' is not in the run, so the current stage stays - Save To Current Stage writes to that one, not this.",
                    "Load", "Cancel")) return;

            int placed = editor.LoadEnemyTeam(stage, withZone);
            Debug.Log($"[StageEditor] loaded {placed} of {inStage} enemies from '{stage.name}'.");
        }

        internal const string QuickSavePath = "Assets/Data/BattleSetups/QuickSave.asset";
        private const string QuickSaveStageKey = "MagicSchool4.QuickSave.Stage";
        private const string QuickSaveWhenKey = "MagicSchool4.QuickSave.When";

        internal static bool HasQuickSave => AssetDatabase.LoadAssetAtPath<BattlePlacementSO>(QuickSavePath) != null;

        internal static string QuickSaveSummary
        {
            get
            {
                var save = AssetDatabase.LoadAssetAtPath<BattlePlacementSO>(QuickSavePath);
                if (save == null) return null;

                int heroes = 0, enemies = 0;
                SerializedProperty list = new SerializedObject(save).FindProperty("_heroesPlacement");
                for (int i = 0; list != null && i < list.arraySize; i++)
                {
                    SerializedProperty entry = list.GetArrayElementAtIndex(i);
                    if (entry.FindPropertyRelative("dataSO").objectReferenceValue == null) continue;
                    if (entry.FindPropertyRelative("team").intValue == 0) heroes++;
                    else enemies++;
                }
                string stage = System.IO.Path.GetFileNameWithoutExtension(EditorPrefs.GetString(QuickSaveStageKey, ""));
                string when = EditorPrefs.GetString(QuickSaveWhenKey, "");
                return $"{when}{(when.Length > 0 ? ", " : "")}{(stage.Length > 0 ? stage : "no stage")}: {heroes} heroes vs {enemies} enemies";
            }
        }

        internal static void QuickSave()
        {
            if (!TryGetEditor(out StageEditor editor)) return;

            var save = AssetDatabase.LoadAssetAtPath<BattlePlacementSO>(QuickSavePath);
            if (save == null)
            {
                save = BattlePlacementSO.CreateRuntime(new List<HeroPlacement>());
                AssetDatabase.CreateAsset(save, QuickSavePath);
            }

            editor.WriteQuickSave(save);
            EditorUtility.SetDirty(save);
            AssetDatabase.SaveAssetIfDirty(save);

            BattlePlacementSO stage = editor.CurrentStage;
            EditorPrefs.SetString(QuickSaveStageKey, stage != null ? AssetDatabase.GetAssetPath(stage) : "");
            EditorPrefs.SetString(QuickSaveWhenKey, System.DateTime.Now.ToString("HH:mm"));
            Debug.Log($"[StageEditor] quick saved - {QuickSaveSummary}.");
        }

        internal static void QuickLoad()
        {
            if (!TryGetEditor(out StageEditor editor)) return;

            var save = AssetDatabase.LoadAssetAtPath<BattlePlacementSO>(QuickSavePath);
            if (save == null)
            {
                Debug.LogWarning("[StageEditor] there is no quick save yet.");
                return;
            }

            var stage = AssetDatabase.LoadAssetAtPath<BattlePlacementSO>(EditorPrefs.GetString(QuickSaveStageKey, ""));
            if (stage != null && stage != editor.CurrentStage && editor.IsInRun(stage)) editor.LoadStage(stage);

            if (!editor.FitsThisBoard(save))
            {
                Debug.LogWarning($"[StageEditor] the quick save was made on '{save.BoardLayout.name}' - play a stage on that board to load it.");
                return;
            }

            (int players, int enemies) = editor.LoadQuickSave(save);
            Debug.Log($"[StageEditor] quick loaded {players} heroes and {enemies} enemies{(stage != null ? $" on '{stage.name}'" : "")}.");
        }

        private static void EnsureFolder(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder)) return;

            int split = folder.LastIndexOf('/');
            AssetDatabase.CreateFolder(folder.Substring(0, split), folder.Substring(split + 1));
        }

        private static void Write(StageEditor editor, BattlePlacementSO stage, List<HeroPlacement> team, bool writeZone)
        {
            var zone = editor.ReadDeployZone();

            editor.WriteEnemyTeam(stage, team);
            if (writeZone) editor.WriteDeployZone(stage, zone);

            EditorUtility.SetDirty(stage);
            AssetDatabase.SaveAssetIfDirty(stage);
            EditorGUIUtility.PingObject(stage);

            string zoneSaved = writeZone ? $"a deploy zone of {zone.Count} hexes" : $"its own deploy zone kept ({editor.DeployZoneCountIn(stage)} hexes)";
            Debug.Log($"[StageEditor] saved {team.Count} enemies and {zoneSaved} to {AssetDatabase.GetAssetPath(stage)}.");
        }

        private static bool TryReadTeam(out StageEditor editor, out List<HeroPlacement> team)
        {
            team = null;
            if (!TryGetEditor(out editor)) return false;

            team = editor.ReadEnemyTeam();
            if (team.Count == 0)
            {
                Debug.LogWarning("[StageEditor] there is no enemy on the board to save.");
                return false;
            }

            if (editor.EditsDeployZone && editor.ReadDeployZone().Count == 0)
            {
                Debug.LogWarning("[StageEditor] the deploy zone is empty - click at least one player hex to put it in the zone.");
                return false;
            }

            return true;
        }

        private static bool TryGetEditor(out StageEditor editor)
        {
            editor = null;

            if (!EditorApplication.isPlaying || GameManager.Instance == null)
            {
                Debug.LogWarning("[StageEditor] enter Play mode first.");
                return false;
            }

            editor = StageEditor.Current;
            if (editor == null)
            {
                Debug.LogWarning("[StageEditor] tick 'Stage Edit Mode' on GameManager first.");
                return false;
            }

            if (!editor.CanEditNow)
            {
                Debug.LogWarning("[StageEditor] only in Preparation - once the fight starts the enemies have left their hexes.");
                return false;
            }

            return true;
        }
    }
}
