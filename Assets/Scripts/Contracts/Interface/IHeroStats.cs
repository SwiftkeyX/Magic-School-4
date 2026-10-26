namespace MagicSchool.Contracts
{
    // IHeroStats answer: how much is hero stat?
    // Star is 1-3: how many times upgraded, plus 1 (See HeroStar).
    public interface IHeroStats
    {
        float GetStat(StatEnum type);       
        float GetBaseStat(StatEnum type);

        int CurrentHP { get; }
        int MaxHP { get; }
        int CurrentMana { get; }
        int MaxMana { get; }
        int AttackDamage { get; }
        int Defence { get; }
        int Magic { get; }
        float AttackSpeed { get; }
        int Range { get; }
        int Star { get; }
    }
}
