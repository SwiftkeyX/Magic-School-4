using UnityEngine;
using MagicSchool.Contracts;

namespace MagicSchool.Combat.Heroes
{
    // Damage: DF divides it (damage / (1 + DF/100), so EHP = HP * (1 + DF/100)), Damage Reduction takes its percent, a
    // shield absorbs before HP, and what falls past 0 HP is overkill, not damage. Healing past max HP is overheal;
    // Wound halves it.
    // DFAfterPenetration is LoL's: the % armor penetration first (sources add, clamped at 100), then flat lethality;
    // it never takes DF below 0 and leaves DF already below 0 (shredded) alone. 100 DF, 40% pen, 15 lethality => 45;
    // 10 DF, 15 lethality => 0, not -5.
    internal static class CombatMath
    {
        public static DamageOutcome ResolveDamage(int rawDamage, int defense, float damageReductionPercent, int currentHP,
                                                  int shield, out int shieldAbsorbed)
        {
            int mitigated = DamageAfterMitigation(rawDamage, defense, damageReductionPercent);

            shieldAbsorbed = Mathf.Min(mitigated, Mathf.Max(0, shield));
            int toHP = mitigated - shieldAbsorbed;

            int newHP = Mathf.Max(0, currentHP - toHP);
            int lost = currentHP - newHP;

            return new DamageOutcome(newHP, lost, toHP - lost, rawDamage - mitigated);
        }

        public static HealOutcome ResolveHeal(float amount, bool isWounded, int currentHP, int maxHP)
        {
            int incoming = Mathf.RoundToInt(amount);
            int healed = HealAfterMitigation(amount, isWounded);

            int newHP = Mathf.Min(maxHP, currentHP + healed);
            int gained = newHP - currentHP;

            return new HealOutcome(newHP, gained, healed - gained, incoming - healed);
        }

        public static int DFAfterPenetration(int defense, float armorPenPercent, float lethality)
        {
            if (defense <= 0) return defense;

            float pen = Mathf.Clamp(armorPenPercent, 0f, 100f);
            float df = defense * (1f - pen / 100f) - Mathf.Max(0f, lethality);
            return Mathf.Max(0, Mathf.RoundToInt(df));
        }

        private static int DamageAfterMitigation(int rawDamage, int defense, float damageReductionPercent)
        {
            float mitigated = rawDamage / (1f + defense / 100f);

            mitigated *= 1f - damageReductionPercent / 100f;

            return Mathf.RoundToInt(mitigated);
        }

        private static int HealAfterMitigation(float amount, bool isWounded)
        {
            float healed = isWounded ? amount * 0.5f : amount;
            return Mathf.RoundToInt(healed);
        }
    }
}
