using UnityEngine;
using MagicSchool.Contracts;
using MagicSchool.Skills;

namespace MagicSchool.Combat.Heroes.States
{
    // A free cast starts with mana not full and leaves it alone (See ISkillCaster.GrantFreeCast). Cast holds while a step taken
    // during it is still landing, and while a skill's Move (a jump or dash) is still moving the caster: walking off
    // mid-air, the step would carry him back once he landed (Knight's leap outlasts his cast time). It also holds
    // while the skill repeats (Rogue's third shadowstep).
    // walkWhileCasting: still casting and unable to attack, but walking a hex at a time toward the target as Walk
    // does, standing still once in range (Orc Blademaster's storm).
    internal class HeroCast : HeroState
    {
        public override HeroStateEnum StateType => HeroStateEnum.Cast;

        private float _remaining;
        private bool _canWalk;
        private readonly HexStep _step;

        public HeroCast(Hero hero, MovementConfig movement, Transition transition) : base(hero, transition)
        {
            _step = new HexStep(hero, movement);
        }

        public override void OnEnter()
        {
            _remaining = _me.GetCastTime();
            _canWalk = _me.CanWalkWhileCasting();
            _step.Cancel();

            _me.PlaySkillCastEffect("Skill Activated!");

            if (_me.IsManaCapped()) _me.SpendMana();
        }

        public override void OnUpdate()
        {
            _remaining -= Time.deltaTime;

            if (_canWalk) WalkWhileCasting();

            CheckSwitchState();
        }

        public override void OnExit() => _step.CutOff();

        protected override void CheckSwitchState()
        {
            if (_remaining > 0f) return;

            if (_step.IsWalking) return;

            if (_me.IsSkillMovingMe()) return;

            if (_me.IsSkillRepeating()) return;

            ICombatant currentTarget = _me.FindCurrentTarget();

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

        private void WalkWhileCasting()
        {
            if (_step.IsWalking)
            {
                _step.Tick(Time.deltaTime);
                return;
            }

            if (_remaining <= 0f) return;

            ICombatant target = _me.FindCurrentTarget();
            if (target == null || _transition.CanAttack(target)) return;

            if (_transition.CanWalk(target)) _step.StartTo(_me.ReservedHex);
        }

    }
}
