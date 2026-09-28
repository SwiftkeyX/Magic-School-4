using UnityEngine;
using MagicSchool.Contracts;
using MagicSchool.Engine;
using MagicSchool.Combat.Placements;

namespace MagicSchool.Combat.Heroes.States
{
    // Walks one hex, one frame at a time - no coroutine, since the state machine already drives
    // everything off OnUpdate(). SetTarget must be called (by whoever transitions in, e.g.
    // HeroIdle via Hero.BeginWalkTo) before this state becomes current.
    internal class HeroWalk : HeroState
    {
        public override HeroStateEnum StateType => HeroStateEnum.Walk;

        private Hex _targetHex;
        private CurveMotion _step;

        private readonly MovementConfig _movement;

        public HeroWalk(Hero hero, MovementConfig movement, Transition transition) : base(hero, transition)
        {
            _movement = movement;
        }

        public override void OnEnter()
        {
            StartStepTo(_me.ReservedHex);
        }

        public override void OnUpdate()
        {
            // walk according the movement's curve
            _me.transform.position = _step.Tick(Time.deltaTime);

            // if walk is finished, hand the hero over to the hex it stepped onto, check switch state
            if (_step.IsFinished)
            {
                ArriveAt(_targetHex);
            }

            CheckSwitchState();
        }

        protected override void CheckSwitchState()
        {
            // if walk isn't finished, return
            if (!_step.IsFinished) return;

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

        // A unit arrive at new hex
        // un owner to previous hex, set new owner to the new hex.
        // NOTE: this use same pattern as HeroMover & Move Template action 
        private void ArriveAt(Hex hex)
        {
            // guard
            if (_me.CurrentHex == hex) return;      

            IPlacement previous = _me.CurrentPlacement;
            if (previous != null) previous.OnUnitUnplaced(_me);
            hex.OnUnitPlaced(_me);
        }

        private void StartStepTo(Hex hex)
        {
            _targetHex = hex;
            _step = new CurveMotion(
                start: _me.transform.position,
                end: hex.transform.position,
                duration: 1f / _movement.MoveSpeed,
                curve: _movement.WalkCurve);
        }

        // FLAGGING: This look completely like OnEnter() BUT later OnEnter() maybe have animation and other logic
        // so I separate it.
        private void ResumeWalk()
        {
            StartStepTo(_me.ReservedHex);
        }
    }
}
