using UnityEngine;
using MagicSchool.Contracts;

namespace MagicSchool.Combat.Heroes.States
{
    internal class HeroStunned : HeroState
    {
        public override HeroStateEnum StateType => HeroStateEnum.Stunned;

        public HeroStunned(Hero hero, Transition transition) : base(hero, transition) { }

        public override void OnExit()
        {
            // Snap back in case a stun landed mid-attack-dash, same reasoning as HeroAttack.OnExit.
            _me.transform.position = _me.CurrentHex.transform.position;
        }

        public override void OnUpdate()
        {
            CheckSwitchState();
        }

        protected override void CheckSwitchState()
        {
            if (!_me.IsStunned) _me.ChangeState(HeroStateEnum.Idle);
        }
    }
}
