using System.Linq;
using MagicSchool.Contracts;

namespace MagicSchool.Core.States
{
    /// <summary>
    /// The fight. Heroes act on their own from here; this only watches for it being over.
    /// </summary>
    internal class CombatState : GameState
    {
        public override GamePhaseEnum StateType => GamePhaseEnum.Combat;

        public CombatState(GameManager game) : base(game) { }

        public override void OnEnter()
        {
            _game.Board.SetBattleOn(true);

            // remember the team's formation at the start
            _game.Formation.Remember(_game.Board.HeroesOnBoard, TeamEnum.Blue);

            // this round's numbers start from nothing; the run's totals keep accumulating
            _game.Recorder.BeginRound();

            _game.Hint?.ShowCombat(_game.StageNumber, _game.StageCount);
        }

        // if there is no hero on the board, change to result state 
        protected override void CheckSwitchState()
        {
            var alive = _game.Board.HeroesOnBoard.Where(h => h.StateType != HeroStateEnum.Dead);
            bool blueAlive = alive.Any(h => h.Team == TeamEnum.Blue);
            bool redAlive = alive.Any(h => h.Team == TeamEnum.Red);

            if (blueAlive && redAlive) return;

            _game.SetWinner(blueAlive ? TeamEnum.Blue : redAlive ? TeamEnum.Red : (TeamEnum?)null);
            _game.ChangeState(GamePhaseEnum.Result);
        }
    }
}
