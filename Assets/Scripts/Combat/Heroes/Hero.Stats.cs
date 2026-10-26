using MagicSchool.Contracts;

namespace MagicSchool.Combat.Heroes
{
    // Hero: its numbers - IHeroStats, mana and the attack cooldown, all read from Stat.
    public partial class Hero
    {
        public bool IsAttackReady => _attackCooldown.IsReady(AttackSpeed);
        public void SpendAttack() => _attackCooldown.Spend();

        public void GainMana(int amount) => Stat.AddMana(amount);
        public bool IsManaCapped() => Stat.IsManaCapped();
        public void SpendMana() => Stat.SpendMana();
        public void TickModifiers(float deltaTime) => Stat.TickModifiers(deltaTime);

        public int CurrentHP => Stat.CurrentHP;
        public int MaxHP => Stat.MaxHP;
        public int Shield => Stat.Shield;
        public int CurrentMana => Stat.CurrentMana;
        public int MaxMana => Stat.MaxMana;
        public int AttackDamage => Stat.Atk;
        public int Defence => Stat.DF;
        public int Magic => Stat.MG;
        public float AttackSpeed => Stat.AttackSpeed;
        public int Range => Stat.Range;
        public bool IsStunned => Stat.IsStunned;
        public bool IsWounded => Stat.IsWounded;
        public float GetStat(StatEnum type) => Stat.GetFinalStat(type);
        public float GetBaseStat(StatEnum type) => Stat.GetBaseStat(type);
    }
}
