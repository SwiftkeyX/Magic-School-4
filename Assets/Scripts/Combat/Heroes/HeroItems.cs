using System.Collections.Generic;
using UnityEngine;
using MagicSchool.Contracts;

namespace MagicSchool.Combat.Heroes
{
    internal class HeroItems
    {
        // one each: a hero gets more out of items by standing on an ally's wearing hex (See AlignBonuses)
        public const int Capacity = 1;
        private readonly List<IEquipment> _worn = new List<IEquipment>(Capacity);
        private readonly List<IEquipment> _forged = new List<IEquipment>();     // worn too, past Capacity; gone when the fight is (See WearForged)
        private readonly Hero _wearer;

        private const float Strength = 1f;

        // ====================== where the worn item sit on the hero ======================
        private const float RowY = -0.55f;
        private const float Spacing = 0.34f;
        private const float Scale = 0.3f;


        // ================================== getter ==================================
        public IReadOnlyList<IEquipment> Worn => _worn;
        public int Count => _worn.Count;
        public bool HasRoom => _worn.Count < Capacity;

        public HeroItems(Hero wearer)
        {
            _wearer = wearer;
        }

        // ========================================= public =========================================
        // Put an item on wearer, in the free slot. 
        public bool TryWear(IEquipment item)
        {
            if (item == null || !HasRoom) return false;

            // add new item to the worn list
            _worn.Add(item);

            // parent item to wearer
            int slotPosition = _worn.Count - 1;
            Seat(item, slotPosition);

            // item give modifier to wearer
            Grant(item);

            return true;
        }

        // An item a skill forged mid-fight: worn whatever the slot holds, any number of them, for the rest of the fight.
        // e.g.     Blacksmith's Forged Iron Plates, piling up on her when every ally is wearing something
        public void WearForged(IEquipment item)
        {
            if (item == null) return;

            _worn.Add(item);
            _forged.Add(item);
            SeatAll();
            Grant(item);
        }

        // The fight is over: what was forged in it is destroyed. Called as the stats are rebuilt, so no modifier is taken back.
        public void ClearForged()
        {
            foreach (IEquipment item in _forged)
            {
                _worn.Remove(item);
                if (item as Object != null) Object.Destroy(item.transform.gameObject);
            }
            _forged.Clear();
            SeatAll();
        }

        // Pull an item back off the wearer
        public bool TryTakeOff(IEquipment item)
        {
            if (item == null || !_worn.Remove(item)) return false;

            // remove item modifier from the wearer
            ICustomModifier granted = item.Modifier;
            if (granted != null) _wearer.RemoveModifier(granted);

            // take the item off visually
            Unseat(item);

            // whatever is left shuffles down into the free slot
            SeatAll();

            return true;
        }


        /// Grant everything worn, again.
        /// e.g. when the stat is reset, modifier is reset too, so item have to re-grant the modifier
        public void ReGrantAll()
        {
            foreach (IEquipment item in _worn)
            {
                // an item destroyed while worn. `as Object` because == on an interface-typed
                // reference is plain reference equality and misses Unity's destroyed objects,
                // and reading .Modifier off one of those throws.
                if (item as Object == null) continue;

                Grant(item);
            }
        }

        // ========================================= private =========================================
        // Hand the hero what the item gives.
        private void Grant(IEquipment item)
        {
            ICustomModifier granted = item.Modifier;

            if (granted == null) return;

            _wearer.AddModifier(granted, _wearer, Strength);
        }

        private void SeatAll()
        {
            for (int slot = 0; slot < _worn.Count; slot++)
                if (_worn[slot] as Object != null) Seat(_worn[slot], slot);
        }

        // Park the item under the hero, in its slot.
        private void Seat(IEquipment item, int slot)
        {
            Transform worn = item.transform;

            worn.SetParent(_wearer.transform, worldPositionStays: false);
            int count = Mathf.Max(Capacity, _worn.Count);
            worn.localPosition = new Vector3((slot - (count - 1) / 2f) * Spacing, RowY, 0f);   // the row is centred under the hero
            worn.localScale = new Vector3(Scale, Scale, 1f);
        }

        // take the item off the hero
        private void Unseat(IEquipment item)
        {
            Transform worn = item.transform;

            worn.SetParent(null, worldPositionStays: true);
            worn.localScale = Vector3.one;
        }
    }
}
