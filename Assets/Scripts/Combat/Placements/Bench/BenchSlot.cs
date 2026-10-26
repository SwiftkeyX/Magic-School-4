using UnityEngine;
using MagicSchool.Contracts;

namespace MagicSchool.Combat.Placements
{
    // A hero on its way here holds the slot like one already standing on it, or a second hero could be sent to a slot
    // about to be taken.
    public class BenchSlot : MonoBehaviour, IPlacement
    {
        [Tooltip("May the player drop a hero here? False for the spare bench: heroes are only ever sent to spare after a fight.")]
        [SerializeField] private bool _acceptsDrop = true;

        private IPlaceable _occupant;

        public bool AcceptsDrop => _acceptsDrop;
        public IPlaceable Occupant => _occupant;
        public bool Reserved => _occupant != null;

        public void OnUnitReserved(IPlaceable hero)
        {
            _occupant = hero;
        }

        public void OnUnitPlaced(IPlaceable hero)
        {
            this.EnterPlacementExtension(hero);

            _occupant = hero;
        }

        public void OnUnitUnplaced(IPlaceable hero)
        {
            if (_occupant == hero) _occupant = null;
        }
    }
}
