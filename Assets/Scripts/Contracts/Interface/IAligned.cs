using System.Collections.Generic;

namespace MagicSchool.Contracts
{
    // IAligned answer: who stands on a unit's special marks, for the skill that pays off them (See AlignMark).
    // Counted by AlignBonuses.Refresh, so like every aligning bonus it is fixed by where everyone stood when the fight started.
    // e.g.     Skeleton Archer's long shots deal 15% more for every ally on his "Long Shot" mark
    // e.g.     Rock Golem shields the allies on his "Ward" mark
    // AlliesOnSpecialMark is empty when the unit has no mark of that name.
    public interface IAligned
    {
        IReadOnlyList<ICombatant> AlliesOnSpecialMark(string mark);
    }
}
