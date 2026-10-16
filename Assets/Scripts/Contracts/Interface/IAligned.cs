namespace MagicSchool.Contracts
{
    // IAligned answer: how well a unit's align marks are filled, for a skill that pays off them.
    // Counted by AlignBonuses.Refresh, so like every aligning bonus it is fixed by where everyone stood when the fight started.
    // e.g.     Skeleton Archer's shots deal 15% more for every ally on his first mark's hexes
    public interface IAligned
    {
        // how many allies stand on the hexes of this bonus: 0 = the first mark's (painted orange), 1 = the second's (green)
        // (See AligningHex.MarkedHexesByBonus)
        int AlliesOnMark(int bonus);
    }
}
