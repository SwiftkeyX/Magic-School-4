using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using MagicSchool.Combat.Align;
using MagicSchool.Combat.Heroes;

namespace MagicSchool.Editor
{
    // Magic School 4 > Align Editor
    // A editor window for editing each hero's align mark.
    // Solved problem that we need to manually typing directions and distances in the Inspector.
    //
    // NOTE: The editor also work during Play mode
    internal class AlignEditorWindow : EditorWindow
    {
        private const string HeroFolder = "Assets/Data/Heroes/Stats/Player";

        private const int Reach = 6;                // how many hexes out from the hero the window draws
        private const float ColumnStep = 46f;       // pixels between two columns
        private const float RowStep = 46f;          // pixels between two hexes of one column

        // the board's own colours (See BoardColours), so the window looks like what the game paints
        private static readonly Color FreeColor = new Color(0.353f, 0.608f, 1f);
        private static readonly Color FirstBonusColor = new Color(1f, 0.62f, 0.2f);
        private static readonly Color SecondBonusColor = new Color(0.4f, 0.9f, 0.5f);
        private static readonly Color HeroColor = new Color(0.85f, 0.85f, 0.85f);

        // one name for each HexDirectionEnum value, in its order
        private static readonly string[] DirectionNames = { "N", "NE", "SE", "S", "SW", "NW" };

        // where one step in each direction lands, as (steps NE, steps N), in the same order
        private static readonly Vector2Int[] DirectionSteps =
        {
            new Vector2Int(0, 1), new Vector2Int(1, 0), new Vector2Int(1, -1),
            new Vector2Int(0, -1), new Vector2Int(-1, 0), new Vector2Int(-1, 1),
        };

        private List<HeroDataSO> _heroes = new List<HeroDataSO>();
        private HeroDataSO _hero;
        private SerializedObject _serializedHero;
        private int _selectedMark;
        private string _heroSearch = string.Empty;
        private Vector2 _heroScroll;
        private Vector2 _markScroll;
        private Vector2 _hexScroll;

        [MenuItem("Magic School 4/Align Editor")]
        private static void Open() => GetWindow<AlignEditorWindow>("Align Editor");

        // ======================================== life cycle ========================================
        private void OnEnable()
        {
            FindHeroes();
            Undo.undoRedoPerformed += Repaint;

            if (Selection.activeObject is HeroDataSO selected) Edit(selected);
            else if (_hero == null && _heroes.Count > 0) Edit(_heroes[0]);
        }

        private void OnDisable() => Undo.undoRedoPerformed -= Repaint;

        private void OnProjectChange()
        {
            FindHeroes();
            Repaint();
        }

        // picking a hero in the Project window picks it here too
        private void OnSelectionChange()
        {
            if (!(Selection.activeObject is HeroDataSO selected)) return;

            Edit(selected);
            Repaint();
        }

        private void FindHeroes()
        {
            _heroes = AssetDatabase.FindAssets("t:HeroDataSO", new[] { HeroFolder })
                .Select(guid => AssetDatabase.LoadAssetAtPath<HeroDataSO>(AssetDatabase.GUIDToAssetPath(guid)))
                .Where(hero => hero != null)
                .OrderBy(hero => hero.name)
                .ToList();
        }

        private void Edit(HeroDataSO hero)
        {
            if (hero == _hero && _serializedHero != null) return;

            _hero = hero;
            _serializedHero = hero != null ? new SerializedObject(hero) : null;
            _selectedMark = 0;
        }

        // ======================================== draw ========================================
        private void OnGUI()
        {
            // the asset was deleted while the window was open
            if (_hero == null) _serializedHero = null;

            EditorGUILayout.BeginHorizontal();
            DrawHeroList();

            if (_serializedHero == null)
            {
                EditorGUILayout.HelpBox("Pick a hero on the left, or select a HeroDataSO in the Project window.", MessageType.Info);
                EditorGUILayout.EndHorizontal();
                return;
            }

            _serializedHero.Update();
            SerializedProperty marks = _serializedHero.FindProperty("_alignMarks");
            _selectedMark = Mathf.Clamp(_selectedMark, 0, Mathf.Max(0, marks.arraySize - 1));

            DrawHexes(marks);
            DrawMarks(marks);
            EditorGUILayout.EndHorizontal();

            _serializedHero.ApplyModifiedProperties();
        }

        // ---------------------------------------- left: the heroes ----------------------------------------
        private void DrawHeroList()
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(170f));

            // type part of a name to narrow the list, in any letter case
            _heroSearch = EditorGUILayout.TextField(_heroSearch, EditorStyles.toolbarSearchField);

            _heroScroll = EditorGUILayout.BeginScrollView(_heroScroll);

            bool isAnyShown = false;
            foreach (HeroDataSO hero in _heroes)
            {
                if (!IsSearched(hero)) continue;

                isAnyShown = true;
                bool isEdited = hero == _hero;
                if (GUILayout.Toggle(isEdited, hero.name, EditorStyles.miniButton) && !isEdited) Edit(hero);
            }

            if (!isAnyShown) EditorGUILayout.LabelField("No hero by that name.", EditorStyles.miniLabel);

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        private bool IsSearched(HeroDataSO hero)
        {
            if (string.IsNullOrWhiteSpace(_heroSearch)) return true;

            return hero.name.IndexOf(_heroSearch.Trim(), System.StringComparison.OrdinalIgnoreCase) >= 0;
        }

        // ---------------------------------------- middle: the hexes ----------------------------------------
        private void DrawHexes(SerializedProperty marks)
        {
            EditorGUILayout.BeginVertical();
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(_hero.name, EditorStyles.boldLabel);

            // Play mode only: get this hero onto the bench without rolling the shop for it
            using (new EditorGUI.DisabledScope(!AddHeroMenu.CanAdd))
            {
                if (GUILayout.Button("Add to bench", EditorStyles.miniButton, GUILayout.Width(90f))) AddHeroMenu.Add(_hero);
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.LabelField("Click a hex to give it to the selected bonus. Click it again to take it away.", EditorStyles.wordWrappedMiniLabel);
            EditorGUILayout.LabelField("Right-click a marked hex to make it a wearing hex: an item lying there is worn by the hero. Right-click again to undo.", EditorStyles.wordWrappedMiniLabel);
            EditorGUILayout.LabelField("The enemy is to the right.", EditorStyles.wordWrappedMiniLabel);

            // the grid is about 600 pixels square at reach 6: scroll when the window is smaller than that
            _hexScroll = EditorGUILayout.BeginScrollView(_hexScroll);

            float size = (Reach * 2 + 1) * RowStep + 20f;
            Rect area = GUILayoutUtility.GetRect(size, size, GUILayout.ExpandWidth(true));
            Vector2 centre = area.center;

            // (a, b): a steps NE and b steps N from the hero. Every hex within reach is one such pair.
            for (int a = -Reach; a <= Reach; a++)
            {
                for (int b = -Reach; b <= Reach; b++)
                {
                    int distance = (Mathf.Abs(a) + Mathf.Abs(b) + Mathf.Abs(a + b)) / 2;
                    if (distance > Reach) continue;

                    Vector2 position = centre + new Vector2(a * ColumnStep, -(b + a * 0.5f) * RowStep);
                    var offset = new Vector2Int(a, b);
                    bool isHero = distance == 0;

                    int mark = isHero ? -1 : MarkHolding(marks, offset);

                    Color fill = isHero ? HeroColor
                        : mark < 0 ? FreeColor
                        : ColorOf(mark);
                    DrawHex(position, fill, isOutlined: mark >= 0 && mark == _selectedMark);

                    bool isWearing = mark >= 0 && IsWearing(marks, mark, offset);

                    string label = isHero ? "hero"
                        : isWearing ? $"{mark + 1} ITEM\n{NameOf(offset)}"
                        : mark >= 0 ? $"{mark + 1}\n{NameOf(offset)}"
                        : NameOf(offset);
                    DrawLabel(position, label);

                    // right-click a marked hex: an item lying there is worn, or no longer is
                    if (mark >= 0 && IsClicked(position, button: 1))
                    {
                        SetWearing(marks, mark, offset, !isWearing);
                        Event.current.Use();
                        Repaint();
                    }

                    if (!isHero && IsClicked(position, button: 0))
                    {
                        Toggle(marks, offset);
                        Event.current.Use();
                        Repaint();
                    }
                }
            }

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        // How to walk from the hero to the hex at `offset`: some steps one way, then some steps a second way.
        // A hex on a straight line needs the first leg only, and its second leg comes back with 0 steps.
        // The directions are HexDirectionEnum values: N 0, NE 1, SE 2, S 3, SW 4, NW 5.
        // e.g.     (2, 0)  => NE 2
        // e.g.     (2, -1) => NE 1, then SE 1      (the hex straight ahead, 2 columns over)
        private static void GetLegs(Vector2Int offset, out int direction, out int steps, out int thenDirection, out int thenSteps)
        {
            int a = offset.x, b = offset.y;

            // the six wedges between two neighbouring directions, going round from N-NE
            if (a >= 0 && b >= 0) { direction = 1; steps = a; thenDirection = 0; thenSteps = b; }                   // NE + N
            else if (a >= 0 && a + b >= 0) { direction = 1; steps = a + b; thenDirection = 2; thenSteps = -b; }     // NE + SE
            else if (a >= 0) { direction = 2; steps = a; thenDirection = 3; thenSteps = -(a + b); }                 // SE + S
            else if (b <= 0) { direction = 4; steps = -a; thenDirection = 3; thenSteps = -b; }                      // SW + S
            else if (a + b <= 0) { direction = 4; steps = -(a + b); thenDirection = 5; thenSteps = b; }             // SW + NW
            else { direction = 5; steps = -a; thenDirection = 0; thenSteps = a + b; }                               // NW + N

            // the first leg is the one that has steps
            if (steps > 0) return;

            direction = thenDirection;
            steps = thenSteps;
            thenSteps = 0;
        }

        // e.g.     "NE 2", or "NE1+SE1" for a hex that needs a turn
        private static string NameOf(Vector2Int offset)
        {
            GetLegs(offset, out int direction, out int steps, out int thenDirection, out int thenSteps);

            return thenSteps == 0
                ? $"{DirectionNames[direction]} {steps}"
                : $"{DirectionNames[direction]}{steps}+{DirectionNames[thenDirection]}{thenSteps}";
        }

        // a flat-top hex, squashed the way the board's hexes are
        private static void DrawHex(Vector2 centre, Color fill, bool isOutlined)
        {
            if (Event.current.type != EventType.Repaint) return;

            float halfWidth = ColumnStep / 1.5f * 0.94f;
            float halfHeight = RowStep / 2f * 0.94f;
            var corners = new Vector3[]
            {
                centre + new Vector2(halfWidth, 0f),
                centre + new Vector2(halfWidth / 2f, -halfHeight),
                centre + new Vector2(-halfWidth / 2f, -halfHeight),
                centre + new Vector2(-halfWidth, 0f),
                centre + new Vector2(-halfWidth / 2f, halfHeight),
                centre + new Vector2(halfWidth / 2f, halfHeight),
            };

            Handles.color = fill;
            Handles.DrawAAConvexPolygon(corners);

            // the selected bonus' hexes get a white rim
            if (!isOutlined) return;

            Handles.color = Color.white;
            Handles.DrawAAPolyLine(3f, corners.Append(corners[0]).ToArray());
        }

        private static void DrawLabel(Vector2 centre, string text)
        {
            if (string.IsNullOrEmpty(text)) return;

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

        private static Color ColorOf(int mark) => mark == 1 ? SecondBonusColor : FirstBonusColor;

        // ---------------------------------------- right: the bonuses ----------------------------------------
        private void DrawMarks(SerializedProperty marks)
        {
            _markScroll = EditorGUILayout.BeginScrollView(_markScroll, GUILayout.Width(330f));

            for (int i = 0; i < marks.arraySize; i++)
            {
                SerializedProperty mark = marks.GetArrayElementAtIndex(i);

                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.BeginHorizontal();

                // the colour the game paints this bonus' hexes
                Rect swatch = GUILayoutUtility.GetRect(14f, 14f, GUILayout.Width(14f));
                EditorGUI.DrawRect(swatch, ColorOf(i));

                int hexCount = mark.FindPropertyRelative("hexes").arraySize;
                string title = $"Bonus {i + 1} - {hexCount} hex" + (hexCount == 1 ? "" : "es");
                if (GUILayout.Toggle(_selectedMark == i, title, EditorStyles.radioButton)) _selectedMark = i;

                bool isRemoved = GUILayout.Button("Remove", EditorStyles.miniButton, GUILayout.Width(60f));
                EditorGUILayout.EndHorizontal();

                // the [Header]s on AlignMark's fields title the two halves
                EditorGUILayout.PropertyField(mark.FindPropertyRelative("modifier"));
                EditorGUILayout.PropertyField(mark.FindPropertyRelative("amount"));
                EditorGUILayout.PropertyField(mark.FindPropertyRelative("percentOf"));
                EditorGUILayout.PropertyField(mark.FindPropertyRelative("selfModifier"));
                EditorGUILayout.PropertyField(mark.FindPropertyRelative("selfAmount"));
                EditorGUILayout.PropertyField(mark.FindPropertyRelative("selfPercentOf"));
                EditorGUILayout.PropertyField(mark.FindPropertyRelative("selfWhenEmpty"));
                EditorGUILayout.EndVertical();

                if (!isRemoved) continue;

                marks.DeleteArrayElementAtIndex(i);
                break;
            }

            if (GUILayout.Button("Add bonus")) AddMark(marks);

            if (marks.arraySize > 2)
            {
                EditorGUILayout.HelpBox("The board has two colours: a third bonus is painted like the first.", MessageType.Info);
            }

            // what the inspector panel in the game will say, once these edits are applied
            _serializedHero.ApplyModifiedProperties();
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("In the game's inspector", EditorStyles.boldLabel);
            EditorGUILayout.LabelField(AlignText.Describe(_hero.AlignMarks), EditorStyles.wordWrappedLabel);

            EditorGUILayout.Space();
            if (GUILayout.Button("Save")) AssetDatabase.SaveAssetIfDirty(_hero);

            EditorGUILayout.EndScrollView();
        }

        // ======================================== edit ========================================
        // Which bonus holds this hex? -1 when none does.
        private static int MarkHolding(SerializedProperty marks, Vector2Int offset)
        {
            for (int i = 0; i < marks.arraySize; i++)
            {
                if (IndexOfHex(marks.GetArrayElementAtIndex(i).FindPropertyRelative("hexes"), offset) >= 0) return i;
            }

            return -1;
        }

        private static int IndexOfHex(SerializedProperty hexes, Vector2Int offset)
        {
            for (int i = 0; i < hexes.arraySize; i++)
            {
                if (OffsetOf(hexes.GetArrayElementAtIndex(i)) == offset) return i;
            }

            return -1;
        }

        // Where a saved AlignHex lands, as (steps NE, steps N). Two hexes saved with different legs can be the same hex.
        private static Vector2Int OffsetOf(SerializedProperty hex)
        {
            // a hex saved with no distance is the one next to the hero (See AlignHex.Distance)
            int steps = Mathf.Max(1, hex.FindPropertyRelative("distance").intValue);
            int thenSteps = Mathf.Max(0, hex.FindPropertyRelative("thenDistance").intValue);

            return DirectionSteps[hex.FindPropertyRelative("direction").intValue] * steps
                 + DirectionSteps[hex.FindPropertyRelative("thenDirection").intValue] * thenSteps;
        }

        // Give the hex to the selected bonus, or take it away when that bonus already has it.
        // A hex belongs to one bonus only, so it is taken from whichever other bonus held it.
        private void Toggle(SerializedProperty marks, Vector2Int offset)
        {
            if (marks.arraySize == 0) AddMark(marks);

            bool wasInSelected = false;
            for (int i = 0; i < marks.arraySize; i++)
            {
                SerializedProperty hexes = marks.GetArrayElementAtIndex(i).FindPropertyRelative("hexes");
                int index = IndexOfHex(hexes, offset);
                if (index < 0) continue;

                hexes.DeleteArrayElementAtIndex(index);
                if (i == _selectedMark) wasInSelected = true;
            }

            if (wasInSelected) return;

            SerializedProperty selected = marks.GetArrayElementAtIndex(_selectedMark).FindPropertyRelative("hexes");
            selected.arraySize++;

            GetLegs(offset, out int direction, out int steps, out int thenDirection, out int thenSteps);

            SerializedProperty added = selected.GetArrayElementAtIndex(selected.arraySize - 1);
            added.FindPropertyRelative("direction").intValue = direction;
            added.FindPropertyRelative("distance").intValue = steps;
            added.FindPropertyRelative("thenDirection").intValue = thenDirection;
            added.FindPropertyRelative("thenDistance").intValue = thenSteps;
            added.FindPropertyRelative("isWearing").boolValue = false;
        }

        // Is this hex of the bonus a wearing hex?
        private static bool IsWearing(SerializedProperty marks, int mark, Vector2Int offset)
        {
            SerializedProperty hexes = marks.GetArrayElementAtIndex(mark).FindPropertyRelative("hexes");
            int index = IndexOfHex(hexes, offset);

            return index >= 0 && hexes.GetArrayElementAtIndex(index).FindPropertyRelative("isWearing").boolValue;
        }

        private static void SetWearing(SerializedProperty marks, int mark, Vector2Int offset, bool isWearing)
        {
            SerializedProperty hexes = marks.GetArrayElementAtIndex(mark).FindPropertyRelative("hexes");
            int index = IndexOfHex(hexes, offset);
            if (index < 0) return;

            hexes.GetArrayElementAtIndex(index).FindPropertyRelative("isWearing").boolValue = isWearing;
        }

        // A new, empty bonus, and select it. Unity fills a new list entry with a copy of the last one, so wipe it.
        private void AddMark(SerializedProperty marks)
        {
            const int NoStat = -1;      // StatEnum.None

            marks.arraySize++;
            SerializedProperty mark = marks.GetArrayElementAtIndex(marks.arraySize - 1);

            mark.FindPropertyRelative("hexes").ClearArray();
            mark.FindPropertyRelative("amount").floatValue = 0f;
            mark.FindPropertyRelative("percentOf").intValue = NoStat;
            mark.FindPropertyRelative("selfAmount").floatValue = 0f;
            mark.FindPropertyRelative("selfPercentOf").intValue = NoStat;
            mark.FindPropertyRelative("selfWhenEmpty").boolValue = false;

            _selectedMark = marks.arraySize - 1;
        }
    }
}
