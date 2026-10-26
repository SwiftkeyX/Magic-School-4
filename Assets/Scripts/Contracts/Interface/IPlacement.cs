using UnityEngine;

namespace MagicSchool.Contracts
{
    // IPlacement answer: what is the thing that a unit could stand on?
    // e.g. BenchSlot and Hex
    // OnUnitReserved: a unit is on its way here, hold it for them. OnUnitPlaced / OnUnitUnplaced: a unit now stands
    // here / is leaving.
    public interface IPlacement
    {
        Transform transform { get; }
        void OnUnitReserved(IPlaceable hero);
        void OnUnitPlaced(IPlaceable hero);
        void OnUnitUnplaced(IPlaceable hero);
    }

    // need somewhere to put the redundant code of each Placement logic, so we move it into dedicated Extension class
    public static class PlacementExtensions
    {
        public static void EnterPlacementExtension(this IPlacement placement, IPlaceable hero)
        {
            hero.SetCurrentPlacement(placement);
            hero.transform.position = placement.transform.position;
        }
    }
}
