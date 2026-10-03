using UnityEngine;

namespace MagicSchool.Contracts
{
    // ISellable answers: what does selling this refund, and what is it called?
    // e.g.     a hero (Hero)
    //          an item (Item)
    public interface ISellable
    {
        string DisplayName { get; }
        int Price { get; }                  // what the shop charged for it, and what selling it refunds
        Transform transform { get; }        // what is removed from the world once it is sold
    }
}
