using System.Collections.Generic;
using UnityEngine;
using MagicSchool.Contracts;
using MagicSchool.Combat.Align;
using MagicSchool.Combat.Heroes.Stats;
using MagicSchool.Skills;

namespace MagicSchool.Combat.Heroes
{
    /// <summary>
    /// SO for hero.
    /// ONLY use to seed a Hero's runtime state AND SkillSO.
    /// </summary>
    [CreateAssetMenu(fileName = "HeroStat", menuName = "Magic School 4/Hero Stat")]
    public class HeroDataSO : ScriptableObject
    {
        [SerializeField] private GameObject _prefab;
        [SerializeField] private string _name = "Hero A";
        [SerializeField] private HeroTierEnum _tier = HeroTierEnum.Common;
        [SerializeField] private HeroRoleEnum _role = HeroRoleEnum.None;
        [SerializeField] private int _hp = 500;
        [SerializeField] private int _attack = 40;
        [SerializeField] private int _defend = 20;
        [SerializeField] private int _magic = 100;
        // FIXLATER: MR isn't need, get rid of if
        [SerializeField] private int _magicResist = 20;
        [SerializeField] private float _attackSpeed = 0.7f;
        [SerializeField] private int _range = 1;
        [SerializeField] private int _startMana = 0;
        [SerializeField] private int _maxMana = 50;
        [SerializeField] private float _critChance = 25f;   
        [SerializeField] private SkillIdEnum _skillId = SkillIdEnum.None;
        [Tooltip("The unit this hero's skill puts on the board. e.g. Big Slime -> Small Slime. Empty for almost everyone.")]
        [SerializeField] private HeroDataSO _spawnUnit;
        [Tooltip("Aligning: every way this hero can be turned, each with its own marks. Rotating switches to the next one. Edit in Magic School 4 > Align Editor.")]
        [SerializeField] private List<AlignPattern> _alignPatterns = new List<AlignPattern>();
        // FLAGGING: a dummy should be its own unit type, not a Hero. Let's see later if it make sense.
        [SerializeField] private bool _isDummy = false; // A dummy never walks or attacks 

        // ===================== setter & getter =====================
        public GameObject Prefab => _prefab;
        public string Name => _name;
        public HeroTierEnum Tier => _tier;
        public int Price => HeroPrice.Of(_tier);
        public HeroRoleEnum Role => _role;
        public int HP => _hp;
        public int Atk => _attack;
        public int DF => _defend;
        public int MG => _magic;
        public int MR => _magicResist;
        public float AttackSpeed => _attackSpeed;
        public int Range => _range;
        public int StartMana => _startMana;
        public int MaxMana => _maxMana;
        public float CritChance => _critChance;
        public SkillIdEnum SkillId => _skillId;
        public HeroDataSO SpawnUnit => _spawnUnit;

        // a hero with no pattern still counts as one, with no marks: it cannot rotate
        public int AlignPatternCount => Mathf.Max(1, _alignPatterns.Count);

        // the marks of one pattern; none when there is no such pattern
        // the special marks this hero's skill reads (See AlignMark), for the Align Editor
        public IReadOnlyList<string> SpecialMarksOf(TemplateActionRegistrySO registry)
            => SkillLibrary.Resolve(_skillId, registry)?.SpecialMarks ?? new string[0];

        public IReadOnlyList<AlignMark> AlignMarksOf(int pattern)
            => pattern >= 0 && pattern < _alignPatterns.Count && _alignPatterns[pattern].marks != null
                ? _alignPatterns[pattern].marks
                : NoMarks;

        private static readonly List<AlignMark> NoMarks = new List<AlignMark>();

        public bool IsDummy => _isDummy;
    }
}
