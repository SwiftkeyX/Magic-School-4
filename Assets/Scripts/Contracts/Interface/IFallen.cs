namespace MagicSchool.Contracts
{
    // IFallen answer: how a unit died, for the skills that care whose kill it was.
    // e.g.     Troll grows only from the kills of the allies on his white mark, and never from a summon's death
    // KilledBy is null when nothing authored the killing hit. IsSummon: put on the board mid-fight by another unit's
    // skill (the Small Slimes a Big Slime splits into).
    public interface IFallen
    {
        IEffectable KilledBy { get; }

        bool IsSummon { get; }
    }
}
