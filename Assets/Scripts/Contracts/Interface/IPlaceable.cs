using UnityEngine;

namespace MagicSchool.Contracts
{
    // IPlaceable answer: to move a unit (the hero) on the IPlacement (the board) 
    // e.g. hero walk to the next hex
    // e.g. player move hero by dragging
    public interface IPlaceable
    {
        Transform transform { get; }
        IPlacement CurrentPlacement { get; }
        void SetCurrentPlacement(IPlacement placement);
        bool IsInCombat { get; }
    }
}
