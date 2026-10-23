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
        private int _hpOwed; // HP a max HP loss could not take without killing (See FollowMaxHP)

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

            // the max HP change from expired modifiers that keep their health (See CustomModifier.KeepsHealth):
            // a lost bonus (the thief's) or a returned loss (the victim's) that current HP does not follow
            int lastMaxHP = previousMaxHP;
            int keptChange = 0;
            _statModifier.Tick(deltaTime, expired =>
            {
                int nowMaxHP = MaxHP;
                if (expired is CustomModifier custom && custom.KeepsHealth) keptChange += nowMaxHP - lastMaxHP;
                lastMaxHP = nowMaxHP;
            });

            // a returned loss that keeps its health does not pay back the HP it was owed either (See FollowMaxHP)
            if (keptChange > 0) _hpOwed -= Mathf.Min(_hpOwed, keptChange);

            // as if the kept part never changed; current HP is still capped at the new max
            FollowMaxHP(previousMaxHP + keptChange);
        }

        // currentHP moves with MaxHP, both ways
        // e.g. hero have 500/1000 hp, he get modifier resulting in 1500/2000 hp; losing it again takes him back to 500/1000.
        // Losing max HP never kills: a living hero keeps at least 1 HP, since no one landed a killing hit.
        // What that floor saved is owed, and paid out of the max HP he gets back before any of it returns as HP,
        // so 20 HP -> lose 80 max -> 1 HP -> get the 80 back -> 20 HP, not 81.
        // It used to only clamp on a loss, so a hurt hero lost nothing while a steal lasted and gained HP when it ended.
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

        // the same to FollowMaxHP()
        private void FollowStartMana(int previousStartMana) => _currentMana = Mathf.Clamp(_currentMana + StartMana - previousStartMana, 0, MaxMana);

        // star: the hero's star level, which raises its base HP, AD and AP (See HeroStar)
        public Stat(HeroDataSO so, int star = HeroStar.Min)
        {
            _base[StatEnum.MaxHP] = so.HP * HeroStar.HpMultiplier(star);
            _base[StatEnum.ATK] = so.Atk * HeroStar.AtkMultiplier(star);
            _base[StatEnum.DF] = so.DF;
            _base[StatEnum.AP] = so.MG * HeroStar.ApMultiplier(star);
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
