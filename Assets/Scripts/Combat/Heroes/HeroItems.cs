using System.Collections.Generic;
using UnityEngine;
using MagicSchool.Contracts;

namespace MagicSchool.Combat.Heroes
{
    // One slot each: a hero gets more out of items by standing on an ally's wearing hex (See AlignBonuses). A forged
    // item is worn past Capacity, any number, for the rest of the fight, and ClearForged destroys them as the stats
    // are rebuilt, so no modifier is taken back. ReGrantAll gives everything worn again after the stats are reset; a
    // destroyed item is skipped through `as Object`, since reading .Modifier off one throws.
    internal class HeroItems
    {
        public const int Capacity = 1;
        private readonly List<IEquipment> _worn = new List<IEquipment>(Capacity);
        private readonly List<IEquipment> _forged = new List<IEquipment>();
        private readonly Hero _wearer;

        private const float Strength = 1f;

        private const float RowY = -0.55f;
        private const float Spacing = 0.34f;
        private const float Scale = 0.3f;

        public IReadOnlyList<IEquipment> Worn => _worn;
        public int Count => _worn.Count;
        public bool HasRoom => _worn.Count < Capacity;

        public HeroItems(Hero wearer)
        {
            _wearer = wearer;
        }

        public bool TryWear(IEquipment item)
        {
            if (item == null || !HasRoom) return false;

            _worn.Add(item);

            int slotPosition = _worn.Count - 1;
            Seat(item, slotPosition);

            Grant(item);

            return true;
        }

        public void WearForged(IEquipment item)
        {
            if (item == null) return;

            _worn.Add(item);
            _forged.Add(item);
            SeatAll();
            Grant(item);
        }

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

        public bool TryTakeOff(IEquipment item)
        {
            if (item == null || !_worn.Remove(item)) return false;

            ICustomModifier granted = item.Modifier;
            if (granted != null) _wearer.RemoveModifier(granted);

            Unseat(item);

            SeatAll();

            return true;
        }

        public void ReGrantAll()
        {
            foreach (IEquipment item in _worn)
            {
                if (item as Object == null) continue;

                Grant(item);
            }
        }

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

        private void Seat(IEquipment item, int slot)
        {
            Transform worn = item.transform;

            worn.SetParent(_wearer.transform, worldPositionStays: false);
            int count = Mathf.Max(Capacity, _worn.Count);
            worn.localPosition = new Vector3((slot - (count - 1) / 2f) * Spacing, RowY, 0f);
            worn.localScale = new Vector3(Scale, Scale, 1f);
        }

        private void Unseat(IEquipment item)
        {
            Transform worn = item.transform;

            worn.SetParent(null, worldPositionStays: true);
            worn.localScale = Vector3.one;
        }
    }
}
