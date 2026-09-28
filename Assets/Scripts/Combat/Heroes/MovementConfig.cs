using UnityEngine;

namespace MagicSchool.Combat.Heroes
{
    /// <summary>
    /// How a hero animates moving: walk pacing plus the two curves.
    ///
    /// These used to sit on the blackboard, but they were never shared state - only HeroIdle,
    /// HeroWalk and HeroAttack ever read them, and they never change after Init. Handing them to
    /// those three states directly keeps them out of the API every other system sees.
    /// </summary>
    internal readonly struct MovementConfig
    {
        public readonly float MoveSpeed;
        public readonly AnimationCurve WalkCurve;
        public readonly AnimationCurve AttackCurve;

        public MovementConfig(float moveSpeed, AnimationCurve walkCurve, AnimationCurve attackCurve)
        {
            MoveSpeed = moveSpeed;
            WalkCurve = walkCurve;
            AttackCurve = attackCurve;
        }
    }
}
