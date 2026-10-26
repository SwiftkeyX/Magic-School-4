using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using MagicSchool.Engine;
using MagicSchool.Contracts;
using MagicSchool.Combat.Heroes;

namespace MagicSchool.Combat.Placements
{
    public class Bench : MonoBehaviour
    {
        [SerializeField] private Camera _cam;
        [SerializeField] private List<BenchSlot> _benchSlots;

        [SerializeField] private BattleBoard _board;

        public event Action<HeroDataSO, TeamEnum, IPlacement, BattleBoard> OnSpawnRequested;

        public IEnumerable<Hero> Heroes => _benchSlots.Select(slot => slot.Occupant).OfType<Hero>().Where(hero => hero != null);

        public BenchSlot FindFreeSlot() => _benchSlots.FirstOrDefault(slot => !slot.Reserved);

        public bool SpawnHeroOnBench(HeroDataSO data)
        {
            BenchSlot freeSlot = FindFreeSlot();

            if (freeSlot == null)
            {
                DebugTool.LogWarning("Bench: no free slot to place hero.");
                return false;
            }

            if (OnSpawnRequested == null)
            {
                DebugTool.LogError("Bench: nothing is listening for spawn requests.");
                return false;
            }

            OnSpawnRequested.Invoke(data, TeamEnum.Blue, freeSlot, _board);
            return true;
        }
    }
}
