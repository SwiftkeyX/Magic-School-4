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
    // Only the first contact counts. A dead hero keeps its collider but is not drawn, so flying through it is not a
    // hit (Skeleton Archer's arrow once vanished on an invisible corpse).
    internal class OnContactHitbox : Hitbox
    {
        private ICombatant _caster;
        private readonly HashSet<ICombatant> _triggeredOnce = new HashSet<ICombatant>();

        public event Action<ICombatant> OnHit;

        public void Init(ICombatant caster)
        {
            _caster = caster;
        }

        public void OnTriggerEnter2D(Collider2D other)
        {
            if (_caster == null) return;

            ICombatant heroHit = other.GetComponent<ICombatant>();

            if (heroHit == null) return;

            if (!heroHit.IsAlive) return;

            if (!_triggeredOnce.Add(heroHit)) return;

            OnHit?.Invoke(heroHit);
        }

        public void OnTriggerExit2D(Collider2D other) { }
    }
}
