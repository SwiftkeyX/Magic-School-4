using System;
using MagicSchool.Combat.Heroes;

namespace MagicSchool.Combat.Placements
{
    // for battle setup: which hero? standing where?.
    [Serializable]
    public struct HeroPlacement
    {
        public HeroDataSO dataSO;
        public HexNumber hexPlacement;

        public HeroPlacement(HeroDataSO data, HexNumber hexPlacement)
        {
            this.dataSO = data;
            this.hexPlacement = hexPlacement;
        }
    }
}
