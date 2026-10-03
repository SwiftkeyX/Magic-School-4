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
    ///     AoE test    - a hero vs a cluster of 4 dummies. 
    ///     Tank test   - a hero vs a dummy, while a damage continuously hits a hero, until it dies or 30s pass.
    ///
    /// Split by job across partial files:
    ///     HeroBenchmark.cs         - settings, Run, the loop over every batch of heroes, Result
    ///     HeroBenchmark.Tests.cs   - the three tests
    ///     HeroBenchmark.Board.cs   - the lanes, spawning, clearing, keeping the dummies alive
    ///     HeroBenchmark.Speed.cs   - fixed steps, no frame cap, nothing drawn - and putting it all back
    ///     HeroBenchmark.Report.cs  - peer comparison, flags, the CSV in and out
    /// </summary>
    public partial class HeroBenchmark : MonoBehaviour
    {
        // =================================== test settings ===================================
        public const float ShortSeconds = 15f;
        public const float LongSeconds = 30f;

        // time step for .captureDeltaTime
        private const float TimeStep = 1f / 30f;

        // How many heroes are tested at once (read Lane in HeroBenchmark.Board.cs)
        private const int LaneCount = 8;
        private const float LaneSpacing = 10f;

        // the dummy is heal when the healthbar go below the threshold, so it never dies mid-test 
        private const float DummyRefillBelow = 0.5f;

        // pressure = the continuously direct damage apply to hero in the tank test 
        // 50 raw damage every 0.5s => 100 raw damage per second
        private const int PressurePerHit = 50;
        private const float PressureInterval = 0.5f;

        // a hero this far from its role's median is flagged
        private const float FlagBand = 0.2f;

        private static readonly HexNumber HeroHex = new HexNumber(TeamEnum.Blue, 3, 3);    // front of the player side
        private static readonly HexNumber DummyHex = new HexNumber(TeamEnum.Red, 0, 3);    // right next to it

        // dummy cluster position in AoE test
        private static readonly HexNumber[] ClusterHexes =
        {
            DummyHex,
            new HexNumber(TeamEnum.Red, 0, 2),
            new HexNumber(TeamEnum.Red, 1, 2),
            new HexNumber(TeamEnum.Red, 1, 3),
        };

        // =================================== run ===================================
        /// Run the benchmark test
        ///     dummy       =   a hero that have no skill. use to test specifically.
        ///     testHero    =   a hero to test
        ///     onDone      =   open CSV result
        public static void Run(HeroDataSO dummy, IReadOnlyList<HeroDataSO> testHero, Action<string> onDone)
        {
            var go = new GameObject(nameof(HeroBenchmark));
            HeroBenchmark benchmark = go.AddComponent<HeroBenchmark>();
            benchmark.StartCoroutine(benchmark.RunAll(dummy, testHero, onDone));
        }

        private IEnumerator RunAll(HeroDataSO dummy, IReadOnlyList<HeroDataSO> testHero, Action<string> onDone)
        {
            yield return WaitForGame();

            GameManager game = GameManager.Instance;
            float realStart = Time.realtimeSinceStartup;

            // if partial, test only the selected heroes. 
            // if not, test every hero.
            bool isPartialMode = testHero != null;
            List<HeroDataSO> testRoster = HeroesToTest(game, testHero);
            var results = new List<Result>();

            // uncapped the fps for fast testing, test, then back to normal fps
            GoFast();
            yield return TestEveryHero(game, testRoster, dummy, results);
            GoNormal();

            // compare everyone's numbers, write the CSV
            string path = Report(results, isPartialMode, realStart);

            onDone?.Invoke(path);
            Destroy(gameObject);
        }

        // let GameManager start the game first
        private static IEnumerator WaitForGame()
        {
            while (GameManager.Instance == null || GameManager.Instance.Phase != GamePhaseEnum.Preparation) yield return null;
            yield return null;
        }

        // get the selected heroes, or the whole roster
        private static List<HeroDataSO> HeroesToTest(GameManager game, IReadOnlyList<HeroDataSO> testHero)
            => (testHero ?? game.ShopRoster).Where(h => h != null).Distinct().ToList();

        // a batch = every lane run through the three tests together (read Lane in HeroBenchmark.Board.cs)
        private IEnumerator TestEveryHero(GameManager game, List<HeroDataSO> testRoster, HeroDataSO dummy, List<Result> results)
        {
            game.Board.SetBattleOn(false);
            List<Lane> lanes = CreateLanes(game, Mathf.Min(LaneCount, testRoster.Count));

            for (int first = 0; first < testRoster.Count; first += lanes.Count)
            {
                List<Lane> batch = lanes.Take(testRoster.Count - first).ToList();
                for (int i = 0; i < batch.Count; i++) batch[i].Assign(testRoster[first + i]);

                yield return TestBatch(game, batch, dummy);

                foreach (Lane lane in batch)
                {
                    results.Add(lane.Result);
                    LogProgress(lane.Result, results.Count, testRoster.Count);
                }
            }

            // a test is finished, destroy all lane
            DestroyLanes(lanes);
        }

        // run 3 test in order
        private IEnumerator TestBatch(GameManager game, List<Lane> batch, HeroDataSO dummy)
        {
            yield return DamageTest(game, batch, dummy);
            yield return AoeTest(game, batch, dummy);
            yield return TankTest(game, batch, dummy);
        }

        // log the test
        // e.g. dps, AoE dps, dies_at
        private static void LogProgress(Result result, int done, int total)
        {
            Debug.Log($"[HeroBenchmark] {done}/{total} {result.Hero.Name}: " +
                      $"{result.Dps30:0.0} DPS @30s, AoE {result.AoeDps30:0.0} (x{result.AoeGain:0.0}), " +
                      $"{(result.Survived ? "survived" : $"died at {result.TimeToDie:0.0}s")}");
        }

        // one hero's numbers across the three tests
        private class Result
        {
            public readonly HeroDataSO Hero;
            public float Dps15, Dps30, AutoShare, Hps30, DpsVsRole;
            public float AoeDps15, AoeDps30, AoeGain, AoeVsRole;      // AoeGain: AoE DPS / single-target DPS - x1.0 hits one thing
            public float TimeToDie, HpLeft, TtdVsRole;
            public bool Survived;
            public int DamageTaken, Mitigated, HealingReceived, DummyRevives;
            public string Error;

            public Result(HeroDataSO hero) => Hero = hero;
        }
    }
}
