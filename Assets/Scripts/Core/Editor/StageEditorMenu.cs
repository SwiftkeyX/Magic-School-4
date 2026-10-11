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
    //      2. Save Enemy Team To Current Stage    - overwrite the enemy team of the current stage
    //      3. Save Enemy Team As New Stage...     - write the enemy team into a new BattlePlacementSO
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

        [MenuItem(Menu + "Save Enemy Team To Current Stage")]
        private static void SaveToCurrent()
        {
            if (!TryReadTeam(out StageEditor editor, out List<HeroPlacement> team)) return;

            BattlePlacementSO stage = editor.CurrentStage;
            if (stage == null || !AssetDatabase.Contains(stage))
            {
                Debug.LogWarning("[StageEditor] the current stage is not an asset - use Save Enemy Team As New Stage.");
                return;
            }

            int before = editor.EnemyCountIn(stage);
            bool confirmed = EditorUtility.DisplayDialog("Overwrite stage",
                $"Replace the enemy team of '{stage.name}'?\n\n{before} enemies now, {team.Count} after.", "Overwrite", "Cancel");
            if (!confirmed) return;

            Write(editor, stage, team);
        }

        [MenuItem(Menu + "Save Enemy Team As New Stage...")]
        private static void SaveAsNew()
        {
            if (!TryReadTeam(out StageEditor editor, out List<HeroPlacement> team)) return;

            string path = EditorUtility.SaveFilePanelInProject("Save enemy team", "New Stage", "asset",
                                                               "Where to save the enemy team", DefaultFolder);
            if (string.IsNullOrEmpty(path)) return;

            // picked an existing stage: keep the asset (and everything pointing at it), swap its enemy team
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
            editor.WriteEnemyTeam(stage, team);

            EditorUtility.SetDirty(stage);
            AssetDatabase.SaveAssetIfDirty(stage);
            EditorGUIUtility.PingObject(stage);

            Debug.Log($"[StageEditor] saved {team.Count} enemies to {AssetDatabase.GetAssetPath(stage)}.");
        }

        private static bool TryReadTeam(out StageEditor editor, out List<HeroPlacement> team)
        {
            team = null;
            if (!TryGetEditor(out editor)) return false;

            team = editor.ReadEnemyTeam();
            if (team.Count > 0) return true;

            Debug.LogWarning("[StageEditor] there is no enemy on the board to save.");
            return false;
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
