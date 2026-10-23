using System;
using UnityEditor;
using UnityEngine;

namespace MagicSchool.Editor
{
    // One look for every Magic School 4 window (Align Editor, Skill Tuner, Benchmark, Stage Editor), so they read as one
    // set of tools. A window draws its common pieces through these rather than picking its own sizes and styles:
    //      Intro       - the grey line at the top saying what the window does
    //      Tabs        - the row of tabs under it
    //      Search      - a toolbar search field, no label
    //      Section     - a bold heading inside a window or a tab
    //      Hint        - a small grey note under something
    //      Status      - why something cannot be done yet, or what to do first
    //      Action      - a main button (Save, Run, Write to code): tall
    //      RowButton   - a small button on a row of a list (Add, Load, Reset, Remove)
    //      ListItem    - one pickable entry of a side list (a hero, a skill)
    //      Sidebar     - the side list's width
    internal static class WindowStyle
    {
        public const float Sidebar = 180f;
        public const float ActionHeight = 24f;
        public const float RowButtonWidth = 56f;
        public const float Gap = 6f;

        private static GUIStyle _listItem;

        public static void Intro(string text)
        {
            EditorGUILayout.LabelField(text, EditorStyles.wordWrappedMiniLabel);
            EditorGUILayout.Space(Gap);
        }

        public static int Tabs(int selected, GUIContent[] tabs)
        {
            int picked = GUILayout.Toolbar(selected, tabs, GUILayout.Height(ActionHeight));
            EditorGUILayout.Space(Gap);
            return picked;
        }

        public static string Search(string text) => EditorGUILayout.TextField(text ?? "", EditorStyles.toolbarSearchField);

        public static void Section(string title)
        {
            EditorGUILayout.Space(Gap);
            EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
        }

        public static void Hint(string text) => EditorGUILayout.LabelField(text, EditorStyles.wordWrappedMiniLabel);

        public static void Status(string text) => EditorGUILayout.HelpBox(text, MessageType.Info);

        public static bool Action(GUIContent content, params GUILayoutOption[] options)
            => GUILayout.Button(content, With(options, GUILayout.Height(ActionHeight)));

        public static bool Action(string text, params GUILayoutOption[] options) => Action(new GUIContent(text), options);

        public static bool RowButton(GUIContent content, float width = RowButtonWidth)
            => GUILayout.Button(content, EditorStyles.miniButton, GUILayout.Width(width));

        public static bool RowButton(string text, float width = RowButtonWidth) => RowButton(new GUIContent(text), width);

        // true when picked this frame - an entry already picked does not fire again
        public static bool ListItem(bool isPicked, string label)
        {
            _listItem ??= new GUIStyle(EditorStyles.miniButton) { alignment = TextAnchor.MiddleLeft, fixedHeight = 20f };
            return GUILayout.Toggle(isPicked, label, _listItem) && !isPicked;
        }

        private static GUILayoutOption[] With(GUILayoutOption[] options, GUILayoutOption extra)
        {
            var all = new GUILayoutOption[(options?.Length ?? 0) + 1];
            options?.CopyTo(all, 0);
            all[all.Length - 1] = extra;
            return all;
        }
    }
}
