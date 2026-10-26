using UnityEditor;
using UnityEngine;

namespace MagicSchool.Editor
{
    // ---------------------------------------- top: the patterns ----------------------------------------
    // One tab per pattern. In the game, right-clicking while holding a hero turns it to the next pattern, in this order.
    internal partial class AlignEditorWindow
    {
        private void DrawPatternTabs(SerializedProperty patterns)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Pattern", GUILayout.Width(50f));

            for (int i = 0; i < patterns.arraySize; i++)
            {
                bool isSelected = i == _selectedPattern;
                if (GUILayout.Toggle(isSelected, (i + 1).ToString(), EditorStyles.miniButtonMid, GUILayout.Width(28f)) && !isSelected) SelectPattern(i);
            }

            GUILayout.Space(8f);
            if (WindowStyle.RowButton("+ Empty", 60f)) AddPattern(patterns, rotatedFrom: -1);
            if (WindowStyle.RowButton(new GUIContent("+ Rotated copy", "A new pattern: this one turned 60 degrees clockwise. Edit it by hand from there."), 100f)) AddPattern(patterns, rotatedFrom: _selectedPattern);

            using (new EditorGUI.DisabledScope(patterns.arraySize <= 1))
            {
                if (WindowStyle.RowButton("Remove", 60f))
                {
                    patterns.DeleteArrayElementAtIndex(_selectedPattern);
                    SelectPattern(Mathf.Max(0, _selectedPattern - 1));
                }
            }

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            string rotates = patterns.arraySize > 1
                ? $"{patterns.arraySize} patterns: right-clicking while holding this hero turns it through them in the game."
                : "One pattern: this hero cannot rotate. Add a pattern to let it.";
            WindowStyle.Hint(rotates);
        }

        private void DrawNoPattern(SerializedProperty patterns)
        {
            EditorGUILayout.BeginVertical();
            EditorGUILayout.LabelField(_hero.name, EditorStyles.boldLabel);
            WindowStyle.Status("This hero has no align marks.");
            if (WindowStyle.Action("Start pattern 1", GUILayout.Width(140f))) AddPattern(patterns, rotatedFrom: -1);
            EditorGUILayout.EndVertical();
        }

        private void SelectPattern(int pattern)
        {
            _selectedPattern = pattern;
            _selectedMark = 0;
            GUI.FocusControl(null);

            _serializedHero.ApplyModifiedProperties();
            Repaint();
            GUIUtility.ExitGUI();
        }

        private static SerializedProperty MarksOf(SerializedProperty patterns, int pattern)
            => patterns.GetArrayElementAtIndex(pattern).FindPropertyRelative("marks");

        private void AddPattern(SerializedProperty patterns, int rotatedFrom)
        {
            if (rotatedFrom >= 0)
            {
                patterns.InsertArrayElementAtIndex(rotatedFrom);
                patterns.MoveArrayElement(rotatedFrom + 1, patterns.arraySize - 1);
            }
            else
            {
                patterns.arraySize++;
            }

            int added = patterns.arraySize - 1;
            SerializedProperty marks = MarksOf(patterns, added);

            if (rotatedFrom < 0) marks.ClearArray();
            else RotateClockwise(marks);

            SelectPattern(added);
        }

        private static void RotateClockwise(SerializedProperty marks)
        {
            for (int i = 0; i < marks.arraySize; i++)
            {
                SerializedProperty hexes = HexesOf(marks, i);

                for (int h = 0; h < hexes.arraySize; h++)
                {
                    SerializedProperty hex = hexes.GetArrayElementAtIndex(h);
                    AlignHexOffset.Write(hex, AlignHexOffset.RotatedClockwise(AlignHexOffset.Read(hex)));
                }
            }
        }
    }
}
