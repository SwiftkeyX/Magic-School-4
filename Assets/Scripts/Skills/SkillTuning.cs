using System.Collections.Generic;
using System.IO;

namespace MagicSchool.Skills
{
    /// <summary>
    /// The skill numbers that can be tuned while the game runs.
    /// A skill script writes its number as `private static float DamageRatio => Tuned(200f);` - the 200 in the code
    /// is the default, and an override set here (by Magic School 4 > Skill Tuner) wins over it.
    /// A skill reads its numbers when it is built, so a hero picks up a change the next time its skill is rebuilt:
    /// CombatState does that at the start of every fight for a hero whose skill is older than `Version`.
    /// </summary>
    // A number is keyed "ElfSkill.DamageRatio" (file name + member). Its default is remembered the first time it is
    // read, and kept in step if the code is recompiled with a new one. Version goes up on every change, so a hero can
    // tell its skill was built with older numbers.
    public static class SkillTuning
    {
        public class Number
        {
            public string Key;
            public string Skill;
            public string Name;
            public string FilePath;
            public float Default;
            public bool IsInt;
        }

        private static readonly Dictionary<string, Number> Numbers = new Dictionary<string, Number>();
        private static readonly Dictionary<string, float> Overrides = new Dictionary<string, float>();

        public static int Version { get; private set; }

        public static IEnumerable<Number> All => Numbers.Values;
        public static IReadOnlyDictionary<string, float> OverrideValues => Overrides;

        internal static float Get(string filePath, string name, float defaultValue, bool isInt)
        {
            string skill = Path.GetFileNameWithoutExtension(filePath);
            string key = skill + "." + name;

            if (!Numbers.TryGetValue(key, out Number number))
            {
                number = new Number { Key = key, Skill = skill, Name = name, FilePath = filePath, IsInt = isInt };
                Numbers[key] = number;
            }
            number.Default = defaultValue;

            return Overrides.TryGetValue(key, out float value) ? value : defaultValue;
        }

        public static float ValueOf(Number number)
            => Overrides.TryGetValue(number.Key, out float value) ? value : number.Default;

        public static void Set(string key, float value)
        {
            if (Overrides.TryGetValue(key, out float old) && old == value) return;

            Overrides[key] = value;
            Version++;
        }

        public static void Clear(string key)
        {
            if (Overrides.Remove(key)) Version++;
        }

        public static void ClearAll()
        {
            if (Overrides.Count == 0) return;

            Overrides.Clear();
            Version++;
        }
    }
}
