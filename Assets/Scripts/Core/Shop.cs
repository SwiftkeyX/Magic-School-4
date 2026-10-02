using System;
using System.Collections.Generic;
using UnityEngine;
using MagicSchool.Contracts;
using MagicSchool.Combat.Heroes;
using MagicSchool.Combat.Placements;

namespace MagicSchool.Core
{
    // The shop's rules: what each slot is selling, what it costs, buying and refreshing.
    // It draws nothing - the shop panel shows whatever this says (see IShop).
    internal class Shop : IShop
    {
        private readonly IWallet _wallet;
        private readonly Bench _bench;
        private readonly IReadOnlyList<HeroDataSO> _roster;     // every hero the shop can roll
        private readonly HeroDataSO[] _stock;                   // what each slot is selling. 

        public int SlotCount => _stock.Length;
        public int Gold => _wallet.Gold;
        public int RefreshCost { get; }
        public event Action OnChanged;

        public Shop(IWallet wallet, Bench bench, IReadOnlyList<HeroDataSO> roster, int slotCount, int refreshCost)
        {
            _wallet = wallet;
            _bench = bench;
            _roster = roster ?? new List<HeroDataSO>();
            _stock = new HeroDataSO[slotCount];
            RefreshCost = refreshCost;

            _wallet.OnGoldChanged += _ => OnChanged?.Invoke();

            FillInOrder();
        }

        // ============================== IShop ==============================
        // Get the shop offer of the specify slot
        public ShopOffer OfferAt(int slot)
        {
            HeroDataSO data = _stock[slot];

            return data == null ? default : new ShopOffer(data.Name, data.Price);
        }

        public bool CanAfford(int slot) => _stock[slot] != null && _wallet.CanAfford(_stock[slot].Price);
        public bool CanAffordRefresh => _wallet.CanAfford(RefreshCost);

        public bool TryBuy(int slot)
        {
            HeroDataSO data = _stock[slot];
            if (data == null) return false;

            // can't buy hero because not enough money.
            if (!_wallet.CanAfford(data.Price))
            {
                Debug.Log($"Not enough gold for '{data.Name}' ({data.Price} g).");
                return false;
            }
 
            // if spawn hero don't work, no buy, return
            if (_bench == null || !_bench.SpawnHeroOnBench(data)) return false;

            // buy hero, empty the slot
            _stock[slot] = null;

            // pay money (its mirror image is the refund in HeroSeller.Sell)
            _wallet.TrySpend(data.Price);
            Debug.Log($"Bought hero '{data.Name}'.");

            return true;
        }

        // Re-roll every slot with a random hero. Costs gold.
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
