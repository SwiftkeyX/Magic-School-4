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
    // OnSpawnRequested fires before anything is spawned and HeroSpawner handles it at once, so once it returns the
    // hero is on its hex; the stage's level and the saved star are given then (See HeroLevel, HeroStar).
    public class HeroSeed
    {
        private BattlePlacementSO _placementSO;
        private readonly BattleBoard _board;

        public event Action<HeroDataSO, TeamEnum, IPlacement, BattleBoard> OnSpawnRequested;

        public HeroSeed(BattlePlacementSO placementSO, BattleBoard board)
        {
            _placementSO = placementSO;
            _board = board;
        }

        public void SwitchSeed(BattlePlacementSO placementSO) => _placementSO = placementSO;

        public void SpawnTeamOnBoard(TeamEnum team)
        {
            if (_placementSO == null) { DebugTool.LogError("Can't seed the hero. Hero Placement is null"); return; }
            if (OnSpawnRequested == null) { DebugTool.LogError("Can't seed the hero. Nothing is listening for spawn requests."); return; }

            foreach (var heroPlacement in _placementSO.HeroesPlacement)
            {
                HeroDataSO data = heroPlacement.dataSO;
                HexNumber placement = heroPlacement.hexPlacement;

                if (heroPlacement.team != team) continue;

                if (!_board.Hexs.TryGetValue(placement, out Hex hex))
                {
                    DebugTool.LogError($"Can't seed {data.name}. {_placementSO.name} puts it on hex {placement.column},{placement.row}, which this board doesn't have.");
                    continue;
                }

                OnSpawnRequested.Invoke(data, heroPlacement.team, hex, _board);

                if (heroPlacement.Level > HeroLevel.Min && _board.WhoReservedThisHex(hex) is Hero hero) hero.SetLevel(heroPlacement.Level);

                if (heroPlacement.Star > HeroStar.Min && _board.WhoReservedThisHex(hex) is Hero upgraded) upgraded.UpgradeTo(heroPlacement.Star);
            }
        }
    }
}
