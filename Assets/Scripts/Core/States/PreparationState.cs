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

            // clear the previous board
            ClearBoard();
            
            // init new board layout
            ApplyLayout();

            // seed hero on the new board 
            SeedTeams();

            ApplyDeployZone();

            _game.RefreshAligning();

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

        // Take everyone off the board
        // e.g.     the last stage's enemies, and the player's team
        private void ClearBoard()
        {
            // wipe all enemies
            _game.Board.ClearTeam(TeamEnum.Red);

            // if player was seeded, wipe the old team too. It is seeded again in SeedTeams()
            if (_game.IsPlayerSeed)
            {
                _game.Board.ClearTeam(TeamEnum.Blue);
            }

            // if not seeded, reset, and send everyone who fought to the spare bench
            else
            {
                ResetTeam(TeamEnum.Blue);
                _game.Formation.ReturnToSpareBench();
            }
        }

        // Initialize new board layout
        private void ApplyLayout()
        {
            BattlePlacementSO stage = _game.GetStage(_game.StageIndex);
            if (stage == null) return;

            // every stage must name its board
            if (stage.BoardLayout == null)
            {
                DebugTool.LogError($"{stage.name} has no board layout, so the board keeps the hexes it has. Set its Board Layout.");
                return;
            }

            _game.Board.SetLayout(stage.BoardLayout);
        }

        private void SeedTeams()
        {
            // switch to this stage's seed, before anything spawns from it
            // context: this stage = could be a same stage that was repeated or new stage.
            _game.Seed.SwitchSeed(_game.GetStage(_game.StageIndex));

            // spawn enemy team in preparation state
            _game.Seed.SpawnTeamOnBoard(TeamEnum.Red);

            // spawn player team too, if the player was seeded
            if (_game.IsPlayerSeed) _game.Seed.SpawnTeamOnBoard(TeamEnum.Blue);
        }

        // Specify which hex player can put the hero on
        private void ApplyDeployZone()
        {
            BattlePlacementSO stage = _game.GetStage(_game.StageIndex);
            if (stage == null) return;

            _game.Board.SetDeployZone(stage.DeployZone);
            _game.Board.ShowDeployZone(true);

            // every stage must list its zone
            // without one the player can't place a single hero
            if (stage.DeployZone.Count == 0)
            {
                DebugTool.LogError($"{stage.name} has no deploy zone, so no hero can be placed. Add hexes to its Deploy Zone list.");
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
