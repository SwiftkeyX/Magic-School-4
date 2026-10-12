using System;
using MagicSchool.Contracts;
using MagicSchool.Combat.Heroes;

namespace MagicSchool.Combat.Placements
{
    // for battle setup: which hero? on which team? standing where?.
    [Serializable]
    public struct HeroPlacement
    {
        public HeroDataSO dataSO;
        public TeamEnum team;
        public HexNumber hexPlacement;

        public HeroPlacement(HeroDataSO data, TeamEnum team, HexNumber hexPlacement)
        {
            this.dataSO = data;
            this.team = team;
            this.hexPlacement = hexPlacement;
        }
    }
}
