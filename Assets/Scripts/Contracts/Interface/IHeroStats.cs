namespace MagicSchool.Contracts
{
    // IHeroStats answer: how much is hero stat?
    public interface IHeroStats
    {
        float GetStat(StatEnum type);       
        float GetBaseStat(StatEnum type);

        // FLAGGING: since we have GetStat() now, all this below is no need. ???   
        int CurrentHP { get; }
        int MaxHP { get; }
        int CurrentMana { get; }
        int MaxMana { get; }
        int AttackDamage { get; }
        int Defence { get; }
        int Magic { get; }
        int MagicResist { get; }
        float AttackSpeed { get; }
        int Range { get; }
    }
}
