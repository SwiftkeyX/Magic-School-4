using System;
using MagicSchool.Contracts;
using MagicSchool.Combat.Heroes;
using MagicSchool.Combat.Placements;
using MagicSchool.Items;

namespace MagicSchool.Economy
{
    // a shop slot: can be a heroes, items 
    internal interface IShopEntry
    {
        string Name { get; }
        int Price { get; }
        ShopOfferKindEnum Kind { get; }

        // hand the bought thing to the player. 
        bool TryDeliver();

        // return inspectable of this slot
        IInspectable Preview(Func<bool> isStillOffered);
    }

    // a hero spawn to the bench 
    internal class HeroEntry : IShopEntry
    {
        private readonly HeroDataSO _data;
        private readonly Bench _bench;
        private readonly Func<HeroDataSO, Func<bool>, IInspectable> _preview;    

        public HeroEntry(HeroDataSO data, Bench bench, Func<HeroDataSO, Func<bool>, IInspectable> preview)
        {
            _data = data;
            _bench = bench;
            _preview = preview;
        }

        public string Name => _data.Name;
        public int Price => _data.Price;
        public ShopOfferKindEnum Kind => ShopOfferKindEnum.Hero;
        public HeroTierEnum Tier => _data.Tier;     

        public bool TryDeliver() => _bench != null && _bench.SpawnHeroOnBench(_data);
        public IInspectable Preview(Func<bool> isStillOffered) => _preview?.Invoke(_data, isStillOffered);
    }

    // an item spawn in the world
    internal class ItemEntry : IShopEntry
    {
        private readonly ItemDataSO _data;

        public ItemEntry(ItemDataSO data)
        {
            _data = data;
        }

        public string Name => _data.Name;
        public int Price => _data.Price;
        public ShopOfferKindEnum Kind => ShopOfferKindEnum.Item;

        public bool TryDeliver() => ItemDrop.Spawn(_data) != null;
        public IInspectable Preview(Func<bool> isStillOffered) => new ItemPreview(_data, isStillOffered);
    }
}
