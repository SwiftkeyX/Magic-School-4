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

        [SerializeField] private FloatingText _skillCastTextPrefab;

        private Hero _me;
        private SpriteRenderer _sprite;

        void Awake()
        {
            _me = GetComponent<Hero>();
            _sprite = GetComponent<SpriteRenderer>();
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
