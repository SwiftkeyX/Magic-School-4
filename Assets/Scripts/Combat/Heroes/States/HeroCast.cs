using UnityEngine;
using MagicSchool.Contracts;
using MagicSchool.Skills;

namespace MagicSchool.Combat.Heroes.States
{
    internal class HeroCast : HeroState
    {
        public override HeroStateEnum StateType => HeroStateEnum.Cast;

        private float _remaining;
        private bool _canWalk;              // the skill lets him walk while casting e.g. Orc Blademaster's storm
        private readonly HexStep _step;

        public HeroCast(Hero hero, MovementConfig movement, Transition transition) : base(hero, transition)
        {
            _step = new HexStep(hero, movement);
        }

        public override void OnEnter()
        {
            // get cast time of skill
            _remaining = _me.GetCastTime();
            _canWalk = _me.CanWalkWhileCasting();
            _step.Cancel();

            // if skill cast is success, pop skill effect
            _me.PlaySkillCastEffect("Skill Activated!");

            // spend mana. A free cast starts with mana not full, and keeps what he has (See IFreeCaster)
            if (_me.IsManaCapped()) _me.SpendMana();
        }

        public override void OnUpdate()
        {
            _remaining -= Time.deltaTime;

            if (_canWalk) WalkWhileCasting();

            CheckSwitchState();
        }

        protected override void CheckSwitchState()
        {
            // if still casting, return
            if (_remaining > 0f) return;

            // a step taken while casting finishes on its hex first
            if (_step.IsWalking) return;

            // if the skill still repeats, return
            // e.g. Rogue's third shadowstep
            if (_me.IsSkillRepeating()) return;

            ICombatant currentTarget = _me.FindCurrentTarget();

            // guard
            if (currentTarget == null) { _me.ChangeState(HeroStateEnum.Idle); return; }

            if (_transition.CanAttack(currentTarget))
            {
                _me.ChangeState(HeroStateEnum.Attack);
                return;
            }

            if (_transition.CanWalk(currentTarget))
            {
                _me.ChangeState(HeroStateEnum.Walk);
                return;
            }

            else
            {
                _me.ChangeState(HeroStateEnum.Idle);
                return;
            }


        }

        // Still casting, but walking the way the Walk state does: a hex at a time toward the target,
        // standing still once it is in range. He cannot attack until the cast is over.
        private void WalkWhileCasting()
        {
            if (_step.IsWalking)
            {
                _step.Tick(Time.deltaTime);
                return;
            }

            // the cast is over: no new step, CheckSwitchState takes it from here
            if (_remaining <= 0f) return;

            ICombatant target = _me.FindCurrentTarget();
            if (target == null || _transition.CanAttack(target)) return;

            // CanWalk reserves the next hex when it says yes
            if (_transition.CanWalk(target)) _step.StartTo(_me.ReservedHex);
        }

    }
}
