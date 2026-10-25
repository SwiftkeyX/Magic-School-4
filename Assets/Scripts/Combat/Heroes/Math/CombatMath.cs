using UnityEngine;
using MagicSchool.Contracts;

namespace MagicSchool.Combat.Heroes
{
    internal static class CombatMath
    {
        // calculate final damage after mitigation
        // final damage = damage that use to to reduce the HP directly
        public static DamageOutcome ResolveDamage(int rawDamage, int defense, float damageReductionPercent, int currentHP,
                                                  int shield, out int shieldAbsorbed)
        {
            int mitigated = DamageAfterMitigation(rawDamage, defense, damageReductionPercent);

            // the shield absorb damage before HP
            shieldAbsorbed = Mathf.Min(mitigated, Mathf.Max(0, shield));
            int toHP = mitigated - shieldAbsorbed;

            // HP stops at 0; whatever the hit was still worth past that is overkill, not damage
            int newHP = Mathf.Max(0, currentHP - toHP);
            int lost = currentHP - newHP;

            return new DamageOutcome(newHP, lost, toHP - lost, rawDamage - mitigated);
        }

        // calculate final heal
        // final heal = amount of HP increase directly
        public static HealOutcome ResolveHeal(float amount, bool isWounded, int currentHP, int maxHP)
        {
            int incoming = Mathf.RoundToInt(amount);
            int healed = HealAfterMitigation(amount, isWounded);

            // HP stops at MaxHP; the rest is overheal
            int newHP = Mathf.Min(maxHP, currentHP + healed);
            int gained = newHP - currentHP;

            return new HealOutcome(newHP, gained, healed - gained, incoming - healed);
        }

        // The DF a hit from this attacker meets, LoL's way: the % armor penetration first, then the flat lethality.
        // Penetration never takes DF below 0, and DF already below 0 (shredded) is left as it is.
        // e.g.     100 DF, 40% pen, 15 lethality => 100 * 0.6 - 15 = 45 DF
        //          20 DF, 15 lethality           => 5 DF;   10 DF, 15 lethality => 0, not -5
        public static int DFAfterPenetration(int defense, float armorPenPercent, float lethality)
        {
            if (defense <= 0) return defense;

            float pen = Mathf.Clamp(armorPenPercent, 0f, 100f);     // two pen items add up; past 100% would be negative DF
            float df = defense * (1f - pen / 100f) - Mathf.Max(0f, lethality);
            return Mathf.Max(0, Mathf.RoundToInt(df));
        }

        // ======================================== private ========================================
        private static int DamageAfterMitigation(int rawDamage, int defense, float damageReductionPercent)
        {
            // Effective health pool formula: EHP = HP * (1 + DF / 100)
            float mitigated = rawDamage / (1f + defense / 100f);

            // damage reduction will reduce the damage by percentage 
            mitigated *= 1f - damageReductionPercent / 100f;

            return Mathf.RoundToInt(mitigated);
        }

        private static int HealAfterMitigation(float amount, bool isWounded)
        {
            // wound reduce healing by 50%
            float healed = isWounded ? amount * 0.5f : amount;
            return Mathf.RoundToInt(healed);
        }
    }
}
