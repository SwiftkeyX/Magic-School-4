using System;
using MagicSchool.Contracts;
using MagicSchool.Combat.Heroes.Stats;
using MagicSchool.Skills;

namespace MagicSchool.Combat.Heroes
{
    // Hero is a inspectable. But hero's UI version can't be inspected. 
    // HeroPreview.cs help the UI version to be able to be inspected.
    public class HeroPreview : IInspectableHero
    {
        private readonly HeroDataSO _data;
        private readonly Stat _stat;
        private readonly HeroSkill _skill;
        private readonly Func<bool> _isStillOffered;

        public HeroPreview(HeroDataSO data, SkillDefinition skill, Func<bool> isStillOffered)
        {
            _data = data;
            _stat = new Stat(data);
            _isStillOffered = isStillOffered;
            _skill = new HeroSkill(null, skill);    
        }

        // === IInspectable ===
        public string DisplayName => _data.Name;
        public bool IsAlive => _isStillOffered == null || _isStillOffered();

        // === IInspectableHero ===
        public TeamEnum Team => TeamEnum.Blue;      // when bought, default to player's team
        public bool HasSkill => _skill.HasSkill;
        public string SkillName => _skill.SkillName;
        public string SkillDescription => _skill.Description;
        public bool HasAutoAttackPassive => _skill.HasAutoAttackPassive;
        public string PassiveDescription => _skill.PassiveDescription;

        // === IHeroStats ===
        public float GetStat(StatEnum type) => _stat.GetFinalStat(type);
        public float GetBaseStat(StatEnum type) => _stat.GetBaseStat(type);
        public int CurrentHP => _stat.CurrentHP;
        public int MaxHP => _stat.MaxHP;
        public int CurrentMana => _stat.CurrentMana;
        public int MaxMana => _stat.MaxMana;
        public int AttackDamage => _stat.Atk;
        public int Defence => _stat.DF;
        public int Magic => _stat.MG;
        public int MagicResist => _stat.MR;
        public float AttackSpeed => _stat.AttackSpeed;
        public int Range => _stat.Range;
    }
}
