using System.Collections.Generic;
using System.Linq;
using MagicSchool.Contracts;
using MagicSchool.CombatRecording;
using MagicSchool.Combat.Heroes;
using UnityEngine.Profiling;

namespace MagicSchool.Core.States
{
    /// <summary>
    /// The combat is over, annouce the winner.
    /// </summary>
    // The first continue only takes the scoreboard down. Clearing the run loops back to stage 1, so the team size
    // loops back too, or the replay would start oversized.
    internal class ResultState : GameState
    {
        public override GamePhaseEnum StateType => GamePhaseEnum.Result;
        private bool _scoreboardDismissed;

        public ResultState(GameManager game) : base(game) { }

        public override void OnEnter()
        {
            _game.Board.SetBattleOn(false);

            _scoreboardDismissed = false;

            _game.Hint?.ShowScoreboard(_game.Winner, _game.StageNumber, _game.StageCount, _game.IsRunCleared);
            _game.Banner?.ShowResult(_game.Winner, _game.StageNumber, _game.StageCount, _game.IsRunCleared);

            _game.Recorder?.EndRound();

            IReadOnlyList<ScoreRow> scores = BuildScores();
            _game.Scoreboard?.ShowScores(scores);
            _game.BalanceLog?.AppendRound(_game.StageNumber, _game.StageCount, _game.Winner, scores);
        }

        public override void OnExit()
        {
            _game.Reward?.SetShown(false);
            _game.Scoreboard?.SetShown(false);
        }

        protected override void CheckSwitchState()
        {
            if (!_game.Requests.ConsumeContinue()) return;

            bool playerWon = _game.Winner == TeamEnum.Blue;
            bool runCleared = _game.IsRunCleared;

            if (TakeScoreboardDown())
            {
                if (playerWon)
                {
                    _game.Hint?.ShowResult(_game.Winner, _game.StageNumber, _game.StageCount, _game.IsRunCleared);
                    _game.Reward?.ShowReward();
                }

                return;
            }

            if (playerWon)
            {
                if (IsStillChoosingReward()) return;

                _game.SetStageIndex(runCleared ? 0 : _game.StageIndex + 1);

                if (runCleared) _game.ResetHeroLimit();
                else _game.GrowHeroLimit();
            }

            _game.Hint?.ShowResult(_game.Winner, _game.StageNumber, _game.StageCount, runCleared);

            _game.PayStageIncome();

            _game.ChangeState(GamePhaseEnum.Preparation);
        }

        private IReadOnlyList<ScoreRow> BuildScores()
        {
            CombatRecorder recorder = _game.Recorder;

            if (recorder == null) return new List<ScoreRow>();

            return _game.Board.HeroesOnBoard
                .Where(hero => hero != null)
                .Select(hero => GetHeroScoreData(hero, recorder))
                .OrderByDescending(row => row.Record.DamageDealt)
                .ThenByDescending(row => row.Record.HealingDone)
                .ToList();
        }

        private ScoreRow GetHeroScoreData(ICombatant hero, CombatRecorder recorder)
        {
            return new ScoreRow(
                    name: (hero as IInspectable)?.DisplayName ?? hero.transform.name,
                    team: hero.Team,
                    isAlive: hero.StateType != HeroStateEnum.Dead,
                    record: recorder.RoundOf(hero));
        }

        private bool TakeScoreboardDown()
        {
            if (_scoreboardDismissed) return false;

            _scoreboardDismissed = true;
            _game.Scoreboard?.SetShown(false);

            return true;
        }

        private bool IsStillChoosingReward()
        {
            if (_game.Reward == null || !_game.Reward.IsChoosing) return false;

            _game.Reward.SetShown(true);
            return true;
        }
    }
}
