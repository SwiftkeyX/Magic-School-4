using UnityEngine;

namespace MagicSchool.Contracts
{
    // ISellZone answers: is the player about to drop this: 
    // e.g.    hero, item, (add here)... 
    // to sells it?
    public interface ISellZone
    {
        bool IsInSellBoundary(Vector2 screenPosition);
        void ShowSellHint(bool isOverZone);
    }
}
