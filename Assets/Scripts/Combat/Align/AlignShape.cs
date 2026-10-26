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
    // Positions are walked on the grid, not across the board's hexes, so a mark past the board's edge still has a spot
    // to draw. A turn is walked first-direction-first, the way AlignHex.GetHex tries it first. One step up or down a
    // column is the nearest-neighbour distance; a diagonal is a column across and half a step up or down (atan(0.5) =
    // 26.57 degrees, See Hex.AngleOf). A hex in two marks comes twice, once per colour.
    public static class AlignShape
    {
        public struct ShownHex
        {
            public Vector3 position;
            public Hex hex;
            public bool isPastEdge;
            public int colour;
            public bool isWearing;

            public bool IsOffBoard => hex == null;
        }

        private const float SameSpot = 0.3f;

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

        private static Vector3 SpotOf(AlignHex alignHex, Hex from, float step)
        {
            Vector3 spot = from.transform.position + OffsetOf(alignHex.direction, step) * alignHex.Distance;
            if (alignHex.HasTurn) spot += OffsetOf(alignHex.thenDirection, step) * alignHex.thenDistance;

            return spot;
        }

        private static Vector3 OffsetOf(HexDirectionEnum direction, float step)
        {
            switch (direction)
            {
                case HexDirectionEnum.N: return new Vector3(0f, step);
                case HexDirectionEnum.NE: return new Vector3(step, step / 2f);
                case HexDirectionEnum.SE: return new Vector3(step, -step / 2f);
                case HexDirectionEnum.S: return new Vector3(0f, -step);
                case HexDirectionEnum.SW: return new Vector3(-step, -step / 2f);
                default: return new Vector3(-step, step / 2f);
            }
        }

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
