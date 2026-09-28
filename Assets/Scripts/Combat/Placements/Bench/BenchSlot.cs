using UnityEngine;
using MagicSchool.Contracts;

namespace MagicSchool.Combat.Placements
{
    public class BenchSlot : MonoBehaviour, IPlacement
    {
        private IPlaceable _occupant;

        // ==================== getter ===================
        public IPlaceable Occupant => _occupant;
        public bool Reserved => _occupant != null;

        // A hero on its way here holds the slot, same as one already standing on it - otherwise
        // a second hero could be sent to a slot that's about to be occupied.
        public void OnUnitReserved(IPlaceable hero)
        {
            _occupant = hero;
        }

        // A hero placed on a bench slot has no hex - just remember who's here so
        // OnHeroUnplaced can free the slot again once they leave.
        public void OnUnitPlaced(IPlaceable hero)
        {
            this.EnterPlacementExtension(hero);

            _occupant = hero;
        }

        // The hero sitting here is moving to a different placement - free the slot.
        public void OnUnitUnplaced(IPlaceable hero)
        {
            if (_occupant == hero) _occupant = null;
        }
    }
}
