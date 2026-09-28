using UnityEngine;
using MagicSchool.Contracts;
using MagicSchool.Skills;

namespace MagicSchool.Combat.Heroes.States
{
    internal class HeroCast : HeroState
    {
        public override HeroStateEnum StateType => HeroStateEnum.Cast;

        private float _remaining;

        public HeroCast(Hero hero, Transition transition) : base(hero, transition) { }

        public override void OnEnter()
        {
            // get cast time of skill
            _remaining = _me.GetCastTime();

            // if skill cast is success, pop skill effect
            _me.PlaySkillCastEffect("Skill Activated!");

            // spend mana
            _me.SpendMana();
        }

        public override void OnUpdate()
        {
            _remaining -= Time.deltaTime;

            CheckSwitchState();
        }

        protected override void CheckSwitchState()
        {
            // if still casting, return
            if (_remaining > 0f) return;

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

    }
}
