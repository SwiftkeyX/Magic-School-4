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

        // need to know which board, its heroes will fight on (there maybe several board at once).
        [SerializeField] private BattleBoard _board;

        public event Action<HeroDataSO, TeamEnum, IPlacement, BattleBoard> OnSpawnRequested;

        /// <summary>
        /// Spawn hero on the bench. Returns false (and spawns nothing) if the bench is full.
        /// </summary>
        public bool SpawnHeroOnBench(HeroDataSO data)
        {
            // find the first slot that hasn't been used yet
            BenchSlot freeSlot = _benchSlots.FirstOrDefault(slot => !slot.Reserved);

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

            // spawn hero prefab, move it to that slot, reserved the freeslot
            OnSpawnRequested.Invoke(data, TeamEnum.Blue, freeSlot, _board);
            return true;
        }
    }
}
