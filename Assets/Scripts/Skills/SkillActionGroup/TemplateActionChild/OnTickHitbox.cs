using System;
using System.Collections.Generic;
using UnityEngine;
using MagicSchool.Contracts;

namespace MagicSchool.Skills
{
    /// <summary>
    /// To Apply effect on tick e.g. Anivia's R.
    /// Hitbox will trigger every fix interval (cadence interval), even hero walk into hitbox won't get damage, if the hitbox doesn't trigger.
    /// e.g. you walk into spike but you are fine, until the spike trigger itself
    /// </summary>
    internal class OnTickHitbox : Hitbox
    {
        private ICombatant _caster;
        private readonly List<ICombatant> _heroesWhoWasHit = new List<ICombatant>();

        public event Action<ICombatant> OnHit;

        public void Init(ICombatant caster)
        {
            _caster = caster;
        }

        // Get all the hero who was hit by the skill
        public void OnTriggerEnter2D(Collider2D other)
        {
            if (_caster == null) return;

            ICombatant heroHit = other.GetComponent<ICombatant>();

            // not apply effect to myself, my team, the dead hero
            if (heroHit == null || heroHit.Team == _caster.Team || heroHit.StateType == HeroStateEnum.Dead) return;

            // Group all the heroes who was hit by the skill in 1 list
            if (!_heroesWhoWasHit.Contains(heroHit)) _heroesWhoWasHit.Add(heroHit);
        }

        // Remove hero who was not hit by the skill out of the list
        public void OnTriggerExit2D(Collider2D other)
        {
            ICombatant hero = other.GetComponent<ICombatant>();
            if (hero != null) _heroesWhoWasHit.Remove(hero);
        }

        // Called by the owner on its cadence interval - fires OnHit for everyone currently inside
        public void FireTick()
        {
            _heroesWhoWasHit.RemoveAll(hero => hero == null || hero.StateType == HeroStateEnum.Dead);

            foreach (ICombatant hero in _heroesWhoWasHit)
            {
                OnHit?.Invoke(hero);
            }
        }
    }
}
