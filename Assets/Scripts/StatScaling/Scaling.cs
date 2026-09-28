using System.Collections.Generic;
using MagicSchool.Contracts;
using UnityEngine;

namespace MagicSchool.StatScaling
{
    // Scaling Give answer to this question, "how much total stat do this scaling give?".
    // e.g. a modifier ask scaling the same question, so modifier itself know how much stat will it give to the hero.
    // 
    // read StatRatio.cs for how the ratio work.
    //
    // warnning: but it doesn't specify where the total stat go into.
    // HeroA's buff derived from atk, but it doesn't mean total stat have to be added into ATK,
    // the skill may specify to add the total stat into AS instead.
    // e.g. currently the ModifierEnum use to specify where the total stat go into, ModifierEnum.ATK/ModiferEnum.DF/etc...
    public static class Scaling
    {
        public static float Total(IReadOnlyList<StatRatio> ratios, IHeroStats stats)
        {
            // guard
            if (ratios == null || ratios.Count == 0) return 0f;

            float total = 0f;

            foreach (StatRatio ratio in ratios)
            {
                // flat scaling - the number stands on its own
                if (ratio.IsFlat)
                {
                    total += ratio.Amount;
                    continue;
                }

                // percentage scaling
                else
                {
                    // guard
                    if (stats == null)
                    {
                        Debug.LogError($"[Scaling] a ratio scales off {ratio.Stat.Value} but was asked without any stats " +
                                       "to read it from. SkillDefinition.Init() has to reach every effect it holds.");
                        continue;
                    }

                    total += PartOf(ratio, stats) * ratio.Amount / 100f;
                }
            }

            return total;
        }

        // scaling can be derived from the stat using these 3 type, [Total/Base/Bonus] stat 
        private static float PartOf(StatRatio ratio, IHeroStats stats)
        {
            StatEnum stat = ratio.Stat.Value;

            switch (ratio.ScaleFrom)
            {
                // scale of the base stat
                case ScaleFromEnum.Base:
                    return stats.GetBaseStat(stat);

                // scale of the bonus stat
                case ScaleFromEnum.Bonus:
                    return stats.GetStat(stat) - stats.GetBaseStat(stat);

                // scale of the total stat 
                default:
                    return stats.GetStat(stat);
            }
        }
    }
}
