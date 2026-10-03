using MagicSchool.Contracts;
using MagicSchool.Combat.Heroes;
using MagicSchool.Combat.Placements;
using MagicSchool.Items;

namespace MagicSchool.Core
{
    // One thing a shop slot can sell. 
    internal interface IShopEntry
    {
        string Name { get; }
        int Price { get; }
        ShopOfferKindEnum Kind { get; }

        // hand the bought thing to the player. 
        bool TryDeliver();
    }

    // a hero spawn to the bench 
    internal class HeroEntry : IShopEntry
    {
        private readonly HeroDataSO _data;
        private readonly Bench _bench;

        public HeroEntry(HeroDataSO data, Bench bench)
        {
            _data = data;
            _bench = bench;
        }

        public string Name => _data.Name;
        public int Price => _data.Price;
        public ShopOfferKindEnum Kind => ShopOfferKindEnum.Hero;
        public HeroTierEnum Tier => _data.Tier;     

        public bool TryDeliver() => _bench != null && _bench.SpawnHeroOnBench(_data);
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
    }
}
