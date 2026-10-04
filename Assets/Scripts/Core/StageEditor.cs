using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using MagicSchool.Contracts;
using MagicSchool.Combat.Heroes;
using MagicSchool.Combat.Placements;

namespace MagicSchool.Core
{
    /// <summary>
    /// Dev tool: build a stage's enemy team on the board instead of typing hexes into a BattlePlacementSO.
    ///     move    - drag an enemy to another enemy hex (Dragging.cs asks this class whether it may)
    ///     add     - TryAddEnemy puts a new enemy on the first free enemy hex
    ///     remove  - drop an enemy on the shop
    ///     save    - ReadEnemyTeam is what the board holds right now; the menu writes it into an asset
    ///
    /// Only reachable while GameManager._stageEditMode is ticked - GameManager.StageEditor is null otherwise.
    /// Writing the asset is Editor-only, so that half lives in Core/Editor/StageEditorMenu.cs.
    /// </summary>
    public class StageEditor
    {
        private const TeamEnum EnemyTeam = TeamEnum.Red;

        private readonly GameManager _game;
        private readonly HeroSpawner _spawner;

        internal StageEditor(GameManager game, HeroSpawner spawner)
        {
            _game = game;
            _spawner = spawner;
        }

        // ======================================== getter ========================================
        // the stage the board was seeded from - what "save" overwrites
        public BattlePlacementSO CurrentStage => _game.GetStage(_game.StageIndex);

        // a team only stands where it was placed during Preparation. Once the fight starts, heroes walk.
        public bool CanEditNow => _game.Phase == GamePhaseEnum.Preparation;

        // ======================================== add / remove ========================================
        // put a new enemy on the first free enemy hex, front column first. False if the side is full.
        public bool TryAddEnemy(HeroDataSO data)
        {
            if (data == null || !CanEditNow) return false;

            Hex free = FirstFreeEnemyHex();
            if (free == null) return false;

            _spawner.SpawnHero(data, EnemyTeam, free, _game.Board);
            return true;
        }

        public void RemoveEnemy(ICombatant enemy)
        {
            if (enemy as Object == null || enemy.Team != EnemyTeam) return;

            // same cleanup Seller does for a sold hero, minus the gold
            IPlacement placement = enemy.CurrentPlacement;
            if (placement != null) placement.OnUnitUnplaced(enemy);
            enemy.SetCurrentPlacement(null);
            enemy.UntrackFromBoard();

            Object.Destroy(enemy.transform.gameObject);
        }

        // ======================================== read ========================================
        // every enemy on the board and the hex it stands on, in a stable order so a re-save diffs cleanly
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
        // how many enemies a stage holds - what a save is about to replace
        public int EnemyCountIn(BattlePlacementSO stage)
            => stage.HeroesPlacement.Count(placement => placement.hexPlacement.team == EnemyTeam);

        // swap the stage's enemy team for this one. Only changes the object in memory:
        // making it stick on disk is the Editor's job (StageEditorMenu.cs).
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
