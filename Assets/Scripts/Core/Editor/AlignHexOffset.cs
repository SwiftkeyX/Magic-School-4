using UnityEditor;
using UnityEngine;
using MagicSchool.Combat.Align;

namespace MagicSchool.Editor
{
    // The Align Editor's hex maths. A hex around the hero is an offset (a, b): a steps NE and b steps N from it.
    // A saved AlignHex is a walk instead - some steps one way, then some steps a second way - so this turns one into the other.
    // The directions are HexDirectionEnum values: N 0, NE 1, SE 2, S 3, SW 4, NW 5.
    // An offset is (a, b): a steps NE, b steps N. Turning 60 degrees clockwise is (a, b) -> (a + b, -a). Two hexes
    // saved with different legs can be the same hex; one saved with no distance is next to the hero.
    internal static class AlignHexOffset
    {
        private static readonly string[] DirectionNames = { "N", "NE", "SE", "S", "SW", "NW" };

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

        public static int DistanceOf(Vector2Int offset)
            => (Mathf.Abs(offset.x) + Mathf.Abs(offset.y) + Mathf.Abs(offset.x + offset.y)) / 2;

        public static Legs LegsOf(Vector2Int offset)
        {
            int a = offset.x, b = offset.y;

            Legs legs =
                a >= 0 && b >= 0      ? new Legs(1, a, 0, b)
              : a >= 0 && a + b >= 0  ? new Legs(1, a + b, 2, -b)
              : a >= 0                ? new Legs(2, a, 3, -(a + b))
              : b <= 0                ? new Legs(4, -a, 3, -b)
              : a + b <= 0            ? new Legs(4, -(a + b), 5, b)
              :                         new Legs(5, -a, 0, a + b);

            return legs.Steps > 0 ? legs : new Legs(legs.ThenDirection, legs.ThenSteps, legs.ThenDirection, 0);
        }

        public static string NameOf(Vector2Int offset)
        {
            Legs legs = LegsOf(offset);

            return legs.ThenSteps == 0
                ? $"{DirectionNames[legs.Direction]} {legs.Steps}"
                : $"{DirectionNames[legs.Direction]}{legs.Steps}+{DirectionNames[legs.ThenDirection]}{legs.ThenSteps}";
        }

        public static Vector2Int RotatedClockwise(Vector2Int offset) => new Vector2Int(offset.x + offset.y, -offset.x);

        public static Vector2Int Read(SerializedProperty hex)
        {
            int steps = Mathf.Max(1, hex.FindPropertyRelative(nameof(AlignHex.distance)).intValue);
            int thenSteps = Mathf.Max(0, hex.FindPropertyRelative(nameof(AlignHex.thenDistance)).intValue);

            return DirectionSteps[hex.FindPropertyRelative(nameof(AlignHex.direction)).intValue] * steps
                 + DirectionSteps[hex.FindPropertyRelative(nameof(AlignHex.thenDirection)).intValue] * thenSteps;
        }

        public static void Write(SerializedProperty hex, Vector2Int offset)
        {
            Legs legs = LegsOf(offset);

            hex.FindPropertyRelative(nameof(AlignHex.direction)).intValue = legs.Direction;
            hex.FindPropertyRelative(nameof(AlignHex.distance)).intValue = legs.Steps;
            hex.FindPropertyRelative(nameof(AlignHex.thenDirection)).intValue = legs.ThenDirection;
            hex.FindPropertyRelative(nameof(AlignHex.thenDistance)).intValue = legs.ThenSteps;
        }
    }
}
