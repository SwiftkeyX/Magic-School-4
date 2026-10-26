using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using MagicSchool.Contracts;
using MagicSchool.Combat.Heroes;
using MagicSchool.Combat.Placements;

namespace MagicSchool.Core.Benchmark
{
    /// <summary>
    /// Balancing helper: runs every hero through the same three tests, result into CSV
    ///     Damage test - a hero vs a dummy, for 30s. DPS at 15s and 30s
    ///     AoE test    - a hero vs a cluster of 6 dummies. 
    ///     Tank test   - a hero vs a dummy, while a damage continuously hits a hero, until it dies or 30s pass.
    ///
    /// Split by job across partial files:
    ///     HeroBenchmark.cs         - settings, Run, the loop over every batch of heroes, Result
    ///     HeroBenchmark.Tests.cs   - the three tests
    ///     HeroBenchmark.Board.cs   - the lanes, spawning, clearing, keeping the dummies alive
    ///     HeroBenchmark.Speed.cs   - fixed steps, no frame cap, nothing drawn - and putting it all back
    ///     HeroBenchmark.Report.cs  - peer comparison, flags, the CSV in and out
    /// </summary>
    // Each lane is a hidden copy of the board, stacked LaneGap apart: skill hits are physics triggers with no board
    // check, and the tallest layout is 7 hexes, so 20 units leaves a gap no skill reaches (0 cross-board hits measured
    // at 10). Copies share the one recorder, so every lane's numbers land in one place.
    // Three passes (alone, a smaller and a bigger team with its own orange hexes filled); a pass only runs the heroes
    // it changes, and the bigger team's numbers drive the role comparison and flags. A partial run borrows everyone
    // else's numbers from the newest full report. See .claude/docs/features/benchmarks.md.
    public partial class HeroBenchmark : MonoBehaviour
    {
        private static BenchmarkSettingsSO Settings => BenchmarkSettingsSO.Current;

        public static float ShortSeconds => Settings.shortSeconds;
        public static float LongSeconds => Settings.longSeconds;

        private static int LaneCount => Mathf.Max(1, Settings.heroLanes);

        private static float DummyRefillBelow => Settings.dummyRefillBelow;

        private static int PressurePerHit => Settings.pressurePerHit;
        private static float PressureInterval => Mathf.Max(0.05f, Settings.pressureInterval);

        private static int AlliesSmallTeam => Mathf.Min(Settings.alliesSmallTeam, Settings.alliesBigTeam);
        private static int AlliesBigTeam => Settings.alliesBigTeam;

        private static float FlagBand => Settings.flagBand;

        private const float TimeStep = 1f / 30f;
        private const float LaneSpacing = 20f;

        private static readonly HexNumber HeroHex = new HexNumber(3, 3);
        private static readonly HexNumber DummyHex = new HexNumber(4, 3);

        private static readonly HexNumber[] ClusterHexes =
        {
            DummyHex,
            new HexNumber(4, 2),
            new HexNumber(5, 2),
            new HexNumber(5, 3),
            new HexNumber(6, 2),
            new HexNumber(6, 3),
        };

        public static void Run(HeroDataSO dummy, IReadOnlyList<HeroDataSO> testHero, BenchmarkSettingsSO settings, Action<string> onDone)
        {
            BenchmarkSettingsSO.Use(settings);
            var go = new GameObject(nameof(HeroBenchmark));
            HeroBenchmark benchmark = go.AddComponent<HeroBenchmark>();
            benchmark.StartCoroutine(benchmark.RunAll(dummy, testHero, onDone));
        }

        private IEnumerator RunAll(HeroDataSO dummy, IReadOnlyList<HeroDataSO> testHero, Action<string> onDone)
        {
            yield return WaitForGame();

            GameManager game = GameManager.Instance;
            float realStart = Time.realtimeSinceStartup;

            bool isPartialMode = testHero != null;
            List<HeroDataSO> testRoster = HeroesToTest(game, testHero);
            var results = new List<Result>();

            GoFast();
            yield return TestEveryHero(game, testRoster, dummy, results);
            GoNormal();

            string path = Report(results, isPartialMode, realStart);

            onDone?.Invoke(path);
            Destroy(gameObject);
        }

        private static IEnumerator WaitForGame()
        {
            while (GameManager.Instance == null || GameManager.Instance.Phase != GamePhaseEnum.Preparation) yield return null;
            yield return null;
        }

        private static List<HeroDataSO> HeroesToTest(GameManager game, IReadOnlyList<HeroDataSO> testHero)
            => (testHero ?? game.ShopRoster).Where(h => h != null).Distinct().ToList();

        private IEnumerator TestEveryHero(GameManager game, List<HeroDataSO> testRoster, HeroDataSO dummy, List<Result> results)
        {
            game.Board.SetBattleOn(false);
            List<Lane> lanes = CreateLanes(game, Mathf.Min(LaneCount, testRoster.Count));
            BattleBoard board = lanes[0].Board;

            List<Result> everyone = testRoster.Select(hero => new Result(hero)).ToList();
            yield return TestPass(game, lanes, everyone, dummy, maxMarks: 0);
            foreach (Result result in everyone) result.KeepAlone();

            int Fill(Result r, int max) => MarkFill.Of(r.Hero, board, max).Count;
            List<Result> small = everyone.Where(r => r.Error == null && Fill(r, AlliesSmallTeam) > 0).ToList();
            yield return TestPass(game, lanes, small, dummy, AlliesSmallTeam);
            foreach (Result result in everyone) result.KeepSmallTeam();

            List<Result> big = everyone.Where(r => r.Error == null && Fill(r, AlliesBigTeam) > Fill(r, AlliesSmallTeam)).ToList();
            yield return TestPass(game, lanes, big, dummy, AlliesBigTeam);

            for (int i = 0; i < everyone.Count; i++)
            {
                results.Add(everyone[i]);
                LogProgress(everyone[i], i + 1, everyone.Count);
            }

            DestroyLanes(lanes);
        }

        private IEnumerator TestPass(GameManager game, List<Lane> lanes, List<Result> todo, HeroDataSO dummy, int maxMarks)
        {
            for (int first = 0; first < todo.Count; first += lanes.Count)
            {
                List<Lane> batch = lanes.Take(todo.Count - first).ToList();
                for (int i = 0; i < batch.Count; i++) batch[i].Result = todo[first + i];

                yield return TestBatch(game, batch, dummy, maxMarks);
            }
        }

        private IEnumerator TestBatch(GameManager game, List<Lane> batch, HeroDataSO dummy, int maxMarks)
        {
            yield return DamageTest(game, batch, dummy, maxMarks);
            yield return AoeTest(game, batch, dummy, maxMarks);
            yield return TankTest(game, batch, dummy, maxMarks);
        }

        private static void LogProgress(Result result, int done, int total)
        {
            Debug.Log($"[HeroBenchmark] {done}/{total} {result.Hero.Name}: " +
                      $"{result.Dps30:0.0} DPS @30s, AoE {result.AoeDps30:0.0} (x{result.AoeGain:0.0}), " +
                      $"{(result.Survived ? "survived" : $"died at {result.TimeToDie:0.0}s")}" +
                      (result.Marks > 0 ? $" - with {result.Marks} mark hexes filled; smaller team {result.Dps30Small:0.0}, " +
                                          $"alone {result.Dps30Alone:0.0} DPS" : ""));
        }

        // one hero's numbers across the three tests
        private class Result
        {
            public readonly HeroDataSO Hero;
            public float Dps15, Dps30, AutoShare, Hps30, DpsVsRole;
            public float AoeDps15, AoeDps30, AoeGain, AoeVsRole;
            public float TimeToDie, HpLeft, TtdVsRole;
            public bool Survived;
            public int DamageTaken, Mitigated, HealingReceived, DummyRevives;
            public string Error;

            public int Marks, MarksSmall;
            public float Dps30Alone, AoeDps30Alone, TimeToDieAlone;
            public float Dps30Small, AoeDps30Small, TimeToDieSmall;

            public Result(HeroDataSO hero) => Hero = hero;

            public void KeepAlone()
            {
                Dps30Alone = Dps30;
                AoeDps30Alone = AoeDps30;
                TimeToDieAlone = TimeToDie;
            }

            public void KeepSmallTeam()
            {
                MarksSmall = Marks;
                Dps30Small = Dps30;
                AoeDps30Small = AoeDps30;
                TimeToDieSmall = TimeToDie;
            }
        }
    }
}
