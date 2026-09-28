using System;
using MagicSchool.Contracts;

namespace MagicSchool.Combat.Placements
{
    // Which hex, named the way a designer thinks of the board: whose side, which column, which row.
    [Serializable]
    public struct HexNumber
    {
        public TeamEnum team;
        public int column;
        public int row;

        public HexNumber(TeamEnum team, int column, int row)
        {
            this.team = team;
            this.column = column;
            this.row = row;
        }
    }
}
