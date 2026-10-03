using System;
using System.Collections.Generic;
using UnityEngine;
using MagicSchool.Contracts;
using MagicSchool.Combat.Heroes;
using MagicSchool.Combat.Placements;
using MagicSchool.Items;

namespace MagicSchool.Core
{
    // FIXLATER: move the shop logic into its own namespace and .asmdef
    // The shop's rules: what each slot is selling, what it costs, buying and refreshing.
    // It draws nothing - the shop panel shows whatever this says (see IShop).
    internal class Shop : IShop
    {
        private readonly IWallet _wallet;
        private readonly ShopOddsSO _odds;
        private readonly Func<int> _stageNumber;

        /// <summary>
        /// what the shop can roll.
        /// shop roll either heroes, or items.
        /// if it was heroes, the shop also considering tier of the hero 
        /// </summary>
        private readonly List<HeroEntry> _heroes = new List<HeroEntry>();
        private readonly List<ItemEntry> _items = new List<ItemEntry>();
        private readonly Dictionary<HeroTierEnum, List<HeroEntry>> _heroesByTier = new Dictionary<HeroTierEnum, List<HeroEntry>>();

        // what each slot is selling.
        private readonly IShopEntry[] _stock;

        public int SlotCount => _stock.Length;
        public int Gold => _wallet.Gold;
        public int RefreshCost { get; }
        public bool IsLocked { get; private set; }
        public event Action OnChanged;

        public Shop(IWallet wallet, Bench bench, IReadOnlyList<HeroDataSO> heroes, IReadOnlyList<ItemDataSO> items,
                    ShopOddsSO odds, Func<int> stageNumber, int slotCount, int refreshCost)
        {
            _wallet = wallet;
            _odds = odds;
            _stageNumber = stageNumber;
            _stock = new IShopEntry[slotCount];
            RefreshCost = refreshCost;

            _wallet.OnGoldChanged += _ => OnChanged?.Invoke();

            BuildRoster(bench, heroes, items);

            if (_odds != null && _odds.SlotCount != slotCount)
                Debug.LogWarning($"[Shop] ShopOdds has item chances for {_odds.SlotCount} slots but the shop has {slotCount} - a slot with no entry always rolls a hero.");
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

            // bought, empty the slot.
            // NOTE: Must come BEFORE paying because paying invoke a event
            _stock[slot] = null;

            // pay money (its mirror image is the refund in HeroSeller.Sell / ItemSeller.Sell)
            _wallet.TrySpend(entry.Price);
            Debug.Log($"Bought {entry.Kind} '{entry.Name}'.");

            return true;
        }

        // A new shop at the start of every stage. Free, unlike Refresh.
        public void Restock()
        {
            // if the shop is locked, return
            if (IsLocked) return;

            Roll();
            OnChanged?.Invoke();
        }

        // lock or unlock the shop
        // lock the shop prevent the shop from rerolling on itself.
        public void ToggleLock()
        {
            IsLocked = !IsLocked;
            OnChanged?.Invoke();
        }

        // Re-roll every slot. Costs gold.
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
        //      1) the randomized hero are divided into tiers.
        private void BuildRoster(Bench bench, IReadOnlyList<HeroDataSO> heroes, IReadOnlyList<ItemDataSO> items)
        {
            if (heroes != null)
            {
                foreach (HeroDataSO hero in heroes)
                {
                    if (hero == null) continue;

                    HeroEntry entry = new HeroEntry(hero, bench);
                    _heroes.Add(entry);

                    if (!_heroesByTier.TryGetValue(entry.Tier, out List<HeroEntry> tier))
                        _heroesByTier[entry.Tier] = tier = new List<HeroEntry>();
                    tier.Add(entry);
                }
            }

            if (items != null)
            {
                foreach (ItemDataSO item in items)
                    if (item != null && item.ItemId != ItemIdEnum.None) _items.Add(new ItemEntry(item));
            }

            if (_odds == null) Debug.LogError("[Shop] no ShopOdds assigned on GameManager - every slot will roll a hero of any tier.");
        }

        // randomize hero or item into every slot
        private void Roll()
        {
            for (int i = 0; i < _stock.Length; i++) _stock[i] = Randomized(i);
        }

        // randomize hero or item
        // each slot has its own item chance (see ShopOddsSO) - e.g. 0% = always a hero, 100% = always an item
        private IShopEntry Randomized(int slot)
        {
            bool rollsItem = _odds != null && _items.Count > 0 && UnityEngine.Random.Range(0, 100) < _odds.ItemChanceAt(slot);

            // roll the item into this slot
            if (rollsItem)
            {
                return PickFrom(_items);
            }

            // roll the hero into this slot
            if (TryRandomizeTier(out HeroTierEnum tier))
            {
                return PickFrom(_heroesByTier[tier]);
            }

            // fallback: the slot is empty
            return null;
        }

        // randomized using tier odd
        private bool TryRandomizeTier(out HeroTierEnum picked)
        {
            picked = default;

            // guard
            if (_heroes.Count == 0) return false;
            if (_odds == null) return false;

            // get current tiers odd e.g. common/uncommon/rare = 50%/30%/20%
            ShopOddsSO.TierOdds odds = _odds.OddsFor(_stageNumber());

            HeroTierEnum[] Tiers = { HeroTierEnum.Common, HeroTierEnum.Uncommon, HeroTierEnum.Rare };

            // get total weight
            int totalWeight = 0;
            foreach (HeroTierEnum tier in Tiers) totalWeight += StockedWeight(odds, tier);
            if (totalWeight <= 0) return false;

            // randomized using "weighted random selection"
            // Example for 50%/30%/20%,
            //      Common covers 0–49
            //      Uncommon covers 50–79
            //      and Rare covers 80-99
            int roll = UnityEngine.Random.Range(0, totalWeight);
            foreach (HeroTierEnum tier in Tiers)
            {
                roll -= StockedWeight(odds, tier);
                if (roll < 0)
                {
                    picked = tier;
                    return true;
                }
            }

            return false;
        }

        // a tier's weight this stage
        private int StockedWeight(ShopOddsSO.TierOdds odds, HeroTierEnum tier)
        {
            bool hasHeroes = _heroesByTier.TryGetValue(tier, out List<HeroEntry> heroes) && heroes.Count > 0;

            return hasHeroes ? odds.WeightOf(tier) : 0;
        }

        private static T PickFrom<T>(List<T> entries) where T : class
            => entries.Count == 0 ? null : entries[UnityEngine.Random.Range(0, entries.Count)];
    }
}
