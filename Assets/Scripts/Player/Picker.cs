using UnityEngine;

namespace MagicSchool.Player
{
    // Picker answer: what is the pointer point at? 
    internal static class Picker
    {
        // context: OverlapPointAll returns every collider at the point
        // At() to ensure the picker return the right type that asker wanted.
        public static T At<T>(Vector3 worldPos) where T : class
        {
            Collider2D[] hits = Physics2D.OverlapPointAll(worldPos);

            foreach (Collider2D hit in hits)
            {
                T found = hit.GetComponent<T>();
                if (found != null) return found;
            }

            return null;
        }
    }
}
