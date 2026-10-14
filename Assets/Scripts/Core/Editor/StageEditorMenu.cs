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
    // the Editor half of StageEditor.cs: everything that needs the Project window or writes an asset.
    // All menu:
    //      1. Add Selected Heroes As Enemies      - spawn the selected HeroDataSOs (in project window) as enemies
    //      2. Save To Current Stage               - overwrite the enemy team and the deploy zone of the current stage
    //      3. Save As New Stage...                - write the enemy team and the deploy zone into a new BattlePlacementSO
    //      4. Save Player Team As...              - write the player's heroes on the board, the items they wear and the items
    //                                               on hexes into a team comp (Data/BattleSetups/Team Comps)
    //      5. Load Selected Team Comp             - swap the player's heroes on the board for the comp selected in the Project window
    //
    // All of them need:
    //      GameManager's "Stage Edit Mode" ticked > Play mode > Preparation state
    internal static class StageEditorMenu
    {
        private const string Menu = "Magic School 4/Stage Editor/";
        private const string DefaultFolder = "Assets/Data/BattleSetups/Campaign";
        private const string TeamCompFolder = "Assets/Data/BattleSetups/Team Comps";

        [MenuItem(Menu + "Add Selected Heroes As Enemies")]
        private static void AddSelected()
        {
            if (!TryGetEditor(out StageEditor editor)) return;

            List<HeroDataSO> heroes = Selection.objects.OfType<HeroDataSO>().ToList();
            if (heroes.Count == 0)
            {
                Debug.LogWarning("[StageEditor] select one or more HeroDataSO in the Project window first.");
                return;
            }

            foreach (HeroDataSO hero in heroes)
            {
                if (editor.TryAddEnemy(hero)) continue;

                Debug.LogWarning($"[StageEditor] no free enemy hex left for {hero.Name}.");
                return;
            }
        }

        [MenuItem(Menu + "Save To Current Stage")]
        private static void SaveToCurrent()
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

            bool confirmed = EditorUtility.DisplayDialog("Overwrite stage",
                $"Replace the enemy team and the deploy zone of '{stage.name}'?\n\n" +
                $"Enemies: {enemiesBefore} now, {team.Count} after.\n" +
                $"Deploy zone: {zoneBefore} hexes now, {zoneAfter} after.", "Overwrite", "Cancel");
            if (!confirmed) return;

            Write(editor, stage, team);
        }

        [MenuItem(Menu + "Save As New Stage...")]
        private static void SaveAsNew()
        {
            if (!TryReadTeam(out StageEditor editor, out List<HeroPlacement> team)) return;

            string path = EditorUtility.SaveFilePanelInProject("Save stage", "New Stage", "asset",
                                                               "Where to save the enemy team and the deploy zone", DefaultFolder);
            if (string.IsNullOrEmpty(path)) return;

            // picked an existing stage: keep the asset (and everything pointing at it), swap its enemy team and zone
            BattlePlacementSO existing = AssetDatabase.LoadAssetAtPath<BattlePlacementSO>(path);
            if (existing != null)
            {
                Write(editor, existing, team);
                return;
            }

            BattlePlacementSO stage = BattlePlacementSO.CreateRuntime(team);
            AssetDatabase.CreateAsset(stage, path);
            Write(editor, stage, team);
        }

        // ======================================== team comp ========================================
        [MenuItem(Menu + "Save Player Team As...", priority = 100)]
        private static void SavePlayerTeam()
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

            // picked an existing comp: keep the asset, swap its team
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

        [MenuItem(Menu + "Load Selected Team Comp", priority = 101)]
        private static void LoadPlayerTeam()
        {
            if (!TryGetEditor(out StageEditor editor)) return;

            BattlePlacementSO comp = Selection.activeObject as BattlePlacementSO;
            if (comp == null)
            {
                Debug.LogWarning($"[StageEditor] select a saved team comp (a Battle Placement in {TeamCompFolder}) in the Project window first.");
                return;
            }

            int inComp = editor.PlayerCountIn(comp);
            if (inComp == 0)
            {
                Debug.LogWarning($"[StageEditor] '{comp.name}' has no player hero in it.");
                return;
            }

            // its hex numbers would land on the wrong hexes of another board
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

        // ======================================== helper ========================================
        private static void EnsureFolder(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder)) return;

            int split = folder.LastIndexOf('/');
            AssetDatabase.CreateFolder(folder.Substring(0, split), folder.Substring(split + 1));
        }

        private static void Write(StageEditor editor, BattlePlacementSO stage, List<HeroPlacement> team)
        {
            var zone = editor.ReadDeployZone();

            editor.WriteEnemyTeam(stage, team);
            editor.WriteDeployZone(stage, zone);

            EditorUtility.SetDirty(stage);
            AssetDatabase.SaveAssetIfDirty(stage);
            EditorGUIUtility.PingObject(stage);

            Debug.Log($"[StageEditor] saved {team.Count} enemies and a deploy zone of {zone.Count} hexes to {AssetDatabase.GetAssetPath(stage)}.");
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

            // every stage must list its zone - an empty one would leave the player nowhere to stand
            if (editor.ReadDeployZone().Count == 0)
            {
                Debug.LogWarning("[StageEditor] the deploy zone is empty - click at least one player hex to put it in the zone.");
                return false;
            }

            return true;
        }

        // say why, rather than grey the menu out with no explanation
        private static bool TryGetEditor(out StageEditor editor)
        {
            editor = null;

            if (!EditorApplication.isPlaying || GameManager.Instance == null)
            {
                Debug.LogWarning("[StageEditor] enter Play mode first.");
                return false;
            }

            editor = GameManager.Instance.StageEditor;
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
