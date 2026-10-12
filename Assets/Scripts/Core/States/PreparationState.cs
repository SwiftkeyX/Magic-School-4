using System.Linq;
using MagicSchool.Contracts;
using MagicSchool.Combat.Heroes;
using MagicSchool.Combat.Placements;
using MagicSchool.Engine;
using MagicSchool.Economy;

namespace MagicSchool.Core.States
{
    /// <summary>
    /// Buying, and putting heroes on the board. Nothing fights.
    /// Left only when the player asks for it - see GameManager.StartCombat.
    /// </summary>
    internal class PreparationState : GameState
    {
        public override GamePhaseEnum StateType => GamePhaseEnum.Preparation;

        public PreparationState(GameManager game) : base(game) { }

        public override void OnEnter()
        {
            _game.Board.SetBattleOn(false);

            // reset var
            _game.SetWinner(null);
            _game.ClearPendingRequests();

            SeedNewEnemies();

            // before the player's team is reset: a hero that has to stay on the board is put inside the new zone
            ApplyDeployZone();

            ResetPlayerTeam();

            _game.Shop.Restock();

            _game.Hint?.ShowPreparation(_game.StageNumber, _game.StageCount);
            _game.Banner?.SetShown(false);
        }

        // if there's heroes from the player side on the board, allow to start combat
        protected override void CheckSwitchState()
        {
            if (!_game.ConsumeStartCombatRequest()) return;

            bool hasHeroesOnBoard = _game.Board.HeroesOnBoard.Any(h => h.Team == TeamEnum.Blue);
            if (!hasHeroesOnBoard)
            {
                DebugTool.LogWarning("Can't start combat - place at least one hero on the board first.");
                return;
            }

            _game.ChangeState(GamePhaseEnum.Combat);
        }

        private void SeedNewEnemies()
        {
            // wipe all enemies
            _game.Board.ClearTeam(TeamEnum.Red);

            // switch to this stage's seed, before anything spawns from it
            // context: this stage = could be a same stage that was repeated or new stage.
            _game.Seed.SwitchSeed(_game.GetStage(_game.StageIndex));

            // spawn enemy team in preparation state
            _game.Seed.SpawnTeamOnBoard(TeamEnum.Red);
        }

        // limit where the player may put heroes to this stage's zone, and show it
        private void ApplyDeployZone()
        {
            BattlePlacementSO stage = _game.GetStage(_game.StageIndex);
            if (stage == null) return;

            _game.Board.SetDeployZone(stage.DeployZone);
            _game.Board.ShowDeployZone(true);

            // an empty zone is the whole side, so it can't be too small
            int zoneSize = stage.DeployZone.Count;
            if (zoneSize > 0 && zoneSize < _game.HeroLimit)
            {
                DebugTool.LogWarning($"{stage.name}: the deploy zone has {zoneSize} hexes but the player may field {_game.HeroLimit} heroes.");
            }
        }

        private void ResetPlayerTeam()
        {
            // if player was seeded, clear old team, then seed again.
            if (_game.IsPlayerSeed)
            {
                _game.Board.ClearTeam(TeamEnum.Blue);
                _game.Seed.SpawnTeamOnBoard(TeamEnum.Blue);
            }

            // if not seeded, reset, and send everyone back to the bench
            else
            {
                ResetTeam(TeamEnum.Blue);
                _game.Formation.ReturnToBench();
            }
        }

        // Reset hero = reset its stat, sprite, statemachine
        // e.g. after player is losing, reset each hero from player team
        private void ResetTeam(TeamEnum team)
        {
            foreach (ICombatant combatant in _game.Board.HeroesOnBoard)
            {
                if (combatant == null || combatant.Team != team) continue;

                if (combatant is Hero hero) hero.ResetForNewStage();
            }
        }
    }
}
