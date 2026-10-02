using MagicSchool.Contracts;

namespace MagicSchool.Combat.Heroes
{
    // What a hero costs in the shop, and what selling it gives back. The price comes from the tier alone.
    public static class HeroPrice
    {
        public static int Of(HeroTierEnum tier)
        {
            switch (tier)
            {
                case HeroTierEnum.Common: return 1;
                case HeroTierEnum.Uncommon: return 2;
                case HeroTierEnum.Rare: return 3;
                default: return 1;
            }
        }
    }
}
