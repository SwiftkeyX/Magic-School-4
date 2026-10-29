using System;
using System.Collections.Generic;
using MagicSchool.Contracts;
using MagicSchool.StatScaling;

namespace MagicSchool.Items
{
    // Simple items: each gives one stat, in three rarities. Rare is about what the old item of that stat gave; Common is
    // 0.5x Rare, Epic 1.75x (Whetstone's 20 / 40 / 70). Keeping items this plain makes them easy to balance while
    // heroes and marks are still being tuned (Notion: Power level -> Takeaway 10).
    //
    //   Whetstone      +20 / 40 / 70% of base Atk
    //   Archmage Tome  +20 / 40 / 70 AP
    //   Iron Plate     +15 / 30 / 50 DF
    //   Oaken Charm    +150 / 300 / 500 HP
    //   Hunter's Cord  +0.15 / 0.30 / 0.50 Attack Speed
    //   Starwell Orb   +1 / 2 / 3.5 Mana per second
    //   Lucky Coin     +10 / 20 / 35% Crit Chance
    internal static class StatItems
    {
        internal static readonly Dictionary<ItemIdEnum, Func<ICustomModifier>> Builders =
            new Dictionary<ItemIdEnum, Func<ICustomModifier>>
            {
                { ItemIdEnum.CommonWhetstone,    () => AtkPercent(20f) },
                { ItemIdEnum.RareWhetstone,      () => AtkPercent(40f) },
                { ItemIdEnum.EpicWhetstone,      () => AtkPercent(70f) },

                { ItemIdEnum.CommonArchmageTome, () => Flat(ModifierEnum.AP, 20f) },
                { ItemIdEnum.RareArchmageTome,   () => Flat(ModifierEnum.AP, 40f) },
                { ItemIdEnum.EpicArchmageTome,   () => Flat(ModifierEnum.AP, 70f) },

                { ItemIdEnum.CommonIronPlate,    () => Flat(ModifierEnum.DF, 15f) },
                { ItemIdEnum.RareIronPlate,      () => Flat(ModifierEnum.DF, 30f) },
                { ItemIdEnum.EpicIronPlate,      () => Flat(ModifierEnum.DF, 50f) },

                { ItemIdEnum.CommonOakenCharm,   () => Flat(ModifierEnum.BonusHP, 150f) },
                { ItemIdEnum.RareOakenCharm,     () => Flat(ModifierEnum.BonusHP, 300f) },
                { ItemIdEnum.EpicOakenCharm,     () => Flat(ModifierEnum.BonusHP, 500f) },

                { ItemIdEnum.CommonHuntersCord,  () => Flat(ModifierEnum.AS, 0.15f) },
                { ItemIdEnum.RareHuntersCord,    () => Flat(ModifierEnum.AS, 0.30f) },
                { ItemIdEnum.EpicHuntersCord,    () => Flat(ModifierEnum.AS, 0.50f) },

                { ItemIdEnum.CommonStarwellOrb,  () => Flat(ModifierEnum.ManaPerSecond, 1f) },
                { ItemIdEnum.RareStarwellOrb,    () => Flat(ModifierEnum.ManaPerSecond, 2f) },
                { ItemIdEnum.EpicStarwellOrb,    () => Flat(ModifierEnum.ManaPerSecond, 3.5f) },

                { ItemIdEnum.CommonLuckyCoin,    () => Flat(ModifierEnum.CritChance, 10f) },
                { ItemIdEnum.RareLuckyCoin,      () => Flat(ModifierEnum.CritChance, 20f) },
                { ItemIdEnum.EpicLuckyCoin,      () => Flat(ModifierEnum.CritChance, 35f) },
            };

        private static ICustomModifier Flat(ModifierEnum stat, float amount)
            => ItemFactory.Bundle(ItemFactory.Buff(stat, amount));

        private static ICustomModifier AtkPercent(float percent)
            => ItemFactory.Bundle(ItemFactory.Buff(ModifierEnum.ATK, new StatRatio(StatEnum.ATK, percent, ScaleFromEnum.Base)));
    }
}
