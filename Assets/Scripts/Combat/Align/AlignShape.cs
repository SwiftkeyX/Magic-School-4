using System.Collections.Generic;
using UnityEngine;
using MagicSchool.Combat.Heroes;
using MagicSchool.Combat.Placements;

namespace MagicSchool.Combat.Align
{
    /// <summary>
    /// The whole shape of a hero's marks, for drawing only - every marked hex, the ones the board does not have included,
    /// so a mark that runs past the board edge still shows where it would be.
    /// The bonuses never read this: they follow AligningHex, where a missing hex on the way ends a line.
    /// e.g.    Shield Knight on the top row, a mark N 2    => a spot above the board, drawn as a gray hex
    /// e.g.    a mark N 3 whose N 2 is off the board but N 3 is back on it => that hex is drawn, but gives nothing (IsPastEdge)
    /// </summary>
    public static class AlignShape
    {
        public struct ShownHex
        {
            public Vector3 position;    // where the hex is, or would be
            public Hex hex;             // the board's hex there; null = off the board
            public bool isPastEdge;     // on the board, but its line went off the board on the way: it gives nothing
            public int colour;          // AligningHex.Orange / Green / Special
            public bool isWearing;

            public bool IsOffBoard => hex == null;
        }

        // A board hex counts as "there" when it is this close to a spot, in steps (same-column hexes are 1 step apart)
        private const float SameSpot = 0.3f;

        // Every hex of every mark the hero has, standing on [standingOn]. A hex in two marks comes twice, once per colour.
        public static List<ShownHex> ShownHexes(Hero giver, Hex standingOn, IEnumerable<Hex> boardHexes)
        {
            var shown = new List<ShownHex>();
            var board = new List<Hex>(boardHexes);
            float step = StepOf(standingOn);

            IReadOnlyList<AlignMark> marks = giver.AlignMarks;
            for (int i = 0; i < marks.Count; i++)
            {
                int colour = AligningHex.ColourOf(marks, i);
                if (colour == AligningHex.None || marks[i].hexes == null) continue;

                foreach (AlignHex alignHex in marks[i].hexes)
                {
                    // the bonus's own answer wins: a hex it reaches is that hex, nothing to work out
                    Hex reached = alignHex.GetHex(standingOn);
                    Vector3 position = reached != null ? reached.transform.position : SpotOf(alignHex, standingOn, step);
                    Hex there = reached != null ? reached : HexAt(board, position, step);

                    shown.Add(new ShownHex
                    {
                        position = position,
                        hex = there,
                        isPastEdge = reached == null && there != null,
                        colour = colour,
                        isWearing = alignHex.isWearing,
                    });
                }
            }

            return shown;
        }

        // Where an AlignHex lands from [from], walked on the grid rather than across the board's hexes, so it never stops at an edge.
        // A turn is walked first-direction-first, the way AlignHex.GetHex tries it first.
        private static Vector3 SpotOf(AlignHex alignHex, Hex from, float step)
        {
            Vector3 spot = from.transform.position + OffsetOf(alignHex.direction, step) * alignHex.Distance;
            if (alignHex.HasTurn) spot += OffsetOf(alignHex.thenDirection, step) * alignHex.thenDistance;

            return spot;
        }

        // One step in a direction on the flat-top grid: up and down a column is [step], a diagonal is a column across and half a step up or down
        // (See Hex.AngleOf: 26.57 degrees is atan(0.5))
        private static Vector3 OffsetOf(HexDirectionEnum direction, float step)
        {
            switch (direction)
            {
                case HexDirectionEnum.N: return new Vector3(0f, step);
                case HexDirectionEnum.NE: return new Vector3(step, step / 2f);
                case HexDirectionEnum.SE: return new Vector3(step, -step / 2f);
                case HexDirectionEnum.S: return new Vector3(0f, -step);
                case HexDirectionEnum.SW: return new Vector3(-step, -step / 2f);
                default: return new Vector3(-step, step / 2f);     // NW
            }
        }

        // How far apart two hexes in one column are: the nearest neighbour's distance (a diagonal one is ~1.118 of it)
        private static float StepOf(Hex hex)
        {
            float nearest = float.MaxValue;
            foreach (Hex neighbor in hex.GetNeighbors())
                nearest = Mathf.Min(nearest, Vector2.Distance(hex.transform.position, neighbor.transform.position));

            return nearest == float.MaxValue ? 1f : nearest;
        }

        private static Hex HexAt(List<Hex> board, Vector3 spot, float step)
        {
            foreach (Hex hex in board)
            {
                if (Vector2.Distance(hex.transform.position, spot) < SameSpot * step) return hex;
            }

            return null;
        }
    }
}
