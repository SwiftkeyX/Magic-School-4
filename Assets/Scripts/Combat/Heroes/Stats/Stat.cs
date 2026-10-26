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
    // Current HP follows max HP both ways: +1000 max at 500/1000 is 1500/2000, and losing it again is 500/1000. Losing
    // max HP never kills (nobody landed a killing hit): a living hero keeps at least 1 HP, and what that floor saved
    // is owed, paid out of the max HP he gets back before any of it returns as HP - 20 HP, lose 80 max, 1 HP, get the
    // 80 back, 20 HP, not 81. It used to clamp only on a loss, so a hurt hero lost nothing while a steal lasted and
    // gained HP when it ended.
    // An expiring keepsHealth modifier (See CustomModifier) changes max HP without current HP following, and a
    // returned loss that keeps its health does not pay back what was owed either. Current mana follows StartMana the
    // same way. Mana comes over time (See ManaRegen); an auto attack gives none by default, but ManaPerAttack stays a
    // stat so an item, mark or skill can give it.
    internal class Stat
    {
        private readonly ModifierResolver _statModifier = new ModifierResolver();
        private readonly Dictionary<StatEnum, float> _base = new Dictionary<StatEnum, float>();
        private int _currentHP;
        private int _currentMana;
        private int _hpOwed;

        private const float BaseManaPerSecond = 5f;
        private const int BaseManaPerAttack = 0;

        public float GetBaseStat(StatEnum type) => _base[type];
        public bool IsManaCapped() => _currentMana >= MaxMana;

        public void SetCurrentHP(int value) => _currentHP = Mathf.Clamp(value, 0, MaxHP);
        public void AddMana(int amount) => _currentMana += amount;
        public void SpendMana() => _currentMana = 0;

        public float GetFinalStat(StatEnum type) => _statModifier.GetStatModifier(type, _base[type]);
        public int MaxHP => Mathf.RoundToInt(GetFinalStat(StatEnum.MaxHP));
        public int Atk => Mathf.RoundToInt(GetFinalStat(StatEnum.ATK));
        public int DF => Mathf.RoundToInt(GetFinalStat(StatEnum.DF));
        public int MG => Mathf.RoundToInt(GetFinalStat(StatEnum.AP));
        public float AttackSpeed => GetFinalStat(StatEnum.AS);
        public int Range => Mathf.RoundToInt(GetFinalStat(StatEnum.Range));
        public int StartMana => Mathf.RoundToInt(GetFinalStat(StatEnum.StartMana));
        public int MaxMana => Mathf.RoundToInt(GetFinalStat(StatEnum.MaxMana));
        public int ManaPerAttack => Mathf.RoundToInt(GetFinalStat(StatEnum.ManaPerAttack));
        public float ManaPerSecond => GetFinalStat(StatEnum.ManaPerSecond);
        public float HPPerSecond => GetFinalStat(StatEnum.HPPerSecond);
        public float ArmorPen => GetFinalStat(StatEnum.ArmorPen);
        public float Lethality => GetFinalStat(StatEnum.Lethality);
        public float DamageReductionPercent => Mathf.RoundToInt(GetFinalStat(StatEnum.DamageReduction));
        public int CurrentHP => _currentHP;
        public int CurrentMana => _currentMana;

        public bool HasStatus(ModifierEnum type) => _statModifier.GetStatusModifier(type);
        public bool IsStunned => HasStatus(ModifierEnum.Stun);
        public bool IsWounded => HasStatus(ModifierEnum.Wound);

        public int Shield => Mathf.FloorToInt(_statModifier.ShieldTotal);
        public void ConsumeShield(int amount) => _statModifier.AbsorbDamage(amount);

        public float ModifierRemaining(int index) => _statModifier.GetRemainingDuration(index);
        public int ActiveModifierCount => _statModifier.ActiveCount;

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

            int lastMaxHP = previousMaxHP;
            int keptChange = 0;
            _statModifier.Tick(deltaTime, expired =>
            {
                int nowMaxHP = MaxHP;
                if (expired is CustomModifier custom && custom.KeepsHealth) keptChange += nowMaxHP - lastMaxHP;
                lastMaxHP = nowMaxHP;
            });

            if (keptChange > 0) _hpOwed -= Mathf.Min(_hpOwed, keptChange);

            FollowMaxHP(previousMaxHP + keptChange);
        }

        private void FollowMaxHP(int previousMaxHP)
        {
            if (_currentHP <= 0) return;

            int change = MaxHP - previousMaxHP;
            if (change > 0)
            {
                int paid = Mathf.Min(_hpOwed, change);
                _hpOwed -= paid;
                change -= paid;
            }

            int followed = _currentHP + change;
            if (followed < 1)
            {
                _hpOwed += 1 - followed;
                followed = 1;
            }

            _currentHP = Mathf.Min(followed, Mathf.Max(1, MaxHP));
        }

        private void FollowStartMana(int previousStartMana) => _currentMana = Mathf.Clamp(_currentMana + StartMana - previousStartMana, 0, MaxMana);

        public Stat(HeroDataSO so, int star = HeroStar.Min, int level = HeroLevel.Min)
        {
            float leveled = HeroLevel.Multiplier(level);
            _base[StatEnum.MaxHP] = so.HP * HeroStar.HpMultiplier(star) * leveled;
            _base[StatEnum.ATK] = so.Atk * HeroStar.AtkMultiplier(star) * leveled;
            _base[StatEnum.DF] = so.DF * leveled;
            _base[StatEnum.AP] = so.MG * HeroStar.ApMultiplier(star) * leveled;
            _base[StatEnum.AS] = so.AttackSpeed;
            _base[StatEnum.Range] = so.Range;
            _base[StatEnum.StartMana] = so.StartMana;
            _base[StatEnum.MaxMana] = so.MaxMana;
            _base[StatEnum.DamageReduction] = 0f;
            _base[StatEnum.CritChance] = so.CritChance;
            _base[StatEnum.Omnivamp] = 0f;
            _base[StatEnum.ManaPerAttack] = BaseManaPerAttack;
            _base[StatEnum.ManaPerSecond] = BaseManaPerSecond;
            _base[StatEnum.HPPerSecond] = 0f;
            _base[StatEnum.ArmorPen] = 0f;
            _base[StatEnum.Lethality] = 0f;

            _currentHP = MaxHP;

            _currentMana = Mathf.Min(StartMana, MaxMana);
        }
    }
}
