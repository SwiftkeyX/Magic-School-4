using UnityEngine;
using MagicSchool.Contracts;

namespace MagicSchool.Core
{
    // Sell what player own. 
    // e.g.     heroes, items.
    internal class Seller
    {
        private readonly IWallet _wallet;

        public Seller(IWallet wallet)
        {
            _wallet = wallet;
        }

        public void Sell(ISellable sellable)
        {
            // `as Object` because == on an interface-typed reference misses Unity's destroyed objects
            if (sellable as Object == null) return;

            // player get money
            _wallet?.Earn(sellable.Price);
            Debug.Log($"Sold '{sellable.DisplayName}' for {sellable.Price} g.");

            // if hero is what player sell, reset its var too. 
            if (sellable is ICombatant hero) Unplace(hero);

            // delete it
            Object.Destroy(sellable.transform.gameObject);
        }

        private static void Unplace(ICombatant hero)
        {
            IPlacement placement = hero.CurrentPlacement;
            if (placement != null) placement.OnUnitUnplaced(hero);
            hero.SetCurrentPlacement(null);
            hero.UntrackFromBoard();
        }
    }
}
