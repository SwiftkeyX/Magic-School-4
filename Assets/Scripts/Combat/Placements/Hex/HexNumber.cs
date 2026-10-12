using System;

namespace MagicSchool.Combat.Placements
{
    // Assign number to each hex
    // The board is a grid shape: column 0 is the far left, row 0 is the top of its column.
    [Serializable]
    public struct HexNumber
    {
        public int column;
        public int row;

        public HexNumber(int column, int row)
        {
            this.column = column;
            this.row = row;
        }
    }
}
