using System.Collections.Generic;
using MagicSchool.Contracts;
using MagicSchool.Combat.Heroes;
using MagicSchool.Combat.Placements;

namespace MagicSchool.Combat.Align
{
    /// <summary>
    /// The aligning mechanic: a hero marks some of the hexes around it, and an ally standing on a marked hex will grant a bonus.
    /// </summary>
    
    // Get hexes that was marked by aligning mechanic 
    public static class AligningHex
    {
        // Get the list of marked hex by the specify hero
        public static List<Hex> MarkedHexes(Hero giver, Hex standingOn)
        {
            var marked = new List<Hex>();

            foreach (AlignMark mark in giver.Data.AlignMarks)
            {
                Hex hex = standingOn.GetNeighbor(mark.direction);
                if (hex != null) marked.Add(hex);
            }

            return marked;
        }

        // Get the list of makred hex from a specify team.
        // `except` is left out: a hero being dragged is still tracked on the hex it was picked up from.
        public static List<Hex> MarkedHexes(BattleBoard board, TeamEnum team, Hero except = null)
        {
            var marked = new List<Hex>();

            foreach (ICombatant combatant in board.HeroesOnBoard)
            {
                if (!(combatant is Hero giver) || giver == except || giver.Team != team) continue;
                if (giver.CurrentHex == null) continue;

                marked.AddRange(MarkedHexes(giver, giver.CurrentHex));
            }

            return marked;
        }
    }
}
