using System;
using MagicSchool.Contracts;

namespace MagicSchool.Core
{
    // the player's gold.
    internal class Wallet : IWallet
    {
        public int Gold { get; private set; }
        public event Action<int> OnGoldChanged;

        public Wallet(int startingGold)
        {
            Gold = startingGold;
        }

        public bool CanAfford(int cost) => Gold >= cost;

        public bool TrySpend(int cost)
        {
            if (!CanAfford(cost)) return false;

            SetGold(Gold - cost);
            return true;
        }

        public void Earn(int amount) => SetGold(Gold + amount);

        private void SetGold(int gold)
        {
            Gold = gold;
            OnGoldChanged?.Invoke(Gold);
        }
    }
}
