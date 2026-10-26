using UnityEngine;

namespace MagicSchool.Contracts
{
    // ISellable answers: what does selling this refund, and what is it called?
    // e.g.     a hero (Hero)
    //          an item (Item)
    // Price is what the shop charged for it and what selling it refunds.
    public interface ISellable
    {
        string DisplayName { get; }
        int Price { get; }
        Transform transform { get; }
    }
}
