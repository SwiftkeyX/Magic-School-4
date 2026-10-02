using UnityEngine;
using MagicSchool.Contracts;
using MagicSchool.Combat.Heroes;

namespace MagicSchool.Core
{
    internal class HeroSeller
    {
        private readonly IWallet _wallet;

        public HeroSeller(IWallet wallet)
        {
            _wallet = wallet;
        }

        public void Sell(ICombatant hero)
        {
            if (hero == null) return;

            // player get money
            if (hero is Hero sold) _wallet?.Earn(sold.Price);

            // reset var that depend on this hero
            IPlacement placement = hero.CurrentPlacement;
            if (placement != null) placement.OnUnitUnplaced(hero);
            hero.SetCurrentPlacement(null);
            hero.UntrackFromBoard();

            // delete the hero
            Object.Destroy(hero.transform.gameObject);
        }
    }
}
