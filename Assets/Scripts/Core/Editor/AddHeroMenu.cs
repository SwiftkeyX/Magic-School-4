using UnityEditor;
using UnityEngine;
using MagicSchool.Combat.Heroes;
using MagicSchool.Core;

namespace MagicSchool.Editor
{
    // Dev tool: put a hero on the player's bench, or an item on the loose row, for free - the Stage Editor's Player tab (a
    // Bench button per hero, a Drop button per item) and the Align Editor's "Add to bench" button. It used to be a menu item
    // on the Project window's selection.
    // Solved problem that testing one hero or item meant re-rolling the shop until it showed up.
    // Works for any hero, the enemy-only ones too. Needs Play mode.
    internal static class AddHeroMenu
    {
        public static bool CanAdd => EditorApplication.isPlaying && GameManager.Instance != null;

        public static bool Add(HeroDataSO hero)
        {
            if (!CanAdd)
            {
                Debug.LogWarning("[AddHero] enter Play mode first.");
                return false;
            }

            if (GameManager.Instance.Actions.AddHeroToBench(hero)) return true;

            Debug.LogWarning($"[AddHero] no free bench slot left for {hero.Name}.");
            return false;
        }

        public static bool AddItem(ScriptableObject item)
        {
            if (!CanAdd)
            {
                Debug.LogWarning("[AddItem] enter Play mode first.");
                return false;
            }

            if (GameManager.Instance.Actions.AddItemToLooseRow(item)) return true;

            Debug.LogWarning($"[AddItem] could not drop {(item != null ? item.name : "nothing")} - is it an ItemDataSO?");
            return false;
        }
    }
}
