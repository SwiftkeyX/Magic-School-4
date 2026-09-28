using UnityEngine;

namespace MagicSchool.Contracts
{
    // IDraggable answers: what may the player pick up and carry with the pointer?
    // e.g. a hero, an item
    public interface IDraggable
    {
        Transform transform { get; }    
    }
}
