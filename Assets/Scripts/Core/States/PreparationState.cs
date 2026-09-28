using System.Linq;
using MagicSchool.Contracts;
using MagicSchool.Engine;

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

            ResetPlayerTeam();

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

        private void ResetPlayerTeam()
        {
            // if player was seeded, clear old team, then seed again.
            if (_game.IsPlayerSeed)
            {
                _game.Board.ClearTeam(TeamEnum.Blue);
                _game.Seed.SpawnTeamOnBoard(TeamEnum.Blue);
            }

            // if not seeded, reset, and restore the formation
            else
            {
                _game.Board.ResetTeam(TeamEnum.Blue);
                _game.Formation.Restore();
            }
        }
    }
}
