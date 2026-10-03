using UnityEngine;

namespace MagicSchool.Contracts
{
    // ISellZone answers: is the player about to drop this: 
    // e.g.    hero, item, (add here)... 
    // to sells it?
    public interface ISellZone
    {
        bool IsInSellBoundary(Vector2 screenPosition);   // Is the pointer inside the sell boundary?
        void ShowSellHint(bool isOverZone);                 // turn on tint of the shop to indicate the shop is focus
    }
}
