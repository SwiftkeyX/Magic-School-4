using UnityEngine;

namespace MagicSchool.Items
{
    // When item was spawn through:
    // e.g.     buying item at the shop
    //          choosing item at the reward
    // item are placed in the world position using ItemDrop.cs. The item are spawn next to each other.
    public static class ItemDrop
    {
        private static readonly Vector3 Origin = new Vector3(-5.6f, -2f, 0f);
        private const float Spacing = 0.6f;        // how far along x each further item lands
        private const int MaxSpots = 20;           // past this the row runs off screen; stack on the last spot instead

        public static Item Spawn(ItemDataSO data) => Item.Spawn(data, FreeSpot());

        private static Vector3 FreeSpot()
        {
            for (int i = 0; i < MaxSpots; i++)
            {
                Vector3 spot = Origin + new Vector3(i * Spacing, 0f, 0f);
                if (!HasLooseItemAt(spot)) return spot;
            }

            return Origin + new Vector3((MaxSpots - 1) * Spacing, 0f, 0f);
        }

        private static bool HasLooseItemAt(Vector3 spot)
        {
            foreach (Item item in Object.FindObjectsByType<Item>(FindObjectsSortMode.None))
            {
                if (item.transform.parent != null) continue;

                Vector2 offset = item.transform.position - spot;
                if (offset.sqrMagnitude < Spacing * Spacing * 0.25f) return true;
            }

            return false;
        }
    }
}
