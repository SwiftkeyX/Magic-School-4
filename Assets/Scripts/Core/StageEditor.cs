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
    ///     save formation      - ReadEnemyTeam is what the board holds right now; the menu writes it into an asset
    ///
    /// Only reachable while GameManager._stageEditMode is ticked - GameManager.StageEditor is null otherwise.
    /// Writing the asset is Editor-only, so that half lives in Core/Editor/StageEditorMenu.cs.
    /// </summary>
    public class StageEditor
    {
        private const TeamEnum EnemyTeam = TeamEnum.Red;

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

        // ======================================== write ========================================
        // how many enemies now?
        public int EnemyCountIn(BattlePlacementSO stage)
            => stage.HeroesPlacement.Count(placement => placement.hexPlacement.team == EnemyTeam);

        // overwrite enemy team's SO to the specify one
        public void WriteEnemyTeam(BattlePlacementSO stage, IEnumerable<HeroPlacement> team)
            => stage.ReplaceTeam(EnemyTeam, team);

        // ======================================== private ========================================
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
