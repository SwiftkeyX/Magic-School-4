using UnityEngine;
using MagicSchool.VFX;
using MagicSchool.Contracts;

namespace MagicSchool.Combat.Heroes
{
    // A dead player hero is hidden, not destroyed: it is revived for the next stage and drawn again then. With nobody
    // watching (a benchmark, Hero.IsDrawn false) the bars are not made at all, as they would still update every frame.
    [RequireComponent(typeof(SpriteRenderer))]
    internal class HeroVisuals : MonoBehaviour
    {
        private static readonly Color BlueSkillTextColor = new Color(0.4f, 0.85f, 1f);
        private static readonly Color RedSkillTextColor = new Color(1f, 0.55f, 0.3f);

        [SerializeField] private FloatingText _skillCastTextPrefab;

        private Hero _me;
        private SpriteRenderer _sprite;

        void Awake()
        {
            _me = GetComponent<Hero>();
            _sprite = GetComponent<SpriteRenderer>();

            if (!Hero.IsDrawn)
            {
                foreach (Canvas bar in GetComponentsInChildren<Canvas>(true)) bar.gameObject.SetActive(false);
            }
        }

        void LateUpdate()
        {
            if (_me == null || !_me.IsInitialized || !Hero.IsDrawn) return;

            bool isShown = _me.IsAlive && !_me.HasStatus(ModifierEnum.Untargetable);
            if (_sprite.enabled == isShown) return;
            _sprite.enabled = isShown;

            foreach (Canvas bar in GetComponentsInChildren<Canvas>(true)) bar.enabled = isShown;
        }

        public void PlaySkillCastEffect(string skillName)
        {
            if (_skillCastTextPrefab == null || !Hero.IsDrawn) return;

            FloatingText instance = Instantiate(_skillCastTextPrefab);
            Color color = _me.Team == TeamEnum.Blue ? BlueSkillTextColor : RedSkillTextColor;
            instance.Show(skillName, color, _me.transform.position);
        }
    }
}
