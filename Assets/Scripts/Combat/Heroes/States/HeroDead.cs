using System.Collections.Generic;
using MagicSchool.Contracts;

namespace MagicSchool.Combat.Heroes.States
{
    internal class HeroDead : HeroState
    {
        public override HeroStateEnum StateType => HeroStateEnum.Dead;

        public HeroDead(Hero hero, Transition transition) : base(hero, transition) { }

        public override void OnEnter()
        {
            _me.SetDeadVisual();

            AnnounceDeath();
        }

        private void AnnounceDeath()
        {
            foreach (ICombatant combatant in new List<ICombatant>(_me.HeroesOnBoard))
            {
                if (combatant is Hero hero && hero != _me && hero.IsAlive) 
                    hero.TriggerOnHeroDied(_me);
            }
        }

        public override void OnUpdate() { }

        protected override void CheckSwitchState() { }
    }
}
