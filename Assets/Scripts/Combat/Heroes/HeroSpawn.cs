using System;
using System.Collections.Generic;
using MagicSchool.Contracts;
using MagicSchool.Combat.Placements;

namespace MagicSchool.Combat.Heroes
{
    /// <summary>
    /// During combat, lets a hero put new units on the board.
    /// e.g.    when Big Slime die, splits into 2 Small Slimes 
    /// </summary>
    internal class HeroSpawn
    {
        private const int SearchRange = 2;      

        private readonly Hero _me;
        private readonly BattleBoard _board;
        private readonly Action<HeroDataSO, TeamEnum, IPlacement, BattleBoard> _request;

        public HeroSpawn(Hero me, BattleBoard board, Action<HeroDataSO, TeamEnum, IPlacement, BattleBoard> request)
        {
            _me = me;
            _board = board;
            _request = request;
        }

        // spawn a unit up to `count` on the free hexes nearest to me.
        public int SpawnNearby(HeroDataSO unit, int count)
        {
            Hex myHex = _me.CurrentHex;
            if (unit == null || myHex == null) return 0;

            // find nearest hex. My own hex is skipped.
            List<Hex> free = HexFinder.FindFreeHexesWithin(myHex, SearchRange, _me.IsHexReservedByOther);
            free.Remove(myHex);

            int spawned = 0;
            foreach (Hex hex in free)
            {
                if (spawned >= count) break;

                _request(unit, _me.Team, hex, _board);
                spawned++;
            }

            return spawned;
        }
    }
}
