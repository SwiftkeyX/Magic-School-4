using UnityEngine;
using MagicSchool.Contracts;

namespace MagicSchool.Combat.Heroes
{
    internal static class CombatMath
    {
        // calculate final damage after mitigation
        // final damage = damage that use to to reduce the HP directly
        public static DamageOutcome ResolveDamage(int rawDamage, int defense, float damageReductionPercent, int currentHP)
        {
            int landed = DamageAfterMitigation(rawDamage, defense, damageReductionPercent);

            // HP stops at 0; whatever the hit was still worth past that is overkill, not damage
            int newHP = Mathf.Max(0, currentHP - landed);
            int lost = currentHP - newHP;

            return new DamageOutcome(newHP, lost, landed - lost, rawDamage - landed);
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
