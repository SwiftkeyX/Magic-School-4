using UnityEngine;

namespace MagicSchool.Core.Benchmark
{
    /// <summary>
    /// Every number the two benchmarks run with, in one asset (Data/Benchmark/BenchmarkSettings.asset), edited in
    /// Magic School 4 > Balance > Benchmark (Core/Editor/BenchmarkWindow). The menu hands it to Run; a benchmark reads
    /// it through Current, which falls back to these defaults when nothing was handed over.
    /// </summary>
    [CreateAssetMenu(fileName = "BenchmarkSettings", menuName = "Magic School 4/Benchmark Settings")]
    public class BenchmarkSettingsSO : ScriptableObject
    {
        [Header("Hero Benchmark - tests")]
        [Tooltip("Seconds into the damage and AoE tests the first DPS sample is taken (dps_15s, aoe_dps_15s).")]
        [Min(1f)] public float shortSeconds = 15f;

        [Tooltip("Length of the damage and AoE tests, and the longest the tank test lasts. The main DPS numbers are taken here.")]
        [Min(1f)] public float longSeconds = 30f;

        [Tooltip("Tank test: raw damage put on the hero each hit.")]
        [Min(0)] public int pressurePerHit = 50;

        [Tooltip("Tank test: seconds between those hits. 50 every 0.5s = 100 raw damage per second.")]
        [Min(0.05f)] public float pressureInterval = 0.5f;

        [Tooltip("A dummy is healed back to full when it drops below this share of its HP, so it never dies mid-test.")]
        [Range(0.05f, 1f)] public float dummyRefillBelow = 0.5f;

        [Header("Hero Benchmark - marks")]
        [Tooltip("The smaller team: how many stand-in allies may fill the hero's own orange mark hexes " +
                 "(2 = a team of 3, stage 1). 0 = skip this pass.")]
        [Range(0, 12)] public int alliesSmallTeam = 2;

        [Tooltip("The bigger team: how many stand-in allies may fill its mark hexes (4 = a team of 5). Its numbers are the " +
                 "report's main ones and drive the flags. 0 = no marks at all: the main numbers are the hero alone.")]
        [Range(0, 12)] public int alliesBigTeam = 4;

        [Header("Hero Benchmark - report")]
        [Tooltip("A hero further than this from its role's median is flagged HIGH or LOW (0.2 = 20%).")]
        [Range(0.01f, 1f)] public float flagBand = 0.2f;

        [Header("Hero Benchmark - speed only (does not change results)")]
        [Tooltip(SpeedOnly + " More is faster until there are as many lanes as heroes to test (24 for the whole shop); " +
                 "past that it does nothing.")]
        [Range(1, 64)] public int heroLanes = 8;

        [Header("Team Comp Benchmark - fights")]
        [Tooltip("Times each comp fights each stage with aligning. More = steadier win rates, longer runs.")]
        [Min(1)] public int runsAligned = 20;

        [Tooltip("Times each comp fights each stage with aligning off - the baseline. 0 = skip the not-aligned fights.")]
        [Min(0)] public int runsNotAligned = 10;

        [Tooltip("A fight still going after this many seconds is a timeout, which is not a win.")]
        [Min(5f)] public float timeLimit = 90f;

        [Header("Team Comp Benchmark - report")]
        [Tooltip("A stage counts as won at this share of its fights or more (0.7 = 14 of 20). It is written into the report, " +
                 "so the Sheet colours and counts each run by the line it was run with.")]
        [Range(0.05f, 1f)] public float winLine = 0.7f;

        [Header("Team Comp Benchmark - speed only (does not change results)")]
        [Tooltip(SpeedOnly + " 8 -> 32 made a run about 1.5x faster; past 32 each frame just gets heavier and it gains " +
                 "little. Very many lanes can make the Editor slow to come back when the run ends.")]
        [Range(1, 128)] public int teamLanes = 32;

        private const string SpeedOnly =
            "How many fights run at the same time. Speed only: a fight plays out the same however many run beside it - " +
            "each is on its own hidden copy of the board, far enough from the others that no skill reaches across, and " +
            "time moves in the same fixed 1/30s steps. Each fight still gets its own random rolls, so two runs differ a " +
            "little whatever this is set to.";

        public const string AssetPath = "Assets/Data/Benchmark/BenchmarkSettings.asset";

        private static BenchmarkSettingsSO _current;

        public static BenchmarkSettingsSO Current
        {
            get
            {
                if (_current == null) _current = CreateInstance<BenchmarkSettingsSO>();
                return _current;
            }
        }

        internal static void Use(BenchmarkSettingsSO settings)
        {
            if (settings != null) _current = settings;
        }

        private void OnValidate()
        {
            if (shortSeconds > longSeconds) shortSeconds = longSeconds;
            if (alliesSmallTeam > alliesBigTeam) alliesSmallTeam = alliesBigTeam;
        }
    }
}
