using System.Collections.Generic;
using MagicSchool.Contracts;

namespace MagicSchool.Modifiers
{
    // A shield is a extra health that absorb damage before HP. Can expired on duration.
    internal static class Shield
    {
        // Get current total shield
        public static float Total(IReadOnlyList<ActiveCustomModifier> actives)
        {
            float total = 0f;
            foreach (ActiveCustomModifier active in actives) total += active.GetAmount(ModifierEnum.Shield);
            return total;
        }

        // Absorb damage with the active shields. 
        // The one expiring soon absorb first.
        // Returns how much damage was absorbed, the rest is the caller's to take HP off.
        public static float Absorb(IReadOnlyList<ActiveCustomModifier> actives, float damage)
        {
            if (damage <= 0f) return 0f;

            // find the soonest expired modifier
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
