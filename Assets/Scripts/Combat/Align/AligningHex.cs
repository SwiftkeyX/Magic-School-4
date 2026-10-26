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
    // A mark's colour is its kind: orange pays the hero itself, green pays the ally standing there, a special mark
    // (white) pays nobody. A mark saved before kinds existed reads by position (See KindByPosition). A hex can be in
    // several marks: an ally there gets the green bonus and pays the hero the orange one (Elf: N and S are both).
    public static class AligningHex
    {
        public const int Orange = 0;
        public const int Green = 1;
        public const int Special = 2;
        public const int None = -1;

        public static List<Hex>[] MarkedHexesByBonus(Hero giver, Hex standingOn)
        {
            var hexesByBonus = new[] { new List<Hex>(), new List<Hex>(), new List<Hex>() };

            IReadOnlyList<AlignMark> marks = giver.AlignMarks;
            for (int i = 0; i < marks.Count; i++)
            {
                int colour = ColourOf(marks, i);
                if (colour != None) hexesByBonus[colour].AddRange(marks[i].GetMarkedHexes(standingOn));
            }

            return hexesByBonus;
        }

        public static int ColourOf(IReadOnlyList<AlignMark> marks, int index)
        {
            AlignMarkKindEnum kind = marks[index].kind != AlignMarkKindEnum.Unset ? marks[index].kind : KindByPosition(marks, index);
            switch (kind)
            {
                case AlignMarkKindEnum.Orange: return Orange;
                case AlignMarkKindEnum.Green: return Green;
                case AlignMarkKindEnum.Special: return Special;
                default: return None;
            }
        }

        public static AlignMarkKindEnum KindByPosition(IReadOnlyList<AlignMark> marks, int index)
        {
            if (marks[index].IsSpecial) return AlignMarkKindEnum.Special;
            if (index == 0) return AlignMarkKindEnum.Orange;
            if (index == 1) return AlignMarkKindEnum.Green;
            return AlignMarkKindEnum.Unused;
        }

        public static List<Hex> WearingHexes(Hero giver, Hex standingOn)
        {
            var wearing = new List<Hex>();

            foreach (AlignMark mark in giver.AlignMarks)
            {
                wearing.AddRange(mark.GetWearingHexes(standingOn));
            }

            return wearing;
        }

        public static List<Hex> WearingHexes(BattleBoard board, TeamEnum team)
        {
            var wearing = new List<Hex>();

            foreach (ICombatant combatant in board.HeroesOnBoard)
            {
                if (!(combatant is Hero hero) || hero.Team != team || hero.CurrentHex == null) continue;

                wearing.AddRange(WearingHexes(hero, hero.CurrentHex));
            }

            return wearing;
        }
    }
}
