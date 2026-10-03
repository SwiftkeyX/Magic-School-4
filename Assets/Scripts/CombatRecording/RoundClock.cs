using UnityEngine;

namespace MagicSchool.CombatRecording
{
    // count the time in a round
    internal class RoundClock
    {
        private readonly float _start;
        private float? _end;

        public RoundClock()
        {
            _start = Time.time;
        }

        public float Elapsed => (_end ?? Time.time) - _start;

        public void Stop()
        {
            if (_end == null) _end = Time.time;
        }
    }
}
