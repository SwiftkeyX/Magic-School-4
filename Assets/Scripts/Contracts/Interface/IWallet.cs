using System;

namespace MagicSchool.Contracts
{
    // IWallet answers: how much gold does the player have, and can he pay for this?
    public interface IWallet
    {
        int Gold { get; }
        event Action<int> OnGoldChanged;    

        bool CanAfford(int cost);
        bool TrySpend(int cost);            
        void Earn(int amount);
    }
}
