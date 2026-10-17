using System.Linq;
using UnityEditor;
using UnityEngine;
using MagicSchool.Combat.Align;

namespace MagicSchool.Editor
{
    // ---------------------------------------- right: the bonuses ----------------------------------------
    // One box per AlignMark of the selected pattern, and the edits a click on a hex makes to them.
    internal partial class AlignEditorWindow
    {
        // AlignMark's fields, in the order the box shows them. Its [Header]s title the ally and self halves.
        private static readonly string[] MarkFields =
        {
            "modifier", "amount", "percentOf",
            "selfModifier", "selfAmount", "selfPercentOf", "selfWhenEmpty",
            "healFromAllyDamage",
        };

        private Vector2 _markScroll;

        private void DrawMarks(SerializedProperty marks)
        {
            _markScroll = EditorGUILayout.BeginScrollView(_markScroll, GUILayout.Width(330f));

            for (int i = 0; i < marks.arraySize; i++)
            {
                if (!DrawMark(marks, i)) continue;

                marks.DeleteArrayElementAtIndex(i);
                break;
            }

            if (GUILayout.Button("Add bonus")) AddMark(marks);

            if (Enumerable.Range(0, marks.arraySize).Count(i => !IsSpecial(marks, i)) > 2)
            {
                EditorGUILayout.HelpBox("The board has two colours: a third bonus is painted like the first.", MessageType.Info);
            }

            // what the inspector panel in the game will say, once these edits are applied
            _serializedHero.ApplyModifiedProperties();
            EditorGUILayout.Space();
            EditorGUILayout.LabelField($"In the game's inspector (pattern {_selectedPattern + 1})", EditorStyles.boldLabel);
            EditorGUILayout.LabelField(AlignText.Describe(_hero.AlignMarksOf(_selectedPattern)), EditorStyles.wordWrappedLabel);

            EditorGUILayout.Space();
            if (GUILayout.Button("Save")) AssetDatabase.SaveAssetIfDirty(_hero);

            EditorGUILayout.EndScrollView();
        }

        // One bonus' box. True when its Remove button was clicked.
        private bool DrawMark(SerializedProperty marks, int index)
        {
            SerializedProperty mark = marks.GetArrayElementAtIndex(index);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.BeginHorizontal();

            // the colour the game paints this bonus' hexes
            Rect swatch = GUILayoutUtility.GetRect(14f, 14f, GUILayout.Width(14f));
            EditorGUI.DrawRect(swatch, ColorOf(marks, index));

            int hexCount = HexesOf(marks, index).arraySize;
            string hexes = $"{hexCount} hex" + (hexCount == 1 ? "" : "es");
            string special = SpecialOf(marks, index);
            string title = IsSpecial(marks, index) ? $"{special} (skill mark) - {hexes}" : $"Bonus {OrdinaryNumberOf(marks, index)} - {hexes}";
            if (GUILayout.Toggle(_selectedMark == index, title, EditorStyles.radioButton)) _selectedMark = index;

            // a special mark the skill reads cannot be removed: the editor would only put it back
            bool isRead = IsSpecial(marks, index) && _specialMarks.Contains(special);
            bool isRemoved;
            using (new EditorGUI.DisabledScope(isRead))
            {
                isRemoved = GUILayout.Button("Remove", EditorStyles.miniButton, GUILayout.Width(60f));
            }
            EditorGUILayout.EndHorizontal();

            if (isRead) EditorGUILayout.LabelField("The skill reads who stands here. A bonus is optional.", EditorStyles.wordWrappedMiniLabel);
            else if (IsSpecial(marks, index)) EditorGUILayout.HelpBox("The skill no longer reads this mark.", MessageType.Warning);

            foreach (string field in MarkFields) EditorGUILayout.PropertyField(mark.FindPropertyRelative(field));
            EditorGUILayout.EndVertical();

            return isRemoved;
        }

        // A new, empty bonus, and select it. Unity fills a new list entry with a copy of the last one, so wipe it.
        private void AddMark(SerializedProperty marks, string special = "")
        {
            const int NoStat = -1;      // StatEnum.None

            marks.arraySize++;
            SerializedProperty mark = marks.GetArrayElementAtIndex(marks.arraySize - 1);

            mark.FindPropertyRelative("hexes").ClearArray();
            mark.FindPropertyRelative("special").stringValue = special;
            mark.FindPropertyRelative("amount").floatValue = 0f;
            mark.FindPropertyRelative("percentOf").intValue = NoStat;
            mark.FindPropertyRelative("selfAmount").floatValue = 0f;
            mark.FindPropertyRelative("selfPercentOf").intValue = NoStat;
            mark.FindPropertyRelative("selfWhenEmpty").boolValue = false;
            mark.FindPropertyRelative("healFromAllyDamage").floatValue = 0f;

            _selectedMark = marks.arraySize - 1;
        }

        // Every special mark the hero's skill reads is in every pattern, without anyone adding it (See AlignMark).
        // The selected bonus stays the one it was: a special mark is added at the end.
        private void AddMissingSpecialMarks(SerializedProperty marks)
        {
            int selected = _selectedMark;
            foreach (string special in _specialMarks)
            {
                bool isThere = Enumerable.Range(0, marks.arraySize).Any(i => SpecialOf(marks, i) == special);
                if (!isThere) AddMark(marks, special);
            }

            _selectedMark = selected;
        }

        // ======================================== what a mark is ========================================
        private static string SpecialOf(SerializedProperty marks, int mark)
            => marks.GetArrayElementAtIndex(mark).FindPropertyRelative("special").stringValue;

        private static bool IsSpecial(SerializedProperty marks, int mark) => !string.IsNullOrEmpty(SpecialOf(marks, mark));

        // the ordinary marks are numbered without the special ones: "Bonus 1", "Bonus 2"
        private static int OrdinaryNumberOf(SerializedProperty marks, int mark)
            => 1 + Enumerable.Range(0, mark).Count(i => !IsSpecial(marks, i));

        // what a marked hex says on it: the bonus' number, or the special mark's name
        private static string ShortNameOf(SerializedProperty marks, int mark)
            => IsSpecial(marks, mark) ? SpecialOf(marks, mark) : OrdinaryNumberOf(marks, mark).ToString();

        // ======================================== which hex belongs to which bonus ========================================
        private static SerializedProperty HexesOf(SerializedProperty marks, int mark)
            => marks.GetArrayElementAtIndex(mark).FindPropertyRelative("hexes");

        // Which bonus holds this hex? -1 when none does.
        private static int MarkHolding(SerializedProperty marks, Vector2Int offset)
        {
            for (int i = 0; i < marks.arraySize; i++)
            {
                if (IndexOfHex(HexesOf(marks, i), offset) >= 0) return i;
            }

            return -1;
        }

        private static int IndexOfHex(SerializedProperty hexes, Vector2Int offset)
        {
            for (int i = 0; i < hexes.arraySize; i++)
            {
                if (AlignHexOffset.Read(hexes.GetArrayElementAtIndex(i)) == offset) return i;
            }

            return -1;
        }

        // Give the hex to the selected bonus, or take it away when that bonus already has it.
        // A hex belongs to one bonus only, so it is taken from whichever other bonus held it.
        private void Toggle(SerializedProperty marks, Vector2Int offset)
        {
            if (marks.arraySize == 0) AddMark(marks);

            bool wasInSelected = false;
            for (int i = 0; i < marks.arraySize; i++)
            {
                SerializedProperty hexes = HexesOf(marks, i);
                int index = IndexOfHex(hexes, offset);
                if (index < 0) continue;

                hexes.DeleteArrayElementAtIndex(index);
                if (i == _selectedMark) wasInSelected = true;
            }

            if (wasInSelected) return;

            SerializedProperty selected = HexesOf(marks, _selectedMark);
            selected.arraySize++;

            SerializedProperty added = selected.GetArrayElementAtIndex(selected.arraySize - 1);
            AlignHexOffset.Write(added, offset);
            added.FindPropertyRelative("isWearing").boolValue = false;
        }

        // The "isWearing" of this hex of the bonus; null when the bonus does not have the hex.
        private static SerializedProperty WearingOf(SerializedProperty marks, int mark, Vector2Int offset)
        {
            SerializedProperty hexes = HexesOf(marks, mark);
            int index = IndexOfHex(hexes, offset);

            return index >= 0 ? hexes.GetArrayElementAtIndex(index).FindPropertyRelative("isWearing") : null;
        }

        // Is this hex of the bonus a wearing hex?
        private static bool IsWearing(SerializedProperty marks, int mark, Vector2Int offset)
            => WearingOf(marks, mark, offset)?.boolValue ?? false;

        private static void SetWearing(SerializedProperty marks, int mark, Vector2Int offset, bool isWearing)
        {
            SerializedProperty wearing = WearingOf(marks, mark, offset);
            if (wearing != null) wearing.boolValue = isWearing;
        }
    }
}
