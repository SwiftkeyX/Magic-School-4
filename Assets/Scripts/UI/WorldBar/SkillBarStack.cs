using System.Collections.Generic;
using UnityEngine;
using MagicSchool.Combat.Heroes;

namespace MagicSchool.UI
{
    /// <summary>
    /// Draws one SkillBar per CustomModifier running on the hero, 
    /// stacked upward above the health bar. 
    /// </summary>
    internal class SkillBarStack : MonoBehaviour
    {
        [SerializeField] private SkillBar _first;                                   // the bar already on the prefab
        [SerializeField] private Vector3 _offset = new Vector3(0f, 0.8f, 0f);       // where the lowest bar sits
        [SerializeField] private float _spacing = 0.1f;                             // gap to the next one up
        [SerializeField] private int _maxBars = 4;

        private readonly List<SkillBar> _bars = new List<SkillBar>();
        private Hero _hero;

        void Awake()
        {
            _hero = GetComponentInParent<Hero>();

            if (_first != null) _bars.Add(_first);
        }

        void LateUpdate()
        {
            if (_hero == null) _hero = GetComponentInParent<Hero>();
            if (_first == null || !_hero.IsInitialized) return;

            int wanted = Mathf.Min(_hero.ActiveModifierCount, _maxBars);

            while (_bars.Count < wanted)
            {
                SkillBar bar = Instantiate(_first, _first.transform.parent);
                bar.name = $"{_first.name} {_bars.Count}";
                _bars.Add(bar);
            }

            // every bar is told where it sits, including the ones past the end - they hide themselves
            for (int i = 0; i < _bars.Count; i++)
            {
                _bars[i].Bind(i);
                _bars[i].SetOffset(_offset + Vector3.up * (_spacing * i));
            }
        }
    }
}
