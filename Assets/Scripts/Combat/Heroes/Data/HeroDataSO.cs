using UnityEngine;
using MagicSchool.Combat.Heroes.Stats;
using MagicSchool.Skills;

namespace MagicSchool.Combat.Heroes
{
    /// <summary>
    /// SO for hero.
    /// ONLY use to seed a Hero's runtime state AND SkillSO.
    /// </summary>
    [CreateAssetMenu(fileName = "HeroStat", menuName = "Magic School 3/Hero Stat")]
    public class HeroDataSO : ScriptableObject
    {
        [SerializeField] private GameObject _prefab;
        [SerializeField] private string _name = "Hero A";
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
        // which skill in SkillLibrary this hero runs. None means no skill at all, e.g. a dummy.
        [SerializeField] private SkillIdEnum _skillId = SkillIdEnum.None;
        // FLAGGING: a dummy should be its own unit type, not a Hero. Let's see later if it make sense.
        [SerializeField] private bool _isDummy = false; // A dummy never walks or attacks 

        // ===================== setter & getter =====================
        public GameObject Prefab => _prefab;
        public string Name => _name;
        public int HP => _hp;
        public int Atk => _attack;
        public int DF => _defend;
        public int MG => _magic;
        public int MR => _magicResist;
        public float AttackSpeed => _attackSpeed;
        public int Range => _range;
        public int StartMana => _startMana;
        public int MaxMana => _maxMana;
        public SkillIdEnum SkillId => _skillId;
        public bool IsDummy => _isDummy;
    }
}
