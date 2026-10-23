using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace MagicSchool.Editor
{
    // ---------------------------------------- middle: the hexes ----------------------------------------
    // Every hex within reach of the hero. Left-click gives a hex to the selected mark, right-click makes a marked hex a wearing hex.
    internal partial class AlignEditorWindow
    {
        private const int Reach = 6;                // how many hexes out from the hero the window draws
        private const float ColumnStep = 46f;       // pixels between two columns
        private const float RowStep = 46f;          // pixels between two hexes of one column

        // the board's own colours (See BoardColours), so the window looks like what the game paints
        private static readonly Color FreeColor = new Color(0.353f, 0.608f, 1f);
        private static readonly Color FirstBonusColor = new Color(1f, 0.62f, 0.2f);
        private static readonly Color SecondBonusColor = new Color(0.4f, 0.9f, 0.5f);
        private static readonly Color SpecialColor = Color.white;
        private static readonly Color HeroColor = new Color(0.85f, 0.85f, 0.85f);
        private static readonly Color UnusedColor = new Color(0.5f, 0.5f, 0.5f);

        private Vector2 _hexScroll;

        // orange, green, a special mark white, and a third ordinary mark grey: it does nothing (See AligningHex.ColourOf)
        private static Color ColorOf(SerializedProperty marks, int mark)
        {
            if (IsSpecial(marks, mark)) return SpecialColor;
            if (mark == OrangeMark) return FirstBonusColor;
            if (mark == GreenMark) return SecondBonusColor;
            return UnusedColor;
        }

        private void DrawHexes(SerializedProperty marks)
        {
            WindowStyle.Hint("Click a hex to give it to the selected mark. Click it again to take it away. A hex can be in more than one mark: it is drawn in rings, one colour each.");
            WindowStyle.Hint("Right-click a marked hex to make it a wearing hex: an item lying there, or the item of an ally standing there, is worn by the hero. Right-click again to undo.");
            WindowStyle.Hint("The enemy is to the right.");

            // the grid is about 600 pixels square at reach 6: scroll when the window is smaller than that
            _hexScroll = EditorGUILayout.BeginScrollView(_hexScroll);

            float size = (Reach * 2 + 1) * RowStep + 20f;
            Vector2 centre = GUILayoutUtility.GetRect(size, size, GUILayout.ExpandWidth(true)).center;

            // (a, b): a steps NE and b steps N from the hero. Every hex within reach is one such pair.
            for (int a = -Reach; a <= Reach; a++)
            {
                for (int b = -Reach; b <= Reach; b++)
                {
                    var offset = new Vector2Int(a, b);
                    if (AlignHexOffset.DistanceOf(offset) > Reach) continue;

                    Vector2 position = centre + new Vector2(a * ColumnStep, -(b + a * 0.5f) * RowStep);
                    DrawHexCell(marks, offset, position);
                }
            }

            EditorGUILayout.EndScrollView();
        }

        // one hex: its colour, its label, and what a click on it does
        private void DrawHexCell(SerializedProperty marks, Vector2Int offset, Vector2 position)
        {
            if (offset == Vector2Int.zero)
            {
                DrawHex(position, new[] { HeroColor }, isOutlined: false);
                DrawLabel(position, "hero");
                return;
            }

            List<int> holding = MarksHolding(marks, offset);
            bool isMarked = holding.Count > 0;
            bool isWearing = isMarked && IsWearing(marks, offset);
            string name = AlignHexOffset.NameOf(offset);

            Color[] colours = isMarked ? holding.Select(mark => ColorOf(marks, mark)).ToArray() : new[] { FreeColor };
            DrawHex(position, colours, isOutlined: holding.Contains(_selectedMark));

            string label = string.Join(" ", holding.Select(mark => ShortNameOf(marks, mark)));
            DrawLabel(position, isWearing ? $"{label} ITEM\n{name}"
                              : isMarked ? $"{label}\n{name}"
                              : name);

            // right-click a marked hex: an item lying there is worn, or no longer is
            if (isMarked && IsClicked(position, button: 1)) Use(() => SetWearing(marks, offset, !isWearing));

            if (IsClicked(position, button: 0)) Use(() => Toggle(marks, offset));
        }

        // act on the click, and keep anything else from acting on it too
        private void Use(System.Action edit)
        {
            edit();
            Event.current.Use();
            Repaint();
        }

        // a flat-top hex, squashed the way the board's hexes are. Several colours are drawn as rings, the first outermost.
        private static void DrawHex(Vector2 centre, Color[] fills, bool isOutlined)
        {
            if (Event.current.type != EventType.Repaint) return;

            Vector3[] corners = CornersOf(centre, 1f);

            for (int i = 0; i < fills.Length; i++)
            {
                Handles.color = fills[i];
                Handles.DrawAAConvexPolygon(CornersOf(centre, 1f - i * 0.3f));
            }

            // the selected bonus' hexes get a white rim
            if (!isOutlined) return;

            Handles.color = Color.white;
            Handles.DrawAAPolyLine(3f, corners.Append(corners[0]).ToArray());
        }

        private static Vector3[] CornersOf(Vector2 centre, float scale)
        {
            float halfWidth = ColumnStep / 1.5f * 0.94f * scale;
            float halfHeight = RowStep / 2f * 0.94f * scale;
            return new Vector3[]
            {
                centre + new Vector2(halfWidth, 0f),
                centre + new Vector2(halfWidth / 2f, -halfHeight),
                centre + new Vector2(-halfWidth / 2f, -halfHeight),
                centre + new Vector2(-halfWidth, 0f),
                centre + new Vector2(-halfWidth / 2f, halfHeight),
                centre + new Vector2(halfWidth / 2f, halfHeight),
            };
        }

        private static void DrawLabel(Vector2 centre, string text)
        {
            var style = new GUIStyle(EditorStyles.miniLabel) { alignment = TextAnchor.MiddleCenter };
            style.normal.textColor = Color.black;
            GUI.Label(new Rect(centre.x - 30f, centre.y - 16f, 60f, 32f), text, style);
        }

        private static bool IsClicked(Vector2 centre, int button)
        {
            Event e = Event.current;
            if (e.type != EventType.MouseDown || e.button != button) return false;

            return Vector2.Distance(e.mousePosition, centre) < RowStep * 0.45f;
        }
    }
}
