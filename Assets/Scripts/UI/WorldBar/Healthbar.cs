using UnityEngine;
using UnityEngine.UI;
using MagicSchool.Contracts;

namespace MagicSchool.UI
{
    // World-space health bar - green for the player's side, red for the other one.
    // A shield shows as a white section after the HP, [HP][shield]; when HP + shield is more than max HP, the bar
    // rescales to fit both.
    internal class Healthbar : WorldBar
    {
        [SerializeField] private RectTransform _shieldFill;

        private Image _image;

        private float Total => Mathf.Max(_hero.MaxHP, _hero.CurrentHP + _hero.Shield);

        protected override float Fill => Total > 0f ? _hero.CurrentHP / Total : 0f;

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

        protected override void LateUpdate()
        {
            base.LateUpdate();

            if (_shieldFill == null || _hero == null || !_hero.IsInitialized) return;

            float start = Fill;
            float end = Total > 0f ? (_hero.CurrentHP + _hero.Shield) / Total : 0f;

            _shieldFill.anchorMin = new Vector2(start, 0f);
            _shieldFill.anchorMax = new Vector2(end, 1f);
            _shieldFill.gameObject.SetActive(_hero.Shield > 0);
        }
    }
}
