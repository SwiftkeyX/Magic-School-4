using System;
using System.Collections.Generic;
using UnityEngine;
using MagicSchool.Contracts;
using MagicSchool.Combat.Heroes;
using MagicSchool.Combat.Placements;
using MagicSchool.Items;

namespace MagicSchool.Economy
{
    // The shop's rules: what each slot is selling, what it costs, buying and refreshing.
    // It draws nothing - the shop panel shows whatever this says (see IShop).
    // A bought slot is emptied before paying: paying raises OnChanged, and the panel must redraw the slot already
    // empty.
    public class Shop : IShop
    {
        private readonly IWallet _wallet;
        private readonly ShopOddsSO _odds;
        private readonly Func<int> _stageNumber;

        private readonly List<HeroEntry> _heroes = new List<HeroEntry>();
        private readonly List<ItemEntry> _items = new List<ItemEntry>();
        private readonly Dictionary<HeroTierEnum, List<HeroEntry>> _heroesByTier = new Dictionary<HeroTierEnum, List<HeroEntry>>();

        private readonly IShopEntry[] _stock;

        public int SlotCount => _stock.Length;
        public int Gold => _wallet.Gold;
        public int RefreshCost { get; }
        public bool IsLocked { get; private set; }
        public event Action OnChanged;

        public event Action<int, HeroDataSO> OnHeroPickedUp;

        public Shop(IWallet wallet, Bench bench, IReadOnlyList<HeroDataSO> heroes, IReadOnlyList<ItemDataSO> items,
                    ShopOddsSO odds, Func<int> stageNumber, int slotCount, int refreshCost,
                    Func<HeroDataSO, Func<bool>, IInspectable> heroPreview)
        {
            _wallet = wallet;
            _odds = odds;
            _stageNumber = stageNumber;
            _stock = new IShopEntry[slotCount];
            RefreshCost = refreshCost;

            _wallet.OnGoldChanged += _ => OnChanged?.Invoke();

            BuildRoster(bench, heroes, items, heroPreview);

            if (_odds != null && _odds.SlotCount != slotCount)
                Debug.LogWarning($"[Shop] ShopOdds has item chances for {_odds.SlotCount} slots but the shop has {slotCount} - a slot with no entry always rolls a hero.");
        }

        public ShopOffer OfferAt(int slot)
        {
            IShopEntry entry = _stock[slot];

            return entry == null ? default : new ShopOffer(entry.Name, entry.Price, entry.Kind);
        }

        public IInspectable InspectableAt(int slot)
        {
            IShopEntry entry = _stock[slot];
            if (entry == null) return null;

            Func<bool> isNotEmpty = () => _stock[slot] == entry;
            return entry.Preview(isNotEmpty);
        }

        public bool CanAfford(int slot) => _stock[slot] != null && _wallet.CanAfford(_stock[slot].Price);
        public bool CanAffordRefresh => _wallet.CanAfford(RefreshCost);

        public bool TryBuy(int slot)
        {
            IShopEntry entry = _stock[slot];
            if (entry == null) return false;

            if (!_wallet.CanAfford(entry.Price))
            {
                Debug.Log($"Not enough gold for '{entry.Name}' ({entry.Price} g).");
                return false;
            }

            if (!entry.TryDeliver()) return false;

            _stock[slot] = null;

            _wallet.TrySpend(entry.Price);
            Debug.Log($"Bought {entry.Kind} '{entry.Name}'.");

            return true;
        }

        public bool TryPickUp(int slot)
        {
            if (!(_stock[slot] is HeroEntry entry)) return false;

            if (!_wallet.CanAfford(entry.Price))
            {
                Debug.Log($"Not enough gold for '{entry.Name}' ({entry.Price} g).");
                return false;
            }

            OnHeroPickedUp?.Invoke(slot, entry.Data);
            return true;
        }

        public bool TryBuyPickedUp(int slot, HeroDataSO hero)
        {
            if (!(_stock[slot] is HeroEntry entry) || entry.Data != hero) return false;
            if (!_wallet.CanAfford(entry.Price)) return false;

            _stock[slot] = null;

            _wallet.TrySpend(entry.Price);
            Debug.Log($"Bought {entry.Kind} '{entry.Name}'.");

            return true;
        }

        public void Restock()
        {
            if (IsLocked) return;

            Roll();
            OnChanged?.Invoke();
        }

        public void ToggleLock()
        {
            IsLocked = !IsLocked;
            OnChanged?.Invoke();
        }

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

        private void BuildRoster(Bench bench, IReadOnlyList<HeroDataSO> heroes, IReadOnlyList<ItemDataSO> items,
                                 Func<HeroDataSO, Func<bool>, IInspectable> heroPreview)
        {
            if (heroes != null)
            {
                foreach (HeroDataSO hero in heroes)
                {
                    if (hero == null) continue;

                    HeroEntry entry = new HeroEntry(hero, bench, heroPreview);
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

        private void Roll()
        {
            for (int i = 0; i < _stock.Length; i++) _stock[i] = Randomized(i);
        }

        private IShopEntry Randomized(int slot)
        {
            bool rollsItem = _odds != null && _items.Count > 0 && UnityEngine.Random.Range(0, 100) < _odds.ItemChanceAt(slot);

            if (rollsItem)
            {
                return PickFrom(_items);
            }

            if (TryRandomizeTier(out HeroTierEnum tier))
            {
                return PickFrom(_heroesByTier[tier]);
            }

            return null;
        }

        private bool TryRandomizeTier(out HeroTierEnum picked)
        {
            picked = default;

            if (_heroes.Count == 0) return false;
            if (_odds == null) return false;

            ShopOddsSO.TierOdds odds = _odds.OddsFor(_stageNumber());

            HeroTierEnum[] Tiers = { HeroTierEnum.Common, HeroTierEnum.Uncommon, HeroTierEnum.Rare };

            int totalWeight = 0;
            foreach (HeroTierEnum tier in Tiers) totalWeight += StockedWeight(odds, tier);
            if (totalWeight <= 0) return false;

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

        private int StockedWeight(ShopOddsSO.TierOdds odds, HeroTierEnum tier)
        {
            bool hasHeroes = _heroesByTier.TryGetValue(tier, out List<HeroEntry> heroes) && heroes.Count > 0;

            return hasHeroes ? odds.WeightOf(tier) : 0;
        }

        private static T PickFrom<T>(List<T> entries) where T : class
            => entries.Count == 0 ? null : entries[UnityEngine.Random.Range(0, entries.Count)];
    }
}
