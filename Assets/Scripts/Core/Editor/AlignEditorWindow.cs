using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using MagicSchool.Combat.Heroes;
using MagicSchool.Skills;

namespace MagicSchool.Editor
{
    // Magic School 4 > Align Editor
    // A editor window for editing each hero's align mark.
    // Solved problem that we need to manually typing directions and distances in the Inspector.
    //
    // One window, four files:
    //      AlignEditorWindow.cs            => the window, and the heroes on the left
    //      AlignEditorWindow.Patterns.cs   => the pattern tabs above the hexes
    //      AlignEditorWindow.Hexes.cs      => the hexes in the middle
    //      AlignEditorWindow.Marks.cs      => the marks on the right (orange, green, the skill's white ones), and editing which hex belongs to which
    // The hex maths is in AlignHexOffset.cs.
    //
    // NOTE: The editor also work during Play mode
    internal partial class AlignEditorWindow : EditorWindow
    {
        private const string HeroFolder = "Assets/Data/Heroes/Stats/Player";

        private List<HeroDataSO> _heroes = new List<HeroDataSO>();
        private HeroDataSO _hero;
        private SerializedObject _serializedHero;
        private IReadOnlyList<string> _specialMarks = new string[0];   // the special marks the hero's skill reads (See AlignMark)
        private int _selectedPattern;
        private int _selectedMark;
        private string _heroSearch = string.Empty;
        private Vector2 _heroScroll;

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

        // the skill is built to ask it for its special marks, and building one needs the registry
        private static TemplateActionRegistrySO FindRegistry()
        {
            string guid = AssetDatabase.FindAssets("t:TemplateActionRegistrySO").FirstOrDefault();
            return guid != null ? AssetDatabase.LoadAssetAtPath<TemplateActionRegistrySO>(AssetDatabase.GUIDToAssetPath(guid)) : null;
        }

        private void Edit(HeroDataSO hero)
        {
            if (hero == _hero && _serializedHero != null) return;

            _hero = hero;
            _serializedHero = hero != null ? new SerializedObject(hero) : null;
            _specialMarks = hero != null ? hero.SpecialMarksOf(FindRegistry()) : new string[0];
            _selectedPattern = 0;
            _selectedMark = 0;
        }

        // ======================================== draw ========================================
        private void OnGUI()
        {
            // the asset was deleted while the window was open
            if (_hero == null) _serializedHero = null;

            WindowStyle.Intro("Edit a hero's align marks by clicking the hexes around it. Orange pays the hero, green pays the ally " +
                              "standing there; each pattern is one way the hero can be turned.");
            EditorGUILayout.BeginHorizontal();
            DrawHeroList();
            DrawHero();
            EditorGUILayout.EndHorizontal();
        }

        private void DrawHero()
        {
            if (_serializedHero == null)
            {
                WindowStyle.Status("Pick a hero on the left, or select a HeroDataSO in the Project window.");
                return;
            }

            _serializedHero.Update();
            SerializedProperty patterns = _serializedHero.FindProperty("_alignPatterns");

            // no pattern yet: only looking at the hero must not write one into its asset
            if (patterns.arraySize == 0)
            {
                DrawNoPattern(patterns);
                return;
            }

            _selectedPattern = Mathf.Clamp(_selectedPattern, 0, patterns.arraySize - 1);
            SerializedProperty marks = MarksOf(patterns, _selectedPattern);
            AddMissingMarks(marks);
            MatchBonusesToFirstPattern(patterns);

            EditorGUILayout.BeginVertical();
            DrawHeader();
            DrawPatternTabs(patterns);
            DrawHexes(marks);
            EditorGUILayout.EndVertical();

            DrawMarks(marks);

            _serializedHero.ApplyModifiedProperties();
        }

        // the hero's name, and a way to try it out
        private void DrawHeader()
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(_hero.name, EditorStyles.boldLabel);

            // Play mode only: get this hero onto the bench without rolling the shop for it
            using (new EditorGUI.DisabledScope(!AddHeroMenu.CanAdd))
            {
                if (WindowStyle.RowButton(new GUIContent("Add to bench", "Play mode: put this hero on the bench for free."), 90f))
                    AddHeroMenu.Add(_hero);
            }

            EditorGUILayout.EndHorizontal();
        }

        // ---------------------------------------- left: the heroes ----------------------------------------
        private void DrawHeroList()
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(WindowStyle.Sidebar));

            // type part of a name to narrow the list, in any letter case
            _heroSearch = WindowStyle.Search(_heroSearch);

            _heroScroll = EditorGUILayout.BeginScrollView(_heroScroll);

            bool isAnyShown = false;
            foreach (HeroDataSO hero in _heroes.Where(IsSearched))
            {
                isAnyShown = true;
                bool isEdited = hero == _hero;
                if (WindowStyle.ListItem(isEdited, hero.name)) Edit(hero);
            }

            if (!isAnyShown) WindowStyle.Hint("No hero by that name.");

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        private bool IsSearched(HeroDataSO hero)
        {
            if (string.IsNullOrWhiteSpace(_heroSearch)) return true;

            return hero.name.IndexOf(_heroSearch.Trim(), System.StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
