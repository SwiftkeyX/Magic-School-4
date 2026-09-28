using TMPro;
using UnityEngine;

namespace MagicSchool.VFX
{
    // Spawns above a hero, drifts upward, and fades out over its lifetime, then self-destroys.
    public class FloatingText : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _text;
        [SerializeField] private float _floatDistance = 1f;
        [SerializeField] private float _duration = 1.2f;
        [SerializeField] private AnimationCurve _fadeCurve = new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(1f, 0f));

        private Vector3 _start;
        private float _elapsed;
        private Color _baseColor;

        public void Show(string text, Color color, Vector3 worldPosition)
        {
            _text.text = text;
            _baseColor = color;
            _text.color = color;
            _start = worldPosition;
            transform.position = _start;
            _elapsed = 0f;
        }

        void Update()
        {
            // update time
            _elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(_elapsed / _duration);

            // make the text float up
            transform.position = _start + Vector3.up * (_floatDistance * t);

            // fade following the animation curve
            Color c = _baseColor;
            c.a = _baseColor.a * _fadeCurve.Evaluate(t);
            _text.color = c;

            // destroy
            if (t >= 1f) Destroy(gameObject);
        }
    }
}
