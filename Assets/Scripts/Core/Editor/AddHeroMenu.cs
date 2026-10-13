using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using MagicSchool.Combat.Heroes;
using MagicSchool.Core;

namespace MagicSchool.Editor
{
    // Magic School 4 > Add Selected Heroes To Bench
    // Dev tool: put the HeroDataSOs selected in the Project window on the player's bench, for free.
    // Solved problem that testing one hero meant re-rolling the shop until it showed up.
    // Works for any hero, the enemy-only ones too. Needs Play mode.
    internal static class AddHeroMenu
    {
        public static bool CanAdd => EditorApplication.isPlaying && GameManager.Instance != null;

        [MenuItem("Magic School 4/Add Selected Heroes To Bench")]
        private static void AddSelected()
        {
            List<HeroDataSO> heroes = Selection.objects.OfType<HeroDataSO>().ToList();
            if (heroes.Count == 0)
            {
                Debug.LogWarning("[AddHero] select one or more HeroDataSO in the Project window first.");
                return;
            }

            foreach (HeroDataSO hero in heroes)
            {
                if (!Add(hero)) return;
            }
        }

        // say why, rather than fail with no explanation
        public static bool Add(HeroDataSO hero)
        {
            if (!CanAdd)
            {
                Debug.LogWarning("[AddHero] enter Play mode first.");
                return false;
            }

            if (GameManager.Instance.AddHeroToBench(hero)) return true;

            Debug.LogWarning($"[AddHero] no free bench slot left for {hero.Name}.");
            return false;
        }
    }
}
