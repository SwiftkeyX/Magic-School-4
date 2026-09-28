using UnityEngine;

namespace MagicSchool.Engine
{
    /// <summary>
    /// To move "something" from A to B, paced by an AnimationCurve.
    /// e.g. HeroWalk uses it to step one hex, the Move template action to jump across several. 
    /// </summary>
    public struct CurveMotion
    {
        private readonly Vector3 _start;
        private readonly Vector3 _end;
        private readonly float _duration;
        private readonly AnimationCurve _curve;
        private float _elapsed;

        public CurveMotion(Vector3 start, Vector3 end, float duration, AnimationCurve curve)
        {
            _start = start;
            _end = end;
            _duration = Mathf.Max(duration, Mathf.Epsilon);
            _curve = curve;
            _elapsed = 0f;
        }

        public bool IsFinished => _elapsed >= _duration;

        public Vector3 End => _end;

        public Vector3 Tick(float deltaTime)
        {
            _elapsed += deltaTime;

            if (IsFinished) return _end;

            float t = _elapsed / _duration;
            float progress = _curve != null ? _curve.Evaluate(t) : t;

            return Vector3.Lerp(_start, _end, progress);
        }
    }
}
