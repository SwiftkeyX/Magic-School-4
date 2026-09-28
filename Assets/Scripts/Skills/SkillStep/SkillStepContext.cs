using UnityEngine;
using MagicSchool.Contracts;

namespace MagicSchool.Skills
{
    /// <summary>
    /// What the step before this one produced, handed to the step it triggers.
    ///
    /// One object instead of a parameter per trigger:
    /// OnHit knows a position,
    /// OnKill will know who died,
    /// OnAttack will know who was being attacked.
    /// BLOCKED: add context for OnKill, OnAttack later
    ///
    /// Every field is optional - a trigger fills in only what it actually knows.
    /// </summary>
    public class SkillStepContext
    {
        public Vector3? Position { get; private set; }  // context for projectile hit position
        public ICombatant Target { get; private set; }  // context for a target chosen for this shot, e.g. FireTimingRunner's pseudo-random pick

        public SkillStepContext(Vector3 position)
        {
            Position = position;
        }

        public SkillStepContext(ICombatant target)
        {
            Target = target;
        }
    }
}
