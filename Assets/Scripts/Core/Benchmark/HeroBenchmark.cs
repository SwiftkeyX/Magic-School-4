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

        // the stand-in allies a hero's marks may be filled with: a team of 3 (stage 1) and a team of 5 (See TestEveryHero)
        private const int AlliesInTeamOf3 = 2;
        private const int AlliesInTeamOf5 = 4;

        // a hero this far from its role's median is flagged
        private const float FlagBand = 0.2f;

        private static readonly HexNumber HeroHex = new HexNumber(3, 3);    // left of the middle of the board
        private static readonly HexNumber DummyHex = new HexNumber(4, 3);   // right next to it

        // dummy cluster position in AoE test: 6 dummies, all next to (5, 2); only DummyHex is next to the hero
        private static readonly HexNumber[] ClusterHexes =
        {
            DummyHex,
            new HexNumber(4, 2),
            new HexNumber(5, 2),
            new HexNumber(5, 3),
            new HexNumber(6, 2),
            new HexNumber(6, 3),
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

        // Three passes, each a batch of lanes run through the three tests together (read Lane in HeroBenchmark.Board.cs):
        //      alone      - the hero by itself, as it always was: its floor
        //      team of 3  - again, with up to 2 of its own orange mark hexes filled (See MarkFill) - stage 1's team size
        //      team of 5  - again, with up to 4 filled - the team size the comps are designed around
        // A pass only runs the heroes it changes: no mark to fill, or no more than the last pass filled, keeps the numbers
        // it has. The team-of-5 numbers are the report's main ones - the role comparison and the flags use them.
        private IEnumerator TestEveryHero(GameManager game, List<HeroDataSO> testRoster, HeroDataSO dummy, List<Result> results)
        {
            game.Board.SetBattleOn(false);
            List<Lane> lanes = CreateLanes(game, Mathf.Min(LaneCount, testRoster.Count));
            BattleBoard board = lanes[0].Board;

            List<Result> everyone = testRoster.Select(hero => new Result(hero)).ToList();
            yield return TestPass(game, lanes, everyone, dummy, maxMarks: 0);
            foreach (Result result in everyone) result.KeepAlone();

            int Fill(Result r, int max) => MarkFill.Of(r.Hero, board, max).Count;
            List<Result> team3 = everyone.Where(r => r.Error == null && Fill(r, AlliesInTeamOf3) > 0).ToList();
            yield return TestPass(game, lanes, team3, dummy, AlliesInTeamOf3);
            foreach (Result result in everyone) result.KeepTeamOf3();

            List<Result> team5 = everyone.Where(r => r.Error == null && Fill(r, AlliesInTeamOf5) > Fill(r, AlliesInTeamOf3)).ToList();
            yield return TestPass(game, lanes, team5, dummy, AlliesInTeamOf5);

            for (int i = 0; i < everyone.Count; i++)
            {
                results.Add(everyone[i]);
                LogProgress(everyone[i], i + 1, everyone.Count);
            }

            // a test is finished, destroy all lane
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

        // run 3 test in order
        private IEnumerator TestBatch(GameManager game, List<Lane> batch, HeroDataSO dummy, int maxMarks)
        {
            yield return DamageTest(game, batch, dummy, maxMarks);
            yield return AoeTest(game, batch, dummy, maxMarks);
            yield return TankTest(game, batch, dummy, maxMarks);
        }

        // log the test
        // e.g. dps, AoE dps, dies_at
        private static void LogProgress(Result result, int done, int total)
        {
            Debug.Log($"[HeroBenchmark] {done}/{total} {result.Hero.Name}: " +
                      $"{result.Dps30:0.0} DPS @30s, AoE {result.AoeDps30:0.0} (x{result.AoeGain:0.0}), " +
                      $"{(result.Survived ? "survived" : $"died at {result.TimeToDie:0.0}s")}" +
                      (result.Marks > 0 ? $" - with {result.Marks} mark hexes filled; team of 3 {result.Dps30Team3:0.0}, " +
                                          $"alone {result.Dps30Alone:0.0} DPS" : ""));
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

            // each pass overwrites the numbers above, so the earlier ones are kept here (See TestEveryHero).
            // Marks = the mark hexes filled for the main (team of 5) numbers, 0 = none
            public int Marks, MarksTeam3;
            public float Dps30Alone, AoeDps30Alone, TimeToDieAlone;
            public float Dps30Team3, AoeDps30Team3, TimeToDieTeam3;

            public Result(HeroDataSO hero) => Hero = hero;

            public void KeepAlone()
            {
                Dps30Alone = Dps30;
                AoeDps30Alone = AoeDps30;
                TimeToDieAlone = TimeToDie;
            }

            public void KeepTeamOf3()
            {
                MarksTeam3 = Marks;
                Dps30Team3 = Dps30;
                AoeDps30Team3 = AoeDps30;
                TimeToDieTeam3 = TimeToDie;
            }
        }
    }
}
