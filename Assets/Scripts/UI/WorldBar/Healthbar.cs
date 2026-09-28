using UnityEngine;
using UnityEngine.UI;
using MagicSchool.Contracts;

namespace MagicSchool.UI
{
    // World-space health bar - green for the player's side, red for the other one.
    internal class Healthbar : WorldBar
    {
        private Image _image;

        // ====================================== override ======================================
        // fill slider with hero's hp value
        protected override float Fill => (float)_hero.CurrentHP / _hero.MaxHP;

        // ====================================== life cycle ======================================
        protected override void Awake()
        {
            base.Awake();
            _image = _slider.fillRect.GetComponent<Image>();
        }

        void Start()
        {
            if (_hero.Team == TeamEnum.Blue) _image.color = Color.green;

            else if (_hero.Team == TeamEnum.Red) _image.color = Color.red;
        }
    }
}
