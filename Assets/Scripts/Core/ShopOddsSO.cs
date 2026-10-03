using System;
using UnityEngine;
using MagicSchool.Contracts;

namespace MagicSchool.Core
{
    [CreateAssetMenu(fileName = "ShopOdds", menuName = "Magic School 4/Shop Odds")]
    public class ShopOddsSO : ScriptableObject
    {
        // the shop offer are randomized into the shop using their TierOdds.
        // e.g.     common      =   50%
        //          uncommon    =   30%
        //          rare        =   20%
        // new TierOdds(1, 75, 25, 0)   =>      stage 1 onward, chance are 75%/25%/0%
        // new TierOdds(4, 50, 40, 10)  =>      stage 4 onward, chance are 50%/40%/10%
        [Serializable]
        public struct TierOdds
        {
            [Min(1)] public int FromStage;
            [Min(0)] public int Common;
            [Min(0)] public int Uncommon;
            [Min(0)] public int Rare;

            public TierOdds(int fromStage, int common, int uncommon, int rare)
            {
                FromStage = fromStage;
                Common = common;
                Uncommon = uncommon;
                Rare = rare;
            }

            public int Total => Common + Uncommon + Rare;

            public int WeightOf(HeroTierEnum tier)
            {
                switch (tier)
                {
                    case HeroTierEnum.Common: return Common;
                    case HeroTierEnum.Uncommon: return Uncommon;
                    case HeroTierEnum.Rare: return Rare;
                    default: return 0;
                }
            }
        }

        // FIXLATER: let have 3 hero slot, and 2 item slot. don't let them lump together.
        // Could you put each odd into each slot itself? 
        // slot1-3 item chance are 0%, slot4-5 item chance are 100%
        [Tooltip("Chance, in percent, that a slot rolls an item instead of a hero.")]
        [SerializeField, Range(0, 100)] private int _itemChance = 20;

        [Tooltip("Hero tier odds. Each row applies from its stage until the next row's. Keep them in stage order.")]
        [SerializeField] private TierOdds[] _tierOdds =
        {
            new TierOdds(1, 75, 25, 0),
            new TierOdds(4, 50, 40, 10),
            new TierOdds(7, 30, 45, 25),
        };

        // ===================== getter =====================
        public int ItemChance => _itemChance;

        // the row in effect on this stage (1-based, as GameManager.StageNumber)
        public TierOdds OddsFor(int stageNumber)
        {
            if (_tierOdds == null || _tierOdds.Length == 0) return new TierOdds(1, 1, 0, 0);

            TierOdds current = _tierOdds[0];
            foreach (TierOdds row in _tierOdds)
                if (row.FromStage <= stageNumber) current = row;

            return current;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_tierOdds == null) return;

            for (int i = 0; i < _tierOdds.Length; i++)
            {
                if (_tierOdds[i].Total == 0)
                    Debug.LogWarning($"[ShopOdds] row {i} (from stage {_tierOdds[i].FromStage}) has no weight on any tier.", this);

                if (i > 0 && _tierOdds[i].FromStage <= _tierOdds[i - 1].FromStage)
                    Debug.LogWarning($"[ShopOdds] row {i} starts at stage {_tierOdds[i].FromStage}, not after the row above it.", this);
            }
        }
#endif
    }
}
