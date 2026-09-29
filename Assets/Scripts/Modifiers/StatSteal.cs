using System.Collections.Generic;
using MagicSchool.Contracts;
using MagicSchool.StatScaling;

namespace MagicSchool.Modifiers
{
    // steal stat from the victim 
    // e.g.     Take(me, victim, 10%, 10s, ATK) = I steal your 10% of your ATK for 10s.
    public static class StatSteal
    {
        private static readonly Dictionary<StatEnum, ModifierEnum> Stealable = new Dictionary<StatEnum, ModifierEnum>
        {
            { StatEnum.MaxHP, ModifierEnum.BonusHP },
            { StatEnum.ATK  , ModifierEnum.ATK     },
            { StatEnum.DF   , ModifierEnum.DF      },
            { StatEnum.AP   , ModifierEnum.AP      },
            { StatEnum.MR   , ModifierEnum.MR      },
            { StatEnum.AS   , ModifierEnum.AS      },
        };

        public static void Take(ICombatant thief, ICombatant victim, float percent, float duration, params StatEnum[] stats)
        {
            if (thief == null || victim == null || !victim.IsAlive || !(victim is IHeroStats victimStats)) return;

            var taken = new List<IModifier>();
            var given = new List<IModifier>();

            foreach (StatEnum stat in stats)
            {
                if (!Stealable.TryGetValue(stat, out ModifierEnum modifier)) continue;

                float amount = victimStats.GetStat(stat) * percent / 100f;
                if (amount <= 0f) continue;

                taken.Add(new StatModifier(modifier, new[] { new StatRatio(-amount) }));
                given.Add(new StatModifier(modifier, new[] { new StatRatio(amount) }));
            }

            if (taken.Count == 0) return;

            IHeroStats thiefStats = thief as IHeroStats;
            victim.AddModifier(new CustomModifier(duration, taken), thiefStats);
            thief.AddModifier(new CustomModifier(duration, given), thiefStats);
        }
    }
}
