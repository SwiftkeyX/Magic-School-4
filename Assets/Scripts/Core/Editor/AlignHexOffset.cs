using UnityEditor;
using UnityEngine;

namespace MagicSchool.Editor
{
    // The Align Editor's hex maths. A hex around the hero is an offset (a, b): a steps NE and b steps N from it.
    // A saved AlignHex is a walk instead - some steps one way, then some steps a second way - so this turns one into the other.
    // The directions are HexDirectionEnum values: N 0, NE 1, SE 2, S 3, SW 4, NW 5.
    internal static class AlignHexOffset
    {
        // one name for each HexDirectionEnum value, in its order
        private static readonly string[] DirectionNames = { "N", "NE", "SE", "S", "SW", "NW" };

        // where one step in each direction lands, as (steps NE, steps N), in the same order
        private static readonly Vector2Int[] DirectionSteps =
        {
            new Vector2Int(0, 1), new Vector2Int(1, 0), new Vector2Int(1, -1),
            new Vector2Int(0, -1), new Vector2Int(-1, 0), new Vector2Int(-1, 1),
        };

        // How to walk from the hero to one hex. A hex on a straight line needs the first leg only: ThenSteps is 0.
        // e.g.     (2, 0)  => NE 2
        // e.g.     (2, -1) => NE 1, then SE 1      (the hex straight ahead, 2 columns over)
        public readonly struct Legs
        {
            public readonly int Direction, Steps, ThenDirection, ThenSteps;

            public Legs(int direction, int steps, int thenDirection, int thenSteps)
            {
                Direction = direction;
                Steps = steps;
                ThenDirection = thenDirection;
                ThenSteps = thenSteps;
            }
        }

        // how many hexes away from the hero
        public static int DistanceOf(Vector2Int offset)
            => (Mathf.Abs(offset.x) + Mathf.Abs(offset.y) + Mathf.Abs(offset.x + offset.y)) / 2;

        public static Legs LegsOf(Vector2Int offset)
        {
            int a = offset.x, b = offset.y;

            // the six wedges between two neighbouring directions, going round from N-NE
            Legs legs =
                a >= 0 && b >= 0      ? new Legs(1, a, 0, b)                // NE + N
              : a >= 0 && a + b >= 0  ? new Legs(1, a + b, 2, -b)           // NE + SE
              : a >= 0                ? new Legs(2, a, 3, -(a + b))         // SE + S
              : b <= 0                ? new Legs(4, -a, 3, -b)              // SW + S
              : a + b <= 0            ? new Legs(4, -(a + b), 5, b)         // SW + NW
              :                         new Legs(5, -a, 0, a + b);          // NW + N

            // the first leg is the one that has steps
            return legs.Steps > 0 ? legs : new Legs(legs.ThenDirection, legs.ThenSteps, legs.ThenDirection, 0);
        }

        // e.g.     "NE 2", or "NE1+SE1" for a hex that needs a turn
        public static string NameOf(Vector2Int offset)
        {
            Legs legs = LegsOf(offset);

            return legs.ThenSteps == 0
                ? $"{DirectionNames[legs.Direction]} {legs.Steps}"
                : $"{DirectionNames[legs.Direction]}{legs.Steps}+{DirectionNames[legs.ThenDirection]}{legs.ThenSteps}";
        }

        // 60 degrees clockwise round the hero: N (0, 1) -> NE (1, 0), NE (1, 0) -> SE (1, -1), so (a, b) -> (a + b, -a).
        public static Vector2Int RotatedClockwise(Vector2Int offset) => new Vector2Int(offset.x + offset.y, -offset.x);

        // ======================================== saved AlignHex ========================================
        // Where a saved AlignHex lands. Two hexes saved with different legs can be the same hex.
        public static Vector2Int Read(SerializedProperty hex)
        {
            // a hex saved with no distance is the one next to the hero (See AlignHex.Distance)
            int steps = Mathf.Max(1, hex.FindPropertyRelative("distance").intValue);
            int thenSteps = Mathf.Max(0, hex.FindPropertyRelative("thenDistance").intValue);

            return DirectionSteps[hex.FindPropertyRelative("direction").intValue] * steps
                 + DirectionSteps[hex.FindPropertyRelative("thenDirection").intValue] * thenSteps;
        }

        // Save the walk from the hero to the hex at `offset` into an AlignHex
        public static void Write(SerializedProperty hex, Vector2Int offset)
        {
            Legs legs = LegsOf(offset);

            hex.FindPropertyRelative("direction").intValue = legs.Direction;
            hex.FindPropertyRelative("distance").intValue = legs.Steps;
            hex.FindPropertyRelative("thenDirection").intValue = legs.ThenDirection;
            hex.FindPropertyRelative("thenDistance").intValue = legs.ThenSteps;
        }
    }
}
