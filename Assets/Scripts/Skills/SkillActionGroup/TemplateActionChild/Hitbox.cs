using System;
using UnityEngine;
using MagicSchool.Contracts;

namespace MagicSchool.Skills
{
    // Hitbox was a interface for all hitbox class e.g. ZoneAOE, CircleAOE
    // Its purpose is to report who get hit via OnHit
    public interface Hitbox
    {
        public void Init(ICombatant _caster);
        public event Action<ICombatant> OnHit;
        public void OnTriggerEnter2D(Collider2D other);
        public void OnTriggerExit2D(Collider2D other);
    }
}
