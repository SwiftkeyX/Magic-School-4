using System;
using System.Collections.Generic;
using UnityEngine;
using MagicSchool.Contracts;
using MagicSchool.Combat.Heroes;
using MagicSchool.Combat.Placements;
using MagicSchool.Items;

namespace MagicSchool.Core
{
    // The shop's rules: what each slot is selling, what it costs, buying and refreshing.
    // It draws nothing - the shop panel shows whatever this says (see IShop).
    internal class Shop : IShop
    {
        private readonly IWallet _wallet;
        private readonly List<IShopEntry> _roster = new List<IShopEntry>();   // every hero and item the shop can roll
        private readonly IShopEntry[] _stock;                                 // what each slot is selling.

        public int SlotCount => _stock.Length;
        public int Gold => _wallet.Gold;
        public int RefreshCost { get; }
        public event Action OnChanged;

        public Shop(IWallet wallet, Bench bench, IReadOnlyList<HeroDataSO> heroes, IReadOnlyList<ItemDataSO> items, int slotCount, int refreshCost)
        {
            _wallet = wallet;
            _stock = new IShopEntry[slotCount];
            RefreshCost = refreshCost;

            _wallet.OnGoldChanged += _ => OnChanged?.Invoke();

            BuildRoster(bench, heroes, items);
            FillInOrder();
        }

        // ============================== IShop ==============================
        // Get the shop offer of the specify slot
        public ShopOffer OfferAt(int slot)
        {
            IShopEntry entry = _stock[slot];

            return entry == null ? default : new ShopOffer(entry.Name, entry.Price, entry.Kind);
        }

        public bool CanAfford(int slot) => _stock[slot] != null && _wallet.CanAfford(_stock[slot].Price);
        public bool CanAffordRefresh => _wallet.CanAfford(RefreshCost);

        public bool TryBuy(int slot)
        {
            IShopEntry entry = _stock[slot];
            if (entry == null) return false;

            // can't buy because not enough money.
            if (!_wallet.CanAfford(entry.Price))
            {
                Debug.Log($"Not enough gold for '{entry.Name}' ({entry.Price} g).");
                return false;
            }

            // if it has nowhere to go, no buy, return
            // e.g. a hero with the bench full, can't buy
            if (!entry.TryDeliver()) return false;

            // pay money (its mirror image is the refund in HeroSeller.Sell / ItemSeller.Sell)
            _wallet.TrySpend(entry.Price);
            Debug.Log($"Bought {entry.Kind} '{entry.Name}'.");
            
            // bought, empty the slot
            _stock[slot] = null;

            return true;
        }

        // Re-roll every slot with a random hero or item. Costs gold.
        public bool TryRefresh()
        {
            if (!CanAffordRefresh)
            {
                Debug.Log($"Not enough gold to refresh the shop ({RefreshCost} g).");
                return false;
            }

            Roll();
            _wallet.TrySpend(RefreshCost);

            return true;
        }

        // ============================== private ==============================
        // Initialize the roster that shop going to randomize from 
        // FIXLATER: uniform over the pool for now - hero-vs-item odds and tier odds are the roll rules' job.
        private void BuildRoster(Bench bench, IReadOnlyList<HeroDataSO> heroes, IReadOnlyList<ItemDataSO> items)
        {
            if (heroes != null)
                foreach (HeroDataSO hero in heroes)
                    if (hero != null) _roster.Add(new HeroEntry(hero, bench));

            if (items != null)
                foreach (ItemDataSO item in items)
                    if (item != null && item.ItemId != ItemIdEnum.None) _roster.Add(new ItemEntry(item));
        }

        // init the shop offer (stock)
        private void FillInOrder()
        {
            if (_roster.Count < _stock.Length) Debug.LogError("The shop's roster is not enough for all of its slots");

            for (int i = 0; i < _stock.Length && i < _roster.Count; i++) _stock[i] = _roster[i];
        }

        private void Roll()
        {
            if (_roster.Count == 0) return;

            for (int i = 0; i < _stock.Length; i++)
                _stock[i] = _roster[UnityEngine.Random.Range(0, _roster.Count)];
        }
    }
}
