using System;

namespace MagicSchool.Contracts
{
    // IShop answers: what is for sale, what does it cost, and can the player buy it?
    // The shop panel only draws what this says, and asks it to buy / refresh.
    public interface IShop
    {
        int SlotCount { get; }
        int Gold { get; }
        int RefreshCost { get; }
        event Action OnChanged;             // the stock or the player's gold changed - draw again. FLAGGING: we don't have to lump 2 event togehter.

        ShopOffer OfferAt(int slot);
        bool CanAfford(int slot);
        bool CanAffordRefresh { get; }

        bool TryBuy(int slot);              
        bool TryRefresh();                  
    }
}
