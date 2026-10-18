using System.Collections.Generic;

namespace MagicSchool.Contracts
{
    // IAligned answer: who stands on a unit's special marks, for the skill that pays off them (See AlignMark).
    // Counted by AlignBonuses.Refresh, so like every aligning bonus it is fixed by where everyone stood when the fight started.
    // e.g.     Skeleton Archer's long shots deal 15% more for every ally on his "Long Shot" mark
    // e.g.     Rock Golem shields the allies on his "Ward" mark
    public interface IAligned
    {
        // the allies standing on the hexes of the special mark of this name; empty when there is no such mark
        IReadOnlyList<ICombatant> AlliesOnSpecialMark(string mark);
    }
}
