namespace MagicSchool.Contracts
{
    // IFallen answer: how a unit died, for the skills that care whose kill it was.
    // e.g.     Troll grows only from the kills of the allies on his white mark, and never from a summon's death
    public interface IFallen
    {
        // who dealt the hit that took it to 0 HP. null when nothing authored that hit
        IEffectable KilledBy { get; }

        // put on the board mid-fight by another unit's skill
        // e.g.     the Small Slimes a Big Slime splits into
        bool IsSummon { get; }
    }
}
