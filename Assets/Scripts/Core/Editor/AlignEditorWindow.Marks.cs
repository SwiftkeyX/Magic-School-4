using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using MagicSchool.Combat.Align;

namespace MagicSchool.Editor
{
    // ---------------------------------------- right: the marks ----------------------------------------
    // Every pattern has the same boxes, fixed (See AlignMark):
    //      Orange      => mark 0, pays the hero itself
    //      Green       => mark 1, pays the ally standing there
    //      White       => one per special mark the hero's skill reads, after them; it pays nobody
    // and the edits a click on a hex makes to them.
    internal partial class AlignEditorWindow
    {
        private const int OrangeMark = 0;
        private const int GreenMark = 1;
        private const int FirstSpecialMark = 2;

        // the fields each box shows, in order. AlignMark's [Header]s title them.
        private static readonly string[] OrangeFields = { "selfModifier", "selfAmount", "selfPercentOf", "selfWhenEmpty", "healFromAllyDamage" };
        private static readonly string[] GreenFields = { "modifier", "amount", "percentOf", "perOwnerAP" };

        private Vector2 _markScroll;

        private void DrawMarks(SerializedProperty marks)
        {
            _markScroll = EditorGUILayout.BeginScrollView(_markScroll, GUILayout.Width(330f));

            for (int i = 0; i < marks.arraySize; i++)
            {
                if (!DrawMark(marks, i)) continue;

                marks.DeleteArrayElementAtIndex(i);
                _selectedMark = OrangeMark;
                break;
            }

            // what the inspector panel in the game will say, once these edits are applied
            _serializedHero.ApplyModifiedProperties();
            WindowStyle.Section($"In the game's inspector (pattern {_selectedPattern + 1})");
            EditorGUILayout.LabelField(AlignText.Describe(_hero.AlignMarksOf(_selectedPattern)), EditorStyles.wordWrappedLabel);

            EditorGUILayout.Space(WindowStyle.Gap);
            if (WindowStyle.Action(new GUIContent("Save", "Write this hero's marks to its asset."))) AssetDatabase.SaveAssetIfDirty(_hero);

            EditorGUILayout.EndScrollView();
        }

        // One mark's box. True when its Remove button was clicked - only a special mark the skill no longer reads has one.
        private bool DrawMark(SerializedProperty marks, int index)
        {
            SerializedProperty mark = marks.GetArrayElementAtIndex(index);
            bool isSpecial = IsSpecial(marks, index);
            bool isStale = isSpecial && !_specialMarks.Contains(SpecialOf(marks, index));

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.BeginHorizontal();

            // the colour the game paints this mark's hexes
            Rect swatch = GUILayoutUtility.GetRect(14f, 14f, GUILayout.Width(14f));
            EditorGUI.DrawRect(swatch, ColorOf(marks, index));

            int hexCount = HexesOf(marks, index).arraySize;
            string title = $"{TitleOf(marks, index)} - {hexCount} hex" + (hexCount == 1 ? "" : "es");
            if (GUILayout.Toggle(_selectedMark == index, title, EditorStyles.radioButton)) _selectedMark = index;

            bool isRemoved = isStale && WindowStyle.RowButton("Remove", 60f);
            EditorGUILayout.EndHorizontal();

            switch (index)
            {
                case OrangeMark:
                    WindowStyle.Hint("Pays the hero itself, for every ally standing on one of these hexes. The same in every pattern.");
                    DrawBonusFields(mark, index, OrangeFields);
                    break;

                case GreenMark:
                    WindowStyle.Hint("Pays the ally standing on one of these hexes. The same in every pattern.");
                    DrawBonusFields(mark, index, GreenFields);
                    break;

                default:
                    if (isStale) EditorGUILayout.HelpBox("The skill no longer reads this mark.", MessageType.Warning);
                    else if (isSpecial) WindowStyle.Hint("The skill reads who stands here. It pays nobody.");
                    else EditorGUILayout.HelpBox("A third ordinary mark does nothing: only orange and green pay.", MessageType.Warning);
                    break;
            }

            EditorGUILayout.EndVertical();

            return isRemoved || (!isSpecial && index >= FirstSpecialMark && GUILayout.Button("Remove this mark", EditorStyles.miniButton));
        }

        // ======================================== one bonus for every pattern ========================================
        // A pattern only moves the hexes: orange's and green's bonuses are the hero's, the same in every pattern.
        // They are still stored on each pattern's own mark, so the editor keeps the copies equal.

        // A bonus edited in the pattern on screen is written into every pattern at once.
        private void DrawBonusFields(SerializedProperty mark, int index, string[] fields)
        {
            EditorGUI.BeginChangeCheck();
            foreach (string field in fields) EditorGUILayout.PropertyField(mark.FindPropertyRelative(field));
            if (EditorGUI.EndChangeCheck()) CopyBonuses(_serializedHero.FindProperty("_alignPatterns"), _selectedPattern, index);
        }

        // Every pattern gets pattern 1's bonuses: one added empty, one edited before this rule, or one whose boxes were only just added.
        private static void MatchBonusesToFirstPattern(SerializedProperty patterns)
        {
            CopyBonuses(patterns, 0, OrangeMark);
            CopyBonuses(patterns, 0, GreenMark);
        }

        // That mark's bonus fields, from one pattern into every other. A pattern without the mark yet gets it when it is shown.
        private static void CopyBonuses(SerializedProperty patterns, int fromPattern, int mark)
        {
            SerializedProperty fromMarks = MarksOf(patterns, fromPattern);
            if (mark >= fromMarks.arraySize || IsSpecial(fromMarks, mark)) return;

            SerializedProperty from = fromMarks.GetArrayElementAtIndex(mark);
            string[] fields = mark == OrangeMark ? OrangeFields : GreenFields;

            for (int p = 0; p < patterns.arraySize; p++)
            {
                SerializedProperty marks = MarksOf(patterns, p);
                if (p == fromPattern || mark >= marks.arraySize || IsSpecial(marks, mark)) continue;

                SerializedProperty to = marks.GetArrayElementAtIndex(mark);
                foreach (string field in fields) CopyValue(from.FindPropertyRelative(field), to.FindPropertyRelative(field));
            }
        }

        // only what a bonus field can be: an enum, a number or a tick
        private static void CopyValue(SerializedProperty from, SerializedProperty to)
        {
            switch (from.propertyType)
            {
                case SerializedPropertyType.Enum:
                case SerializedPropertyType.Integer: to.intValue = from.intValue; break;
                case SerializedPropertyType.Float: to.floatValue = from.floatValue; break;
                case SerializedPropertyType.Boolean: to.boolValue = from.boolValue; break;
                default: Debug.LogError($"Align Editor: cannot copy {from.propertyPath} ({from.propertyType})"); break;
            }
        }

        // A new, empty mark at the end. Unity fills a new list entry with a copy of the last one, so wipe it.
        private static void AddMark(SerializedProperty marks, string special = "")
        {
            const int NoStat = -1;      // StatEnum.None

            marks.arraySize++;
            SerializedProperty mark = marks.GetArrayElementAtIndex(marks.arraySize - 1);

            mark.FindPropertyRelative("hexes").ClearArray();
            mark.FindPropertyRelative("special").stringValue = special;
            mark.FindPropertyRelative("amount").floatValue = 0f;
            mark.FindPropertyRelative("percentOf").intValue = NoStat;
            mark.FindPropertyRelative("perOwnerAP").boolValue = false;
            mark.FindPropertyRelative("selfAmount").floatValue = 0f;
            mark.FindPropertyRelative("selfPercentOf").intValue = NoStat;
            mark.FindPropertyRelative("selfWhenEmpty").boolValue = false;
            mark.FindPropertyRelative("healFromAllyDamage").floatValue = 0f;
        }

        // Every pattern has orange and green first, then every special mark the hero's skill reads (See AlignMark).
        // A pattern missing any of them gets it, so the boxes are always there without anyone adding them.
        private void AddMissingMarks(SerializedProperty marks)
        {
            // orange and green: two ordinary marks in front of everything else
            while (marks.arraySize < FirstSpecialMark || IsSpecial(marks, OrangeMark) || IsSpecial(marks, GreenMark))
            {
                int at = marks.arraySize == 0 || IsSpecial(marks, OrangeMark) ? OrangeMark : GreenMark;
                if (at >= marks.arraySize)
                {
                    AddMark(marks);
                    continue;
                }

                AddMark(marks);
                marks.MoveArrayElement(marks.arraySize - 1, at);
            }

            foreach (string special in _specialMarks)
            {
                bool isThere = Enumerable.Range(0, marks.arraySize).Any(i => SpecialOf(marks, i) == special);
                if (!isThere) AddMark(marks, special);
            }

            _selectedMark = Mathf.Clamp(_selectedMark, 0, marks.arraySize - 1);
        }

        // ======================================== what a mark is ========================================
        private static string SpecialOf(SerializedProperty marks, int mark)
            => marks.GetArrayElementAtIndex(mark).FindPropertyRelative("special").stringValue;

        private static bool IsSpecial(SerializedProperty marks, int mark) => !string.IsNullOrEmpty(SpecialOf(marks, mark));

        private static string TitleOf(SerializedProperty marks, int mark)
        {
            if (IsSpecial(marks, mark)) return $"{SpecialOf(marks, mark)} (skill mark)";
            if (mark == OrangeMark) return "Orange: self";
            if (mark == GreenMark) return "Green: ally";
            return $"Unused mark {mark + 1}";
        }

        // what a marked hex says on it for this mark: O, G, or the special mark's name
        private static string ShortNameOf(SerializedProperty marks, int mark)
        {
            if (IsSpecial(marks, mark)) return SpecialOf(marks, mark);
            if (mark == OrangeMark) return "O";
            if (mark == GreenMark) return "G";
            return "?";
        }

        // ======================================== which hex belongs to which mark ========================================
        private static SerializedProperty HexesOf(SerializedProperty marks, int mark)
            => marks.GetArrayElementAtIndex(mark).FindPropertyRelative("hexes");

        // Every mark that holds this hex: a hex can be orange, green and white at once.
        private static List<int> MarksHolding(SerializedProperty marks, Vector2Int offset)
            => Enumerable.Range(0, marks.arraySize).Where(i => IndexOfHex(HexesOf(marks, i), offset) >= 0).ToList();

        private static int IndexOfHex(SerializedProperty hexes, Vector2Int offset)
        {
            for (int i = 0; i < hexes.arraySize; i++)
            {
                if (AlignHexOffset.Read(hexes.GetArrayElementAtIndex(i)) == offset) return i;
            }

            return -1;
        }

        // Give the hex to the selected mark, or take it away when that mark already has it. The other marks keep theirs.
        // A new hex is a wearing hex when another mark already has it as one, so the hex stays one or the other.
        private void Toggle(SerializedProperty marks, Vector2Int offset)
        {
            SerializedProperty selected = HexesOf(marks, _selectedMark);
            int index = IndexOfHex(selected, offset);

            if (index >= 0)
            {
                selected.DeleteArrayElementAtIndex(index);
                return;
            }

            bool isWearing = IsWearing(marks, offset);

            selected.arraySize++;
            SerializedProperty added = selected.GetArrayElementAtIndex(selected.arraySize - 1);
            AlignHexOffset.Write(added, offset);
            added.FindPropertyRelative("isWearing").boolValue = isWearing;
        }

        // Is this hex a wearing hex? It is one for every mark holding it, or for none.
        private static bool IsWearing(SerializedProperty marks, Vector2Int offset)
            => MarksHolding(marks, offset).Any(mark => WearingOf(marks, mark, offset).boolValue);

        private static void SetWearing(SerializedProperty marks, Vector2Int offset, bool isWearing)
        {
            foreach (int mark in MarksHolding(marks, offset)) WearingOf(marks, mark, offset).boolValue = isWearing;
        }

        // The "isWearing" of this hex of the mark. Only called for a mark that holds the hex.
        private static SerializedProperty WearingOf(SerializedProperty marks, int mark, Vector2Int offset)
        {
            SerializedProperty hexes = HexesOf(marks, mark);
            return hexes.GetArrayElementAtIndex(IndexOfHex(hexes, offset)).FindPropertyRelative("isWearing");
        }
    }
}
