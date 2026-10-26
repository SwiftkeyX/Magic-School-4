using System.Collections.Generic;
using MagicSchool.Contracts;

namespace MagicSchool.Modifiers
{
    // A shield is a extra health that absorb damage before HP. Can expired on duration.
    // The shield expiring soonest absorbs first; what is left of the hit is the caller's to take off HP.
    internal static class Shield
    {
        public static float Total(IReadOnlyList<ActiveCustomModifier> actives)
        {
            float total = 0f;
            foreach (ActiveCustomModifier active in actives) total += active.GetAmount(ModifierEnum.Shield);
            return total;
        }

        public static float Absorb(IReadOnlyList<ActiveCustomModifier> actives, float damage)
        {
            if (damage <= 0f) return 0f;

            var bySoonest = new List<ActiveCustomModifier>(actives);
            bySoonest.Sort((a, b) => a.Remaining.CompareTo(b.Remaining));

            float left = damage;
            foreach (ActiveCustomModifier active in bySoonest)
            {
                if (left <= 0f) break;
                left -= active.ConsumeShield(left);
            }

            return damage - left;
        }
    }
}
