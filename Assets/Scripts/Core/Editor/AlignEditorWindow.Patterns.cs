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
            if (GUILayout.Button("+ Empty", EditorStyles.miniButton, GUILayout.Width(60f))) AddPattern(patterns, rotatedFrom: -1);
            if (GUILayout.Button(new GUIContent("+ Rotated copy", "A new pattern: this one turned 60 degrees clockwise. Edit it by hand from there."),
                                 EditorStyles.miniButton, GUILayout.Width(100f))) AddPattern(patterns, rotatedFrom: _selectedPattern);

            using (new EditorGUI.DisabledScope(patterns.arraySize <= 1))
            {
                if (GUILayout.Button("Remove", EditorStyles.miniButton, GUILayout.Width(60f)))
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
            EditorGUILayout.LabelField(rotates, EditorStyles.wordWrappedMiniLabel);
        }

        private void DrawNoPattern(SerializedProperty patterns)
        {
            EditorGUILayout.BeginVertical();
            EditorGUILayout.LabelField(_hero.name, EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("This hero has no align marks.", MessageType.Info);
            if (GUILayout.Button("Start pattern 1", GUILayout.Width(140f))) AddPattern(patterns, rotatedFrom: -1);
            EditorGUILayout.EndVertical();
        }

        // The rest of this pass was laid out for the pattern selected before, which may be gone now: save, and start again
        private void SelectPattern(int pattern)
        {
            _selectedPattern = pattern;
            _selectedMark = 0;
            GUI.FocusControl(null);     // a field still focused would carry its typed value over to the other pattern

            _serializedHero.ApplyModifiedProperties();
            Repaint();
            GUIUtility.ExitGUI();
        }

        private static SerializedProperty MarksOf(SerializedProperty patterns, int pattern)
            => patterns.GetArrayElementAtIndex(pattern).FindPropertyRelative("marks");

        // A new pattern at the end, and select it: empty, or a copy of another pattern turned 60 degrees clockwise.
        private void AddPattern(SerializedProperty patterns, int rotatedFrom)
        {
            // Unity fills a new list entry with a copy of the last one, so copy from the wanted one, or wipe it
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

        // Every hex of every mark turned 60 degrees clockwise round the hero: N becomes NE, NE becomes SE, and so on.
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
