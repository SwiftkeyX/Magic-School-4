using System.Collections.Generic;
using UnityEngine;
using MagicSchool.Contracts;
using MagicSchool.Modifiers;

namespace MagicSchool.Combat.Heroes.Stats
{
    /// <summary>
    /// Stat of a hero.
    ///
    /// This class should only contain data and its getter & setter only.
    /// NO LOGIC.
    /// </summary>
    internal class Stat
    {
        private readonly ModifierResolver _statModifier = new ModifierResolver();
        private readonly Dictionary<StatEnum, float> _base = new Dictionary<StatEnum, float>();
        private int _currentHP;
        private int _currentMana;

        private const int BaseManaPerAttack = 10;

        // ========================================== stat getter ==========================================
        public float GetBaseStat(StatEnum type) => _base[type];
        public bool IsManaCapped() => _currentMana >= MaxMana;

        // ========================================== stat setter ==========================================
        public void SetCurrentHP(int value) => _currentHP = Mathf.Clamp(value, 0, MaxHP);
        public void AddMana(int amount) => _currentMana += amount;
        public void SpendMana() => _currentMana = 0;

        // ========================================== modifier getter ==========================================
        // === final stat after modifier calculation ===
        public float GetFinalStat(StatEnum type) => _statModifier.GetStatModifier(type, _base[type]);
        public int MaxHP => Mathf.RoundToInt(GetFinalStat(StatEnum.MaxHP));
        public int Atk => Mathf.RoundToInt(GetFinalStat(StatEnum.ATK));
        public int DF => Mathf.RoundToInt(GetFinalStat(StatEnum.DF));
        public int MG => Mathf.RoundToInt(GetFinalStat(StatEnum.AP));
        public int MR => Mathf.RoundToInt(GetFinalStat(StatEnum.MR));
        public float AttackSpeed => GetFinalStat(StatEnum.AS);
        public int Range => Mathf.RoundToInt(GetFinalStat(StatEnum.Range));
        public int StartMana => Mathf.RoundToInt(GetFinalStat(StatEnum.StartMana));
        public int MaxMana => Mathf.RoundToInt(GetFinalStat(StatEnum.MaxMana));
        public int ManaPerAttack => Mathf.RoundToInt(GetFinalStat(StatEnum.ManaPerAttack));
        public float DamageReductionPercent => Mathf.RoundToInt(GetFinalStat(StatEnum.DamageReduction));
        public int CurrentHP => _currentHP;
        public int CurrentMana => _currentMana;

        // === status modifier ===
        public bool HasStatus(ModifierEnum type) => _statModifier.GetStatusModifier(type);
        public bool IsStunned => HasStatus(ModifierEnum.Stun);
        public bool IsWounded => HasStatus(ModifierEnum.Wound);

        // === shield ===
        public int Shield => Mathf.FloorToInt(_statModifier.ShieldTotal);
        public void ConsumeShield(int amount) => _statModifier.AbsorbDamage(amount);

        // === other ===
        public float ModifierRemaining(int index) => _statModifier.GetRemainingDuration(index);
        public int ActiveModifierCount => _statModifier.ActiveCount;

        // ===  modifier setter ===
        public void AddModifier(ICustomModifier modifier, float amplifier, IHeroStats casterStats, IHeroStats recipientStats)
        {
            int previousMaxHP = MaxHP;
            int previousStartMana = StartMana;
            _statModifier.AddModifier(modifier, amplifier, casterStats, recipientStats);
            FollowMaxHP(previousMaxHP);
            FollowStartMana(previousStartMana);
        }

        public bool RemoveModifier(ICustomModifier modifier)
        {
            int previousMaxHP = MaxHP;
            int previousStartMana = StartMana;
            bool removed = _statModifier.RemoveModifier(modifier);
            FollowMaxHP(previousMaxHP);
            FollowStartMana(previousStartMana);
            return removed;
        }

        public void TickModifiers(float deltaTime)
        {
            int previousMaxHP = MaxHP;
            _statModifier.Tick(deltaTime);
            FollowMaxHP(previousMaxHP);
        }

        // whenever the modifier increase MaxHP, the currentHP is increase permanently according to MaxHP
        // e.g. hero have 500/1000 hp, he get modifier resulting in 1500/2000 hp
        private void FollowMaxHP(int previousMaxHP) => SetCurrentHP(_currentHP + Mathf.Max(0, MaxHP - previousMaxHP));

        // the same to FollowMaxHP()
        private void FollowStartMana(int previousStartMana) => _currentMana = Mathf.Clamp(_currentMana + StartMana - previousStartMana, 0, MaxMana);

        // star: the hero's star level, which raises its base HP, AD and AP (See HeroStar)
        public Stat(HeroDataSO so, int star = HeroStar.Min)
        {
            _base[StatEnum.MaxHP] = so.HP * HeroStar.HpMultiplier(star);
            _base[StatEnum.ATK] = so.Atk * HeroStar.AtkMultiplier(star);
            _base[StatEnum.DF] = so.DF;
            _base[StatEnum.AP] = so.MG * HeroStar.ApMultiplier(star);
            _base[StatEnum.MR] = so.MR;
            _base[StatEnum.AS] = so.AttackSpeed;
            _base[StatEnum.Range] = so.Range;
            _base[StatEnum.StartMana] = so.StartMana;
            _base[StatEnum.MaxMana] = so.MaxMana;
            _base[StatEnum.DamageReduction] = 0f;
            _base[StatEnum.CritChance] = so.CritChance;
            _base[StatEnum.Omnivamp] = 0f;
            _base[StatEnum.ManaPerAttack] = BaseManaPerAttack;

            // there's chance MaxHP is modified at the start, so set _currentHP to MaxHP accordingly  
            _currentHP = MaxHP;

            // there's chance StartMana is modified at the start, so set _currentMana to StartMana.
            _currentMana = Mathf.Min(StartMana, MaxMana);
        }
    }
}
