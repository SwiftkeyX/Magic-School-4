using UnityEngine;
using MagicSchool.VFX;
using MagicSchool.Contracts;

namespace MagicSchool.Combat.Heroes
{
    /// <summary>
    /// The presentation layer for hero - as opposed to the state Hero itself holds, or the logic layer (the states).
    /// This is still unstable though. I think it would be changed a lot from now.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    internal class HeroVisuals : MonoBehaviour
    {
        private const float DeadAlpha = 0.3f;
        private const float AliveAlpha = 1f;

        private static readonly Color BlueSkillTextColor = new Color(0.4f, 0.85f, 1f);
        private static readonly Color RedSkillTextColor = new Color(1f, 0.55f, 0.3f);

        private static readonly Color BurnTint = new Color(1f, 0.45f, 0.1f);
        private const float BurnTintStrength = 0.65f;

        [SerializeField] private FloatingText _skillCastTextPrefab;

        private Hero _me;
        private SpriteRenderer _sprite;
        private Color _ownColor;        // the colour this hero has when nothing tints him
        private bool _isTinted;

        void Awake()
        {
            _me = GetComponent<Hero>();
            _sprite = GetComponent<SpriteRenderer>();
            _ownColor = _sprite.color;
        }

        void LateUpdate()
        {
            if (_me == null || !_me.IsInitialized) return;

            HideWhileVanished();
            TintWhileBurning();
        }

        // if a hero is vanished, don't draw him 
        private void HideWhileVanished()
        {
            bool isShown = !_me.HasStatus(ModifierEnum.Untargetable);
            if (_sprite.enabled == isShown) return;
            _sprite.enabled = isShown;

            // don't draw the canvas of him too.
            foreach (Canvas bar in GetComponentsInChildren<Canvas>(true)) bar.enabled = isShown;
        }

        // if a hero is burning, draw him orange. The alpha is left alone, the dead visual owns it.
        private void TintWhileBurning()
        {
            bool isBurning = _me.IsAlive && _me.HasStatus(ModifierEnum.Burn);
            if (_isTinted == isBurning) return;
            _isTinted = isBurning;

            Color c = isBurning ? Color.Lerp(_ownColor, BurnTint, BurnTintStrength) : _ownColor;
            c.a = _sprite.color.a;
            _sprite.color = c;
        }

        // When hero die, set his sprite transparent to indicate that he is dead.
        public void SetDeadVisual()
        {
            Color c = _sprite.color;
            c.a = DeadAlpha;
            _sprite.color = c;
        }

        // Counterpart to SetDeadVisual, no longer set the sprite to transparent
        public void SetAliveVisual()
        {
            Color c = _sprite.color;
            c.a = AliveAlpha;
            _sprite.color = c;
        }

        // Floating skill-name text above the hero when its skill actually casts
        public void PlaySkillCastEffect(string skillName)
        {
            if (_skillCastTextPrefab == null) return;

            FloatingText instance = Instantiate(_skillCastTextPrefab);
            Color color = _me.Team == TeamEnum.Blue ? BlueSkillTextColor : RedSkillTextColor;
            instance.Show(skillName, color, _me.transform.position);
        }
    }
}
