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
    // A destroyed item is checked through `as Object`: == on the interface misses it.
    public class HexItems
    {
        private readonly Dictionary<Hex, IEquipment> _itemOn = new Dictionary<Hex, IEquipment>();

        public IEquipment ItemOn(Hex hex)
        {
            if (hex == null || !_itemOn.TryGetValue(hex, out IEquipment item)) return null;

            if (item as Object != null) return item;

            _itemOn.Remove(hex);
            return null;
        }

        public Hex HexOf(IEquipment item)
        {
            foreach (KeyValuePair<Hex, IEquipment> pair in _itemOn)
            {
                if (pair.Value == item) return pair.Key;
            }

            return null;
        }

        public List<(Hex hex, IEquipment item)> All()
            => _itemOn.Where(pair => pair.Value as Object != null).Select(pair => (pair.Key, pair.Value)).ToList();

        public bool Place(IEquipment item, Hex hex)
        {
            if (item == null || hex == null || ItemOn(hex) != null) return false;

            Remove(item);
            _itemOn[hex] = item;

            Vector3 position = hex.transform.position;
            item.transform.position = new Vector3(position.x, position.y, item.transform.position.z);
            return true;
        }

        public bool Remove(IEquipment item)
        {
            Hex hex = HexOf(item);
            if (hex == null) return false;

            _itemOn.Remove(hex);
            return true;
        }

        public List<IEquipment> TakeAll()
        {
            List<IEquipment> items = _itemOn.Values.Where(item => item as Object != null).ToList();
            _itemOn.Clear();
            return items;
        }
    }
}
