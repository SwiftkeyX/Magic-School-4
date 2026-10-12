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
    //
    // All three need: 
    //      GameManager's "Stage Edit Mode" ticked > Play mode > Preparation state
    internal static class StageEditorMenu
    {
        private const string Menu = "Magic School 4/Stage Editor/";
        private const string DefaultFolder = "Assets/Data/BattleSetups/Campaign";

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

        // ======================================== helper ========================================
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
