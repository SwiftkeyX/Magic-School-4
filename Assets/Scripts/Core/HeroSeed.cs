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
    // HeroSpawner hands it its spawn (SpawnWith); each hero gets the stage's level and the saved star once it stands on
    // its hex (See HeroLevel, HeroStar).
    public class HeroSeed
    {
        private BattlePlacementSO _placementSO;
        private readonly BattleBoard _board;

        private Func<HeroDataSO, TeamEnum, IPlacement, BattleBoard, Hero> _spawn;

        public void SpawnWith(Func<HeroDataSO, TeamEnum, IPlacement, BattleBoard, Hero> spawn) => _spawn = spawn;

        public HeroSeed(BattlePlacementSO placementSO, BattleBoard board)
        {
            _placementSO = placementSO;
            _board = board;
        }

        public void SwitchSeed(BattlePlacementSO placementSO) => _placementSO = placementSO;

        internal Hero Spawn(HeroDataSO data, TeamEnum team, IPlacement placement) => _spawn(data, team, placement, _board);

        public void SpawnTeamOnBoard(TeamEnum team)
        {
            if (_placementSO == null) { DebugTool.LogError("Can't seed the hero. Hero Placement is null"); return; }
            if (_spawn == null) { DebugTool.LogError("Can't seed the hero. No spawner was handed to the seed."); return; }

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

                Hero hero = _spawn(data, heroPlacement.team, hex, _board);

                if (heroPlacement.Level > HeroLevel.Min) hero.SetLevel(heroPlacement.Level);

                if (heroPlacement.Star > HeroStar.Min) hero.UpgradeTo(heroPlacement.Star);
            }
        }
    }
}
