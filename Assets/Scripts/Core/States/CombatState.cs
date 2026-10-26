using System.Linq;
using MagicSchool.Contracts;
using MagicSchool.Combat.Heroes;
using MagicSchool.Skills;

namespace MagicSchool.Core.States
{
    /// <summary>
    /// The fight. Heroes act on their own from here; this only watches for it being over.
    /// </summary>
    // A skill reads its numbers when built, so any skill changed in the Skill Tuner since is rebuilt before the fight.
    // A hero waiting to get back up (IRevivable) still counts as standing. Won, lost or restarted while paused: the
    // next phase runs at the chosen speed.
    internal class CombatState : GameState
    {
        public override GamePhaseEnum StateType => GamePhaseEnum.Combat;

        public CombatState(GameManager game) : base(game) { }

        public override void OnEnter()
        {
            _game.Board.SetBattleOn(true);

            _game.RefreshAligning();

            RebuildTunedSkills();

            TriggerCombatStart();

            _game.Board.Colours.ShowDeployZone(false);

            _game.Formation.Remember(_game.Board.HeroesOnBoard, TeamEnum.Blue);

            _game.Recorder.BeginRound();

            _game.Hint?.ShowCombat(_game.StageNumber, _game.StageCount);
        }

        public override void OnExit() => _game.SetPaused(false);

        private void RebuildTunedSkills()
        {
            foreach (ICombatant combatant in _game.Board.HeroesOnBoard.ToList())
            {
                if (combatant is Hero hero && hero.IsSkillOutdated)
                    hero.RebuildSkill(SkillLibrary.Resolve(hero.Data.SkillId, _game.TemplateActions));
            }
        }

        private void TriggerCombatStart()
        {
            foreach (ICombatant combatant in _game.Board.HeroesOnBoard.ToList())
            {
                if (combatant is Hero hero && hero.IsAlive) hero.TriggerOnCombatStart();
            }
        }

        protected override void CheckSwitchState()
        {
            if (_game.ConsumeSkipStageRequest())
            {
                _game.Board.ClearTeam(TeamEnum.Red);
                _game.SetWinner(TeamEnum.Blue);
                _game.ChangeState(GamePhaseEnum.Result);
                return;
            }

            var alive = _game.Board.HeroesOnBoard.Where(h => h.StateType != HeroStateEnum.Dead || (h is IRevivable r && r.IsReviving));
            bool blueAlive = alive.Any(h => h.Team == TeamEnum.Blue);
            bool redAlive = alive.Any(h => h.Team == TeamEnum.Red);

            if (blueAlive && redAlive) return;

            _game.SetWinner(blueAlive ? TeamEnum.Blue : redAlive ? TeamEnum.Red : (TeamEnum?)null);
            _game.ChangeState(GamePhaseEnum.Result);
        }
    }
}
