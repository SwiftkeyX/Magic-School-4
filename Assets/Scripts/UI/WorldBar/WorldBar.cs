using UnityEngine;
using UnityEngine.UI;
using MagicSchool.Combat.Heroes;

namespace MagicSchool.UI
{
    /// <summary>
    /// A world-space bar floating over a hero's head.
    /// </summary>

    /// ExecuteAlways lets the offset be tweaked live in edit mode.
    [ExecuteAlways]
    internal abstract class WorldBar : MonoBehaviour
    {
        [SerializeField] protected Slider _slider;
        [SerializeField] private Vector3 _offset = new Vector3(0f, 1f, 0f);

        protected Hero _hero;

        protected abstract float Fill { get; }

        protected virtual bool IsShown => true;

        private bool? _wasShown;

        public void SetOffset(Vector3 offset) => _offset = offset;

        protected virtual void Awake() => _hero = GetComponentInParent<Hero>();

        protected virtual void LateUpdate()
        {
            if (_hero == null) _hero = GetComponentInParent<Hero>();

            transform.position = _hero.transform.position + _offset;

            if (!_hero.IsInitialized) return;

            bool shown = IsShown;

            if (shown != _wasShown)
            {
                _wasShown = shown;
                ShowGraphics(shown);
            }

            if (shown) _slider.value = Fill;
        }

        private void ShowGraphics(bool shown)
        {
            for (int i = 0; i < transform.childCount; i++)
                transform.GetChild(i).gameObject.SetActive(shown);
        }
    }
}
