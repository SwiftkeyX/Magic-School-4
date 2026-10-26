using UnityEngine;
using MagicSchool.Contracts;

namespace MagicSchool.Economy
{
    // Sell what player own. 
    // e.g.     heroes, items.
    public class Seller
    {
        private readonly IWallet _wallet;

        public Seller(IWallet wallet)
        {
            _wallet = wallet;
        }

        public void Sell(ISellable sellable)
        {
            if (sellable as Object == null) return;

            _wallet?.Earn(sellable.Price);
            Debug.Log($"Sold '{sellable.DisplayName}' for {sellable.Price} g.");

            if (sellable is ICombatant hero) Unplace(hero);

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
