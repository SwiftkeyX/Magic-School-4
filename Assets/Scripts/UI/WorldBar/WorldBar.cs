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

        // ====================================== abstract ======================================
        // what this bar reads off the hero. range from 0 - 1.
        protected abstract float Fill { get; }

        // to hide/show worldbar 
        protected virtual bool IsShown => true;

        // helper
        private bool? _wasShown;

        // ====================================== setter ======================================
        // set offset to the worldbar away from its original position
        public void SetOffset(Vector3 offset) => _offset = offset;

        // ====================================== life cycle ======================================
        protected virtual void Awake() => _hero = GetComponentInParent<Hero>();

        // update worldbar position/visibility 
        protected virtual void LateUpdate()
        {
            if (_hero == null) _hero = GetComponentInParent<Hero>();

            // update position
            transform.position = _hero.transform.position + _offset;

            // guard
            if (!_hero.IsInitialized) return;

            bool shown = IsShown;

            // if the worldbar never shown before, show them
            if (shown != _wasShown)
            {
                _wasShown = shown;
                ShowGraphics(shown);
            }

            // update worldbar slider
            if (shown) _slider.value = Fill;
        }

        // ====================================== private ======================================
        // turn on/off worldbar
        private void ShowGraphics(bool shown)
        {
            for (int i = 0; i < transform.childCount; i++)
                transform.GetChild(i).gameObject.SetActive(shown);
        }
    }
}
