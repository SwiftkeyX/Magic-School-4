using UnityEngine;

namespace MagicSchool.Contracts
{
    // IPlacement answer: what is the thing that a unit could stand on?
    // e.g. BenchSlot and Hex
    public interface IPlacement
    {
        Transform transform { get; }
        void OnUnitReserved(IPlaceable hero);    // to tell this placement that a unit is on its way here - hold it for them.
        void OnUnitPlaced(IPlaceable hero);     // to tell this placement that a unit have standing on it.
        void OnUnitUnplaced(IPlaceable hero);   // to tell this placement that a unit is leaving this placement.
    }

    // need somewhere to put the redundant code of each Placement logic, so we move it into dedicated Extension class
    public static class PlacementExtensions
    {
        // basically, set placement for the hero
        public static void EnterPlacementExtension(this IPlacement placement, IPlaceable hero)
        {
            hero.SetCurrentPlacement(placement);
            hero.transform.position = placement.transform.position;
        }
    }
}
