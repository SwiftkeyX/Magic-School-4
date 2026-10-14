using UnityEngine;
using MagicSchool.Contracts;

namespace MagicSchool.Combat.Heroes.States
{
    // Walks one hex, one frame at a time - no coroutine, since the state machine already drives
    // everything off OnUpdate(). SetTarget must be called (by whoever transitions in, e.g.
    // HeroIdle via Hero.BeginWalkTo) before this state becomes current.
    internal class HeroWalk : HeroState
    {
        public override HeroStateEnum StateType => HeroStateEnum.Walk;

        private readonly HexStep _step;

        public HeroWalk(Hero hero, MovementConfig movement, Transition transition) : base(hero, transition)
        {
            _step = new HexStep(hero, movement);
        }

        public override void OnEnter()
        {
            _step.StartTo(_me.ReservedHex);
        }

        public override void OnUpdate()
        {
            // walk according the movement's curve, arriving on the hex once there
            _step.Tick(Time.deltaTime);

            CheckSwitchState();
        }

        protected override void CheckSwitchState()
        {
            // if walk isn't finished, return
            if (_step.IsWalking) return;

            ICombatant nearestEnemy = _me.FindNearestEnemy();

            // guard
            if (nearestEnemy == null) { _me.ChangeState(HeroStateEnum.Idle); return; }

            if (_transition.CanAttack(nearestEnemy))
            {
                _me.ChangeState(HeroStateEnum.Attack);
                return;
            }

            if (_transition.CanWalk(nearestEnemy))
            {
                ResumeWalk();
                return;
            }

            else
            {
                _me.ChangeState(HeroStateEnum.Idle);
                return;
            }
        }

        // FLAGGING: This look completely like OnEnter() BUT later OnEnter() maybe have animation and other logic
        // so I separate it.
        private void ResumeWalk()
        {
            _step.StartTo(_me.ReservedHex);
        }
    }
}
