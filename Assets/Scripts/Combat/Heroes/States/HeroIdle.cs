using MagicSchool.Contracts;

namespace MagicSchool.Combat.Heroes.States
{
    internal class HeroIdle : HeroState
    {
        public override HeroStateEnum StateType => HeroStateEnum.Idle;

        public HeroIdle(Hero hero, Transition transition) : base(hero, transition) { }

        public override void OnUpdate()
        {
            CheckSwitchState();
        }

        protected override void CheckSwitchState()
        {
            // Temporary, tagged once at its source - see the FLAGGING on HeroDataSO._isDummy.
            // Dummy never walks or attacks - it just stands there to be a target.
            if (_me.IsDummy) return;

            ICombatant nearestEnemy = _me.FindNearestEnemy();

            // guard
            if (nearestEnemy == null) { return; }

            // go attack
            if (_transition.CanAttack(nearestEnemy))
            {
                _me.ChangeState(HeroStateEnum.Attack);
                return;
            }

            // go walk
            if (_transition.CanWalk(nearestEnemy))
            {
                _me.ChangeState(HeroStateEnum.Walk);
                return;
            }

            // resume idle state
            else
            {
                return;
            }


        }
    }
}
