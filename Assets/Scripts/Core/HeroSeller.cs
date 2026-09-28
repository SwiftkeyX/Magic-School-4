using UnityEngine;
using MagicSchool.Contracts;

namespace MagicSchool.Core
{
    internal class HeroSeller
    {
        public void Sell(ICombatant hero)
        {
            if (hero == null) return;

            // BLOCKED on: the gold/economy system. => Put refund gold logic here once it exists.
            // (its mirror image is the spend in ShopPanelController.ResolveDrop)
            // ...

            IPlacement placement = hero.CurrentPlacement;
            if (placement != null) placement.OnUnitUnplaced(hero);
            hero.SetCurrentPlacement(null);

            // untrack from the board, since the hero is no longer exist on the team.
            hero.UntrackFromBoard();

            Object.Destroy(hero.transform.gameObject);
        }
    }
}
