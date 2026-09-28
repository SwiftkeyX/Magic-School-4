namespace MagicSchool.Skills
{
    /// <summary>
    /// Not cadence => apply effect when hitbox collide (OnTriggerEnter())
    /// cadence but start only initial collision => e.g. pip's mushroom (after OnTriggerEnter(), start coroutine for the poison damage)
    /// cadence but not need initial collision => e.g. roland's E (OnTriggerEnter() trigger every interval)
    /// </summary>
    public class Cadence
    {
        public bool isCadence = false;

        // neither is read unless isCadence
        public float cadenceInterval = 0.5f;    // the interval of time effect is re-apply
        public float cadenceDuration = 3f;      // total duration of the effect

        public Cadence() { }

        public Cadence(float interval, float duration)
        {
            isCadence = true;
            cadenceInterval = interval;
            cadenceDuration = duration;
        }
    }
}
