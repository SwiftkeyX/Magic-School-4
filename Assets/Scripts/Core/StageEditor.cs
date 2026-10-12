using System.Collections.Generic;
using System.Linq;
using System;
using UnityEngine;
using MagicSchool.Contracts;
using MagicSchool.Combat.Heroes;
using MagicSchool.Combat.Placements;

namespace MagicSchool.Core
{
    /// <summary>
    /// Dev tool: build a enemy team during play mode. 
    /// This fix the problem of manualy typing enemy team into a BattlePlacementSO.
    ///     move enemy          - drag an enemy to another enemy hex 
    ///     add new enemy       - TryAddEnemy puts a new enemy on the free enemy hex
    ///     remove enemy        - drop an enemy on the shop
    ///     paint deploy zone   - click an empty player hex to put it in or out of the zone (dark = out)
    ///     save formation      - ReadEnemyTeam and ReadDeployZone are what the board holds right now; the menu writes them into an asset
    ///
    /// Only reachable while GameManager._stageEditMode is ticked - GameManager.StageEditor is null otherwise.
    /// Writing the asset is Editor-only, so that half lives in Core/Editor/StageEditorMenu.cs.
    /// </summary>
    public class StageEditor
    {
        private const TeamEnum EnemyTeam = TeamEnum.Red;
        private const TeamEnum PlayerTeam = TeamEnum.Blue;

        private readonly GameManager _game;

        public event Action<HeroDataSO, TeamEnum, IPlacement, BattleBoard> OnSpawnRequested;

        internal StageEditor(GameManager game)
        {
            _game = game;
        }

        // ======================================== getter ========================================
        public BattlePlacementSO CurrentStage => _game.GetStage(_game.StageIndex);
        public bool CanEditNow => _game.Phase == GamePhaseEnum.Preparation;

        // ======================================== add ========================================
        // put a new enemy on the first free enemy hex
        public bool TryAddEnemy(HeroDataSO data)
        {
            if (data == null || !CanEditNow) return false;
            if (OnSpawnRequested == null) { Debug.LogError("Can't add the enemy. Nothing is listening for spawn requests."); return false; }

            Hex free = FirstFreeEnemyHex();
            if (free == null) return false;

            OnSpawnRequested.Invoke(data, EnemyTeam, free, _game.Board);
            return true;
        }

        // ======================================== read ========================================
        // get every enemy on the board and the hex it stands on
        public List<HeroPlacement> ReadEnemyTeam()
        {
            return _game.Board.HeroesOnBoard
                .OfType<Hero>()
                .Where(hero => hero != null && hero.Team == EnemyTeam && hero.CurrentHex != null)
                .Select(hero => new HeroPlacement(hero.Data, hero.CurrentHex.Number))
                .OrderBy(placement => placement.hexPlacement.column)
                .ThenBy(placement => placement.hexPlacement.row)
                .ToList();
        }

        // the player's deploy zone as painted right now. Empty when the whole side is open.
        public List<HexNumber> ReadDeployZone() => _game.Board.ReadDeployZone(PlayerTeam);

        // ======================================== deploy zone ========================================
        // put a player hex in the deploy zone, or take it out
        public bool TryToggleDeployHex(Hex hex)
        {
            if (hex == null || !CanEditNow || hex.GetTeam() != PlayerTeam) return false;

            // a zone with no hex would be saved as "the whole side", which is the opposite of what was painted
            bool isLastOne = hex.IsDeployable && OpenPlayerHexCount() == 1;
            if (isLastOne) return false;

            hex.SetDeployable(!hex.IsDeployable);
            return true;
        }

        // ======================================== write ========================================
        // how many enemies now?
        public int EnemyCountIn(BattlePlacementSO stage)
            => stage.HeroesPlacement.Count(placement => placement.hexPlacement.team == EnemyTeam);

        // how many hexes in the player's zone now? 0 = the whole side
        public int DeployZoneCountIn(BattlePlacementSO stage)
            => stage.DeployZone.Count(number => number.team == PlayerTeam);

        // overwrite enemy team's SO to the specify one
        public void WriteEnemyTeam(BattlePlacementSO stage, IEnumerable<HeroPlacement> team)
            => stage.ReplaceTeam(EnemyTeam, team);

        // overwrite the player's deploy zone on the SO
        public void WriteDeployZone(BattlePlacementSO stage, IEnumerable<HexNumber> zone)
            => stage.ReplaceDeployZone(PlayerTeam, zone);

        // ======================================== private ========================================
        private int OpenPlayerHexCount()
            => _game.Board.Hexs.Count(pair => pair.Key.team == PlayerTeam && pair.Value.IsDeployable);

        private Hex FirstFreeEnemyHex()
        {
            BattleBoard board = _game.Board;

            return board.Hexs
                .Where(pair => pair.Key.team == EnemyTeam && board.WhoReservedThisHex(pair.Value) == null)
                .OrderBy(pair => pair.Key.column)
                .ThenBy(pair => pair.Key.row)
                .Select(pair => pair.Value)
                .FirstOrDefault();
        }
    }
}
