using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using MagicSchool.Contracts;

namespace MagicSchool.Combat.Placements
{
    /// <summary>
    /// The items lying on the board's hexes. An item on a hex takes the hex: no hero can be placed there.
    /// The board only knows "something a hero can wear" (IEquipment), never the Item type itself.
    /// What an item on a hex does is the aligning mechanic's job (See AlignBonuses): a hero wears the
    /// items lying on its wearing hexes.
    /// </summary>
    public class HexItems
    {
        private readonly Dictionary<Hex, IEquipment> _itemOn = new Dictionary<Hex, IEquipment>();

        // ======================================== getter ========================================
        // Which item lies on this hex? Null when none does.
        public IEquipment ItemOn(Hex hex)
        {
            if (hex == null || !_itemOn.TryGetValue(hex, out IEquipment item)) return null;

            // an item destroyed while it lay here. `as Object` because == on an interface-typed
            // reference misses Unity's destroyed objects.
            if (item as Object != null) return item;

            _itemOn.Remove(hex);
            return null;
        }

        // Which hex does this item lie on? Null when it is not on the board.
        public Hex HexOf(IEquipment item)
        {
            foreach (KeyValuePair<Hex, IEquipment> pair in _itemOn)
            {
                if (pair.Value == item) return pair.Key;
            }

            return null;
        }

        // Every item lying on the board, with the hex it lies on
        // e.g.     the stage editor saves them into a team comp
        public List<(Hex hex, IEquipment item)> All()
            => _itemOn.Where(pair => pair.Value as Object != null).Select(pair => (pair.Key, pair.Value)).ToList();

        // ======================================== setter ========================================
        // Lay the item on the hex. False when another item already lies there.
        public bool Place(IEquipment item, Hex hex)
        {
            if (item == null || hex == null || ItemOn(hex) != null) return false;

            Remove(item);
            _itemOn[hex] = item;

            Vector3 position = hex.transform.position;
            item.transform.position = new Vector3(position.x, position.y, item.transform.position.z);
            return true;
        }

        // Take the item off its hex. False when it was not on one.
        public bool Remove(IEquipment item)
        {
            Hex hex = HexOf(item);
            if (hex == null) return false;

            _itemOn.Remove(hex);
            return true;
        }

        // Take every item off the board, and hand them back so the caller can put them somewhere.
        // e.g.     the next stage is fought on another board, so the hexes they lie on are about to go
        public List<IEquipment> TakeAll()
        {
            List<IEquipment> items = _itemOn.Values.Where(item => item as Object != null).ToList();
            _itemOn.Clear();
            return items;
        }
    }
}
