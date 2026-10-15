using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using MagicSchool.Combat.Heroes;

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
    //      AlignEditorWindow.Marks.cs      => the bonuses on the right, and editing which hex belongs to which
    // The hex maths is in AlignHexOffset.cs.
    //
    // NOTE: The editor also work during Play mode
    internal partial class AlignEditorWindow : EditorWindow
    {
        private const string HeroFolder = "Assets/Data/Heroes/Stats/Player";

        private List<HeroDataSO> _heroes = new List<HeroDataSO>();
        private HeroDataSO _hero;
        private SerializedObject _serializedHero;
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

        private void Edit(HeroDataSO hero)
        {
            if (hero == _hero && _serializedHero != null) return;

            _hero = hero;
            _serializedHero = hero != null ? new SerializedObject(hero) : null;
            _selectedPattern = 0;
            _selectedMark = 0;
        }

        // ======================================== draw ========================================
        private void OnGUI()
        {
            // the asset was deleted while the window was open
            if (_hero == null) _serializedHero = null;

            EditorGUILayout.BeginHorizontal();
            DrawHeroList();
            DrawHero();
            EditorGUILayout.EndHorizontal();
        }

        private void DrawHero()
        {
            if (_serializedHero == null)
            {
                EditorGUILayout.HelpBox("Pick a hero on the left, or select a HeroDataSO in the Project window.", MessageType.Info);
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
            _selectedMark = Mathf.Clamp(_selectedMark, 0, Mathf.Max(0, marks.arraySize - 1));

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
                if (GUILayout.Button("Add to bench", EditorStyles.miniButton, GUILayout.Width(90f))) AddHeroMenu.Add(_hero);
            }

            EditorGUILayout.EndHorizontal();
        }

        // ---------------------------------------- left: the heroes ----------------------------------------
        private void DrawHeroList()
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(170f));

            // type part of a name to narrow the list, in any letter case
            _heroSearch = EditorGUILayout.TextField(_heroSearch, EditorStyles.toolbarSearchField);

            _heroScroll = EditorGUILayout.BeginScrollView(_heroScroll);

            bool isAnyShown = false;
            foreach (HeroDataSO hero in _heroes.Where(IsSearched))
            {
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
    }
}
