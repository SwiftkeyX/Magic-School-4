using System;
using UnityEditor;
using UnityEngine;

namespace MagicSchool.Editor
{
    // One look for every Magic School 4 window (Align Editor, Skill Tuner, Benchmark, Stage Editor), so they read as one
    // set of tools. A window draws its common pieces through these rather than picking its own sizes and styles:
    //      Intro       - the grey line at the top saying what the window does
    //      Tabs        - the row of tabs under it
    //      SubTabs     - a smaller row of tabs inside one tab
    //      Search      - a toolbar search field, no label
    //      Section     - a bold heading inside a window or a tab
    //      Hint        - a small grey note under something
    //      Status      - why something cannot be done yet, or what to do first
    //      Action      - a main button (Save, Run, Write to code): tall
    //      RowButton   - a small button on a row of a list (Add, Load, Reset, Remove)
    //      ListRow     - one row of an asset list: its button first, the name beside it, details, Select at the far end
    //      ListItem    - one pickable entry of a side list (a hero, a skill)
    //      Sidebar     - the side list's width
    // ListRow lights the row under the mouse only when the window sets wantsMouseMove and repaints on MouseMove.
    internal static class WindowStyle
    {
        public const float Sidebar = 180f;
        public const float ActionHeight = 24f;
        public const float RowButtonWidth = 56f;
        public const float Gap = 6f;
        public const float SubTabIndent = 12f;

        private static GUIStyle _listItem;

        private static Color Stripe => EditorGUIUtility.isProSkin ? new Color(1f, 1f, 1f, 0.035f) : new Color(0f, 0f, 0f, 0.045f);
        private static Color Hover => EditorGUIUtility.isProSkin ? new Color(1f, 1f, 1f, 0.07f) : new Color(0f, 0f, 0f, 0.08f);

        public enum RowClick { None, Button, Select }

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

        public static int SubTabs(int selected, GUIContent[] tabs)
        {
            int picked;
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Space(SubTabIndent);
                picked = GUILayout.Toolbar(selected, tabs, EditorStyles.toolbarButton);
            }
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

        public static RowClick ListRow(int index, GUIContent button, bool buttonEnabled, GUIContent name, float nameWidth,
                                       string detail, GUIStyle nameStyle = null)
        {
            RowClick click = RowClick.None;

            Rect row = EditorGUILayout.BeginHorizontal();
            if (Event.current.type == EventType.Repaint)
            {
                if (index % 2 == 1) EditorGUI.DrawRect(row, Stripe);
                if (row.Contains(Event.current.mousePosition)) EditorGUI.DrawRect(row, Hover);
            }

            using (new EditorGUI.DisabledScope(!buttonEnabled))
            {
                if (RowButton(button)) click = RowClick.Button;
            }
            GUILayout.Space(Gap);
            GUILayout.Label(name, nameStyle ?? EditorStyles.label, GUILayout.Width(nameWidth));
            GUILayout.Label(detail, EditorStyles.miniLabel);
            GUILayout.FlexibleSpace();
            if (RowButton(new GUIContent("Select", "Show it in the Project window."))) click = RowClick.Select;

            EditorGUILayout.EndHorizontal();
            return click;
        }

        public static float NameWidth(System.Collections.Generic.IEnumerable<string> names)
        {
            float widest = 80f;
            foreach (string name in names) widest = Mathf.Max(widest, EditorStyles.boldLabel.CalcSize(new GUIContent(name)).x);
            return widest + 4f;
        }

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
