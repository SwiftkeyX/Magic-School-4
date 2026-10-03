using UnityEngine;
using MagicSchool.Contracts;
using MagicSchool.Items;

namespace MagicSchool.Core
{
    // counterpart to HeroSeller.cs
    // FIXLATER: Group ItemSellfer and HeroSeller together using ISellable.cs
    internal class ItemSeller
    {
        private readonly IWallet _wallet;

        public ItemSeller(IWallet wallet)
        {
            _wallet = wallet;
        }

        public void Sell(Item item)
        {
            if (item == null) return;

            _wallet?.Earn(item.Price);
            Debug.Log($"Sold item '{item.DisplayName}' for {item.Price} g.");

            Object.Destroy(item.gameObject);
        }
    }
}
