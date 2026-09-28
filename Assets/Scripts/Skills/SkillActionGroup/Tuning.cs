namespace MagicSchool.Skills
{
    // Tune the template action.
    // use by each hero skill builder, 
    // they can tune those variable to get different behaviour of the template action. 
    public class Tuning
    {
        public float? CastTime;          // how long the caster is locked out of auto attacking
    }

    public class AOETuning : Tuning
    {
        public float? Duration;          // how long the blast stays before it expires
        public float? Size;              // how big the AOE is
        public bool? Sticky;             // does AOE follow something e.g. roland's skill
        public AOEOffsetEnum? Offset;    // the offset this AOE will be placing
        public int? Range;               // how far a AOE can reach (not a size)
    }

    public class MoveTuning : Tuning
    {
        public int? Range;               // how far a jump can reach
        public float? Duration;          // duration of the move
        public float? Spread;            // ClusteredCircle's landing radius, or ClusteredLaser's lane half-width
    }

    public class ProjectileTuning : Tuning
    {
        public int? Range;               // how far a projectile can reach
        public float? Spread;            // ClusteredCircle's blast radius, or ClusteredLaser's beam half-width
        public float? Size;              // how big projectile is
    }

    public class FireTimingRunnerTuning : Tuning
    {
        public int? Count;                  // number of repeat time
        public FireTimingModeEnum? Mode;    // fire once or in sequence
        public float? Interval;             // delay between shots - only read when Mode is Sequence
        public Tuning InnerTuning;          // tune for the template action that'll be fired
    }

    // FireTimingRunner with Projectile
    // FLAGGING: Currently use in 1 scenario e.g. Sparks attack random enemies in 2 hex of current target
    public class FireTimingRunnerProjectileTuning : FireTimingRunnerTuning
    {
        public int? RandomPoolRadius;    
    }
}
