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
            _me.TriggerOnDeath();
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

        // a revive (See IRevivable) gets the hero back up here
        public override void OnUpdate()
        {
            if (_me.TickRevive(UnityEngine.Time.deltaTime)) _me.Revive();
        }

        protected override void CheckSwitchState() { }
    }
}
