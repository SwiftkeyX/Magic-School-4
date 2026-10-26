namespace MagicSchool.Skills
{
    // Tune the template action.
    // use by each hero skill builder, 
    // they can tune those variable to get different behaviour of the template action. 
    // Every field is optional: unset keeps the prefab's value. CastTime: how long the caster cannot auto attack;
    // WalkWhileCasting: it still walks toward its target meanwhile. AOE Range / Move Range / Projectile Range are
    // reach in hexes, not size; Spread is ClusteredCircle's radius or ClusteredLaser's half-width. Move MaxRange: keep
    // charging as far as possible instead of stopping behind the last enemy hit. Projectile Speed is world units per
    // second (8 when unset); Falloff is a piercing shot's damage less per enemy already hit (0.33 => 100%, 67%, 45%).
    // A runner's Interval is only read in Sequence mode, and PauseAfter is the wait after the last shot that a flow
    // waits on.
    public class Tuning
    {
        public float? CastTime;
        public System.Func<float> CastTimeOf;     // read when the part is played, in place of CastTime
        public float? CastTimeNow => CastTimeOf != null ? CastTimeOf() : CastTime;
        public bool? WalkWhileCasting;
    }

    public class AOETuning : Tuning
    {
        public float? Duration;
        public float? Length;
        public float? Width;
        public bool? Sticky;
        public AOEOffsetEnum? Offset;
        public int? Range;
    }

    public class MoveTuning : Tuning
    {
        public int? Range;
        public float? Duration;
        public float? Spread;
        public bool? MaxRange;
    }

    public class SummonTuning : Tuning
    {
        public float? Duration;
        public float? Interval;
        public SkillPart Act;
    }

    public class ProjectileTuning : Tuning
    {
        public int? Range;
        public float? Spread;
        public float? Size;
        public float? Speed;
        public float? Falloff;
    }

    public class FireTimingRunnerTuning : Tuning
    {
        public int? Count;
        public FireTimingModeEnum? Mode;
        public float? Interval;
        public float? PauseAfter;
        public Tuning InnerTuning;
    }

    // FireTimingRunner with Projectile
    // FLAGGING: Currently use in 1 scenario e.g. Imp attack random enemies in 2 hex of current target
    public class FireTimingRunnerProjectileTuning : FireTimingRunnerTuning
    {
        public int? RandomPoolRadius;    
    }
}
