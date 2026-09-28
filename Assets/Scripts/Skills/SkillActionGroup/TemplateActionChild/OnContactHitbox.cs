using System;
using System.Collections.Generic;
using UnityEngine;
using MagicSchool.Contracts;

namespace MagicSchool.Skills
{
    /// <summary>
    /// Apply effect on contact e.g. projectile.
    /// Hitbox immediately trigger once it contact someone, the normal kind of logic for hitbox
    /// </summary>
    internal class OnContactHitbox : Hitbox
    {
        private ICombatant _caster;
        private readonly HashSet<ICombatant> _triggeredOnce = new HashSet<ICombatant>();

        public event Action<ICombatant> OnHit;

        public void Init(ICombatant caster)
        {
            _caster = caster;
        }

        /// <summary>
        /// When heroes who was hit on first contact:
        /// 1) Apply effect once if not cadence
        /// 2) Apply effect over time if cadence
        /// </summary>
        public void OnTriggerEnter2D(Collider2D other)
        {
            if (_caster == null) return;

            ICombatant heroHit = other.GetComponent<ICombatant>();

            // guard
            if (heroHit == null) return;

            // only the first contact counts - re-entering after walking out doesn't hit again
            if (!_triggeredOnce.Add(heroHit)) return;

            OnHit?.Invoke(heroHit);
        }

        public void OnTriggerExit2D(Collider2D other) { }
    }
}
