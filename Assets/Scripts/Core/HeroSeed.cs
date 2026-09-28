using System;
using MagicSchool.Engine;
using MagicSchool.Contracts;
using MagicSchool.Combat.Heroes;
using MagicSchool.Combat.Placements;

namespace MagicSchool.Core
{
    // seed the hero onto the board
    // e.g. enemy side always got seed, player side don't use seed since player drag hero onto the board themself. 
    // p.s. but player can also be seeded too for fast testing.
    public class HeroSeed
    {
        private BattlePlacementSO _placementSO;
        private readonly BattleBoard _board;     // need to know which board, it'll seed on (there maybe several board at once)

        // Raised to ask for a hero. Fires BEFORE anything is spawned - HeroSpawner does the work.
        public event Action<HeroDataSO, TeamEnum, IPlacement, BattleBoard> OnSpawnRequested;

        public HeroSeed(BattlePlacementSO placementSO, BattleBoard board)
        {
            _placementSO = placementSO;
            _board = board;
        }

        // Switch the existing seed to another.
        // e.g. when player go to the next stage, seed new one for enemy team
        public void SwitchSeed(BattlePlacementSO placementSO) => _placementSO = placementSO;

        // spawn hero on the board according to the set seed
        public void SpawnTeamOnBoard(TeamEnum team)
        {
            if (_placementSO == null) { DebugTool.LogError("Can't seed the hero. Hero Placement is null"); return; }
            if (OnSpawnRequested == null) { DebugTool.LogError("Can't seed the hero. Nothing is listening for spawn requests."); return; }

            foreach (var heroPlacement in _placementSO.HeroesPlacement)
            {
                HeroDataSO data = heroPlacement.dataSO;
                HexNumber placement = heroPlacement.hexPlacement;

                if (placement.team != team) continue;

                OnSpawnRequested.Invoke(data, placement.team, _board.Hexs[placement], _board);
            }
        }
    }
}
