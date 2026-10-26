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
            if (_me.IsDummy) return;

            ICombatant nearestEnemy = _me.FindNearestEnemy();

            if (nearestEnemy == null) { return; }

            if (_transition.CanAttack(nearestEnemy))
            {
                _me.ChangeState(HeroStateEnum.Attack);
                return;
            }

            if (_transition.CanWalk(nearestEnemy))
            {
                _me.ChangeState(HeroStateEnum.Walk);
                return;
            }

            else
            {
                return;
            }


        }
    }
}
