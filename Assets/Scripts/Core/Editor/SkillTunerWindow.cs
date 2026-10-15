using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using MagicSchool.Combat.Heroes;
using MagicSchool.Skills;

namespace MagicSchool.Editor
{
    // Magic School 4 > Skill Tuner
    // Change a skill's numbers (200% AD, a stun's seconds, how many shots) while the game runs, then write
    // the ones worth keeping back into the skill's script.
    //
    // A number is tunable when its skill script writes it as `private static float DamageRatio => Tuned(200f);`
    // (read SkillTuning.cs). The value in the code is the default; a value changed here overrides it.
    //      - the hero inspector's description follows a change at once
    //      - what the skill does follows it at the next fight start, when CombatState rebuilds the skill
    //      - "Write to code" puts the changed value in the script, replacing the default
    //
    // The changed values are kept by the window, so they survive entering and leaving Play mode,
    // but not closing the window - write them to code first.
    internal class SkillTunerWindow : EditorWindow
    {
        [Serializable]
        private class Change
        {
            public string Key;
            public float Value;
        }

        // kept by the window: SkillTuning is static and is wiped by every domain reload
        [SerializeField] private List<Change> _changes = new List<Change>();
        [SerializeField] private string _selectedSkill;

        private List<string> _skills = new List<string>();
        private Dictionary<string, Type> _skillTypes = new Dictionary<string, Type>();
        private string _search = string.Empty;
        private Vector2 _skillScroll;
        private Vector2 _numberScroll;

        [MenuItem("Magic School 4/Skill Tuner")]
        private static void Open() => GetWindow<SkillTunerWindow>("Skill Tuner");

        // ======================================== life cycle ========================================
        private void OnEnable()
        {
            FindSkills();

            // a change already written to code is the default now
            _changes.RemoveAll(change => !IsKnown(change.Key) || Default(change.Key) == change.Value);
            PushChanges();

            if (Selection.activeObject is HeroDataSO selected) SelectSkillOf(selected);
            else if (string.IsNullOrEmpty(_selectedSkill) && _skills.Count > 0) _selectedSkill = _skills[0];
        }

        // picking a hero in the Project window picks its skill here
        private void OnSelectionChange()
        {
            if (!(Selection.activeObject is HeroDataSO selected)) return;

            SelectSkillOf(selected);
            Repaint();
        }

        // the in-game description is read live, so keep it fresh while playing
        private void OnInspectorUpdate()
        {
            if (Application.isPlaying) Repaint();
        }

        // Every skill script deriving SkillDefinition. Reading each of its static numbers once makes
        // SkillTuning learn the number and its default.
        private void FindSkills()
        {
            _skillTypes = typeof(SkillDefinition).Assembly.GetTypes()
                .Where(type => type.IsSubclassOf(typeof(SkillDefinition)) && !type.IsAbstract)
                .ToDictionary(type => type.Name);

            foreach (Type type in _skillTypes.Values)
            {
                foreach (PropertyInfo property in type.GetProperties(BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
                {
                    if (property.PropertyType != typeof(float) && property.PropertyType != typeof(int)) continue;

                    try { property.GetValue(null); }
                    catch (Exception e) { Debug.LogWarning($"[SkillTuner] could not read {type.Name}.{property.Name}: {e.InnerException?.Message ?? e.Message}"); }
                }
            }

            _skills = SkillTuning.All.Select(number => number.Skill).Distinct().OrderBy(skill => skill).ToList();
        }

        private void SelectSkillOf(HeroDataSO hero)
        {
            SerializedProperty skillId = new SerializedObject(hero).FindProperty("_skillId");
            if (skillId == null) return;

            string skill = skillId.enumNames[skillId.enumValueIndex] + "Skill";
            if (_skills.Contains(skill)) _selectedSkill = skill;
        }

        // ======================================== changes ========================================
        private bool IsKnown(string key) => SkillTuning.All.Any(number => number.Key == key);
        private float Default(string key) => SkillTuning.All.First(number => number.Key == key).Default;
        private Change ChangeOf(string key) => _changes.FirstOrDefault(change => change.Key == key);
        private bool HasChanges(string skill) => _changes.Any(change => change.Key.StartsWith(skill + "."));

        private void SetValue(SkillTuning.Number number, float value)
        {
            Change change = ChangeOf(number.Key);

            if (value == number.Default)
            {
                if (change != null) _changes.Remove(change);
                SkillTuning.Clear(number.Key);
                return;
            }

            if (change == null) _changes.Add(change = new Change { Key = number.Key });
            change.Value = value;
            SkillTuning.Set(number.Key, value);
        }

        private void PushChanges()
        {
            SkillTuning.ClearAll();
            foreach (Change change in _changes) SkillTuning.Set(change.Key, change.Value);
        }

        private void Revert(IEnumerable<SkillTuning.Number> numbers)
        {
            foreach (SkillTuning.Number number in numbers.ToList()) SetValue(number, number.Default);
        }

        // ======================================== draw ========================================
        private void OnGUI()
        {
            EditorGUILayout.BeginHorizontal();
            DrawSkillList();
            DrawSkill();
            EditorGUILayout.EndHorizontal();
        }

        private void DrawSkillList()
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(180f));
            _search = EditorGUILayout.TextField(_search, EditorStyles.toolbarSearchField);

            _skillScroll = EditorGUILayout.BeginScrollView(_skillScroll);
            foreach (string skill in _skills)
            {
                if (_search.Length > 0 && skill.IndexOf(_search, StringComparison.OrdinalIgnoreCase) < 0) continue;

                // a skill with changed numbers is marked, so nothing unsaved is forgotten
                string label = skill.Replace("Skill", "") + (HasChanges(skill) ? "  *" : "");
                bool picked = GUILayout.Toggle(skill == _selectedSkill, label, EditorStyles.toolbarButton);
                if (picked && skill != _selectedSkill)
                {
                    _selectedSkill = skill;
                    GUI.FocusControl(null);
                }
            }
            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space();
            using (new EditorGUI.DisabledScope(_changes.Count == 0))
            {
                if (GUILayout.Button($"Write all to code ({_changes.Count})")) WriteToCode(_changes.ToList());
                if (GUILayout.Button("Revert all")) Revert(SkillTuning.All);
            }
            EditorGUILayout.EndVertical();
        }

        private void DrawSkill()
        {
            EditorGUILayout.BeginVertical();

            if (string.IsNullOrEmpty(_selectedSkill) || !_skills.Contains(_selectedSkill))
            {
                EditorGUILayout.HelpBox("Pick a skill on the left, or select a HeroDataSO in the Project window.", MessageType.Info);
                EditorGUILayout.EndVertical();
                return;
            }

            List<SkillTuning.Number> numbers = SkillTuning.All.Where(number => number.Skill == _selectedSkill).ToList();

            EditorGUILayout.LabelField($"{_selectedSkill.Replace("Skill", "")} - {SkillNameOf(_selectedSkill)}", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(Application.isPlaying
                ? "A change takes effect at the next fight start (space bar). The description follows it now."
                : "Not playing: a change takes effect from the next Play.", MessageType.None);

            _numberScroll = EditorGUILayout.BeginScrollView(_numberScroll);
            foreach (SkillTuning.Number number in numbers) DrawNumber(number);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Description", EditorStyles.miniBoldLabel);
            EditorGUILayout.HelpBox(DescriptionOf(_selectedSkill), MessageType.None);
            EditorGUILayout.EndScrollView();

            EditorGUILayout.BeginHorizontal();
            using (new EditorGUI.DisabledScope(!HasChanges(_selectedSkill)))
            {
                if (GUILayout.Button("Write to code"))
                    WriteToCode(_changes.Where(change => change.Key.StartsWith(_selectedSkill + ".")).ToList());
                if (GUILayout.Button("Revert")) Revert(numbers);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        private void DrawNumber(SkillTuning.Number number)
        {
            float value = SkillTuning.ValueOf(number);
            bool changed = ChangeOf(number.Key) != null;

            EditorGUILayout.BeginHorizontal();

            GUIStyle label = changed ? EditorStyles.boldLabel : EditorStyles.label;
            EditorGUILayout.LabelField(new GUIContent(number.Name, number.Key), label, GUILayout.Width(170f));

            float newValue = number.IsInt
                ? EditorGUILayout.IntField(Mathf.RoundToInt(value), GUILayout.Width(80f))
                : EditorGUILayout.FloatField(value, GUILayout.Width(80f));
            if (newValue != value) SetValue(number, newValue);

            EditorGUILayout.LabelField($"code: {Format(number.Default, number.IsInt)}", EditorStyles.miniLabel, GUILayout.Width(90f));

            using (new EditorGUI.DisabledScope(!changed))
            {
                if (GUILayout.Button("Reset", EditorStyles.miniButton, GUILayout.Width(50f))) SetValue(number, number.Default);
            }

            EditorGUILayout.EndHorizontal();
        }

        // ======================================== skill text ========================================
        // A skill built with no registry and no caster only knows its text - the shop's preview does the same.
        private SkillDefinition Instance(string skill)
        {
            if (!_skillTypes.TryGetValue(skill, out Type type)) return null;

            try
            {
                return (SkillDefinition)Activator.CreateInstance(type, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public,
                                                                 null, new object[] { null }, CultureInfo.InvariantCulture);
            }
            catch { return null; }
        }

        private string SkillNameOf(string skill)
        {
            try { return Instance(skill)?.SkillName ?? ""; }
            catch { return ""; }
        }

        private string DescriptionOf(string skill)
        {
            try
            {
                SkillDefinition instance = Instance(skill);
                if (instance == null) return "(no description)";

                string passive = instance.PassiveDescription;
                return string.IsNullOrEmpty(passive) ? instance.Description : instance.Description + "\n\nPassive: " + passive;
            }
            catch (Exception e) { return $"(the description could not be built: {e.Message})"; }
        }

        // ======================================== write to code ========================================
        // Replace the default inside `Name => Tuned(...)` in the skill's script with the changed value.
        // While playing, the script is only recompiled once Play mode ends; the change stays applied until then.
        private void WriteToCode(List<Change> changes)
        {
            int written = 0;

            foreach (IGrouping<string, Change> file in changes.GroupBy(change => FileOf(change.Key)))
            {
                if (string.IsNullOrEmpty(file.Key) || !File.Exists(file.Key))
                {
                    Debug.LogError($"[SkillTuner] the script for {file.First().Key} was not found.");
                    continue;
                }

                string code = File.ReadAllText(file.Key);
                foreach (Change change in file)
                {
                    SkillTuning.Number number = SkillTuning.All.First(n => n.Key == change.Key);
                    var pattern = new Regex(@"(\bstatic\s+(?:float|int)\s+" + Regex.Escape(number.Name) + @"\s*=>\s*Tuned\()[^)]*(\))");

                    if (!pattern.IsMatch(code))
                    {
                        Debug.LogError($"[SkillTuner] `{number.Name} => Tuned(...)` was not found in {Path.GetFileName(file.Key)}.");
                        continue;
                    }

                    code = pattern.Replace(code, "${1}" + Literal(change.Value, number.IsInt) + "${2}", 1);
                    written++;
                }
                File.WriteAllText(file.Key, code);
            }

            Debug.Log($"[SkillTuner] wrote {written} number(s) to code.");

            if (Application.isPlaying) EditorApplication.playModeStateChanged += RefreshAfterPlay;
            else AssetDatabase.Refresh();
        }

        private static void RefreshAfterPlay(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.EnteredEditMode) return;

            EditorApplication.playModeStateChanged -= RefreshAfterPlay;
            AssetDatabase.Refresh();
        }

        private static string FileOf(string key) => SkillTuning.All.FirstOrDefault(number => number.Key == key)?.FilePath;

        private static string Format(float value, bool isInt)
            => isInt ? Mathf.RoundToInt(value).ToString(CultureInfo.InvariantCulture) : value.ToString(CultureInfo.InvariantCulture);

        private static string Literal(float value, bool isInt) => isInt ? Format(value, true) : Format(value, false) + "f";
    }
}
