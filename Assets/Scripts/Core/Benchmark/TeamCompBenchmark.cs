using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using MagicSchool.Contracts;
using MagicSchool.Combat.Align;
using MagicSchool.Combat.Heroes;
using MagicSchool.Combat.Placements;
using MagicSchool.Items;

namespace MagicSchool.Core.Benchmark
{
    /// <summary>
    /// Balancing helper for whole teams: every saved team comp fights every campaign stage (GameManager's stages),
    /// once with aligning and once without, a few times each - so a comp's numbers include what its heroes do for
    /// each other (aligning bonuses, Troll's Feed, Husk's Siphon, Shield Knight's heal link), which the hero benchmark
    /// cannot see, and the gap between the two shows what its marks are worth.
    ///
    /// A fight is a real fight, as CombatState runs it: the comp stands where it was saved (turned the way it was saved,
    /// wearing its items, items on hexes), the stage's enemies where the stage puts them, until one side is dead or
    /// TimeLimit passes (a timeout is not a win). Fights run LaneCount at once, each on its own hidden copy of the board,
    /// and a lane takes the next fight as soon as its own is over.
    ///
    ///     TeamCompBenchmark.cs         - settings, Run, the batches, one fight
    ///     TeamCompBenchmark.Report.cs  - averages per comp x stage x aligning, the CSV
    /// </summary>
    public partial class TeamCompBenchmark : MonoBehaviour
    {
        // =================================== settings ===================================
        // the same fight is run this many times: targets and some skills are random. Aligned is what is being balanced,
        // so it gets the most; not aligned is only the baseline it is measured against
        public const int RunsAligned = 20;
        public const int RunsNotAligned = 10;
        public static int RunsOf(bool aligning) => aligning ? RunsAligned : RunsNotAligned;

        // a stage counts as won at this share of its fights or more (14 of 20, 7 of 10).
        // The Sheet colours and counts by the same line: WIN_LINE in .claude/scripts/benchmark_import.py
        public const float WinLine = 0.7f;

        // a fight not over by then is a timeout - the player did not win it
        public const float TimeLimit = 90f;

        private const float TimeStep = 1f / 30f;
        // fights at once, each on its own board copy. Every frame has a fixed cost (the Editor's own loop) however many
        // fights it runs, so more lanes are cheaper per fight: 8 -> 32 took the run from 162s to 106s
        private const int LaneCount = 32;
        private const int ProgressEvery = 50;   // fights between progress logs
        private const float LaneSpacing = 10f;

        private readonly BenchmarkSpeed _speed = new BenchmarkSpeed();

        // =================================== run ===================================
        /// comps  = the team comps to test (BattlePlacementSOs with blue heroes)
        /// onDone = given the CSV's path
        public static void Run(IReadOnlyList<BattlePlacementSO> comps, Action<string> onDone)
        {
            var go = new GameObject(nameof(TeamCompBenchmark));
            TeamCompBenchmark benchmark = go.AddComponent<TeamCompBenchmark>();
            benchmark.StartCoroutine(benchmark.RunAll(comps, onDone));
        }

        private IEnumerator RunAll(IReadOnlyList<BattlePlacementSO> comps, Action<string> onDone)
        {
            while (GameManager.Instance == null || GameManager.Instance.Phase != GamePhaseEnum.Preparation) yield return null;
            yield return null;

            GameManager game = GameManager.Instance;
            float realStart = Time.realtimeSinceStartup;

            List<Fight> fights = PlanFights(game, comps);
            List<Fight> runnable = fights.Where(f => f.Error == null).ToList();

            _speed.GoFast(TimeStep);
            game.Board.SetBattleOn(false);
            game.Recorder.BeginRound();     // fresh books; then each fight starts a round of its own (See Begin)

            var lanes = new List<Lane>();
            for (int i = 0; i < Mathf.Min(LaneCount, runnable.Count); i++)
                lanes.Add(new Lane(HeroBenchmark.CopyBoard(game, i, LaneSpacing, out HeroSeed seed), seed));

            // Every lane goes at its own pace: one that finishes takes the next fight at once rather than waiting for
            // the slowest of a batch - a single 90s timeout used to hold up every other lane.
            var queue = new Queue<Fight>(runnable);
            int finished = 0, logged = 0;
            while (queue.Count > 0 || lanes.Any(l => l.Step != LaneStep.Idle))
            {
                foreach (Lane lane in lanes)
                {
                    if (Advance(game, lane)) finished++;
                    if (lane.Step == LaneStep.Idle && queue.Count > 0) Take(lane, queue.Dequeue());
                }

                if (finished - logged >= ProgressEvery || (finished == runnable.Count && logged != finished))
                {
                    logged = finished;
                    Debug.Log($"[TeamCompBenchmark] {finished}/{runnable.Count} fights");
                }
                yield return null;
            }

            foreach (Lane lane in lanes)
            {
                Clear(lane);
                Destroy(lane.Board.gameObject);
            }

            _speed.GoNormal();

            string path = Report(fights, realStart);
            onDone?.Invoke(path);
            Destroy(gameObject);
        }

        private void OnDestroy() => _speed.GoNormal();

        // every comp x every stage x aligning on and off x RunsOf(aligning).
        // A comp saved on another board than the stage's cannot stand there: its hex numbers mean nothing on it
        private static List<Fight> PlanFights(GameManager game, IReadOnlyList<BattlePlacementSO> comps)
        {
            var fights = new List<Fight>();

            foreach (BattlePlacementSO comp in comps.Where(c => c != null).Distinct())
            {
                for (int stage = 0; stage < game.StageCount; stage++)
                {
                    BattlePlacementSO setup = game.GetStage(stage);

                    foreach (bool aligning in new[] { true, false })
                    {
                        for (int run = 0; run < RunsOf(aligning); run++)
                        {
                            var fight = new Fight(comp, setup, stage + 1, aligning);
                            if (setup == null) fight.Error = "no stage";
                            else if (comp.BoardLayout != null && setup.BoardLayout != null && comp.BoardLayout != setup.BoardLayout)
                                fight.Error = "comp saved on another board";
                            else if (!comp.HeroesPlacement.Any(p => p.team == TeamEnum.Blue && p.dataSO != null))
                                fight.Error = "comp has no hero";

                            fights.Add(fight);

                            // an unrunnable fight is unrunnable every time: report it once
                            if (fight.Error != null) break;
                        }
                    }
                }
            }

            return fights;
        }

        // =================================== one lane ===================================
        // A fight takes a lane through: Take (clear the last fight, set the layout) -> a frame, so the old hexes are
        // destroyed -> Spawn -> a frame -> Begin -> Fighting, checked every frame until it is over -> Idle.

        private static void Take(Lane lane, Fight fight)
        {
            lane.Fight = fight;
            Clear(lane);
            lane.Board.SetLayout(fight.Stage.BoardLayout != null ? fight.Stage.BoardLayout : fight.Comp.BoardLayout);
            lane.Step = LaneStep.Spawning;
        }

        // one frame of a lane; true when its fight just ended (or could not start)
        private static bool Advance(GameManager game, Lane lane)
        {
            switch (lane.Step)
            {
                case LaneStep.Spawning:
                    Spawn(lane);
                    lane.Step = lane.Fight.Error == null ? LaneStep.Starting : LaneStep.Idle;
                    return lane.Fight.Error != null;

                case LaneStep.Starting:
                    Begin(game, lane);
                    lane.Step = LaneStep.Fighting;
                    return false;

                case LaneStep.Fighting:
                    CheckOver(game, lane, Time.time - lane.StartTime);
                    if (!lane.IsOver) return false;
                    lane.Step = LaneStep.Idle;
                    return true;

                default:
                    return false;
            }
        }

        // as CombatState.OnEnter does it
        private static void Begin(GameManager game, Lane lane)
        {
            lane.Board.SetBattleOn(true);
            if (lane.Fight.Aligning) new AlignBonuses().Refresh(lane.Board, TeamEnum.Blue);

            foreach (Hero hero in lane.Board.HeroesOnBoard.OfType<Hero>().ToList())
            {
                if (hero.IsAlive) hero.TriggerOnCombatStart();
            }

            // this lane's books and clock only - the other lanes are mid-fight
            game.Recorder.BeginRoundFor(lane.Board.HeroesOnBoard.OfType<IEffectable>());
            lane.StartTime = Time.time;
        }

        // the stage's enemies, then the comp's heroes turned and dressed the way it was saved, then the items on hexes
        private static void Spawn(Lane lane)
        {
            Fight fight = lane.Fight;
            BattleBoard board = lane.Board;
            board.DeployZone.Set(fight.Stage.DeployZone);

            lane.Seed.SwitchSeed(fight.Stage);
            lane.Seed.SpawnTeamOnBoard(TeamEnum.Red);
            lane.Seed.SwitchSeed(fight.Comp);
            lane.Seed.SpawnTeamOnBoard(TeamEnum.Blue);

            foreach (HeroPlacement placement in fight.Comp.HeroesPlacement)
            {
                if (placement.team != TeamEnum.Blue || placement.dataSO == null) continue;
                if (!board.Hexs.TryGetValue(placement.hexPlacement, out Hex hex) || !(board.WhoReservedThisHex(hex) is Hero hero)) continue;

                hero.SetAlignMarkPattern(placement.alignPattern);
                lane.Team.Add(hero);

                if (placement.item is ItemDataSO itemData)
                {
                    Item worn = Item.Spawn(itemData, hex.transform.position);
                    if (worn != null && !hero.TryWear(worn)) Destroy(worn.gameObject);
                }
            }

            foreach (ItemPlacement lying in fight.Comp.ItemsOnHexes)
            {
                if (!(lying.item is ItemDataSO itemData)) continue;

                Item item = Item.Spawn(itemData, Vector3.zero);
                if (item == null) continue;

                bool isLaid = board.Hexs.TryGetValue(lying.hexPlacement, out Hex hex)
                              && board.WhoReservedThisHex(hex) == null
                              && board.Items.Place(item, hex);
                if (!isLaid) Destroy(item.gameObject);
            }

            bool hasEnemy = board.HeroesOnBoard.Any(h => h != null && h.Team == TeamEnum.Red);
            if (lane.Team.Count == 0) fight.Error = "comp failed to spawn";
            else if (!hasEnemy) fight.Error = "stage has no enemy";
        }

        // over when a side has nobody standing (summons count, as in CombatState), or at the time limit
        private static void CheckOver(GameManager game, Lane lane, float seconds)
        {
            var standing = lane.Board.HeroesOnBoard.Where(h => h != null && (h.StateType != HeroStateEnum.Dead || (h is IRevivable r && r.IsReviving))).ToList();
            bool blueStanding = standing.Any(h => h.Team == TeamEnum.Blue);
            bool redStanding = standing.Any(h => h.Team == TeamEnum.Red);
            bool timeUp = seconds >= TimeLimit;
            if (blueStanding && redStanding && !timeUp) return;

            Fight fight = lane.Fight;
            fight.Seconds = seconds;
            fight.Outcome = blueStanding && redStanding ? Outcome.Timeout
                          : blueStanding ? Outcome.Win
                          : redStanding ? Outcome.Lose
                          : Outcome.Draw;

            foreach (Hero hero in lane.Team)
            {
                if (hero == null) continue;

                ICombatRecord record = game.Recorder.RoundOf(hero);
                bool alive = hero.StateType != HeroStateEnum.Dead;
                fight.Heroes.Add(new HeroLine
                {
                    Name = hero.Data.Name,
                    Damage = record.DamageDealt,
                    DamageTaken = record.DamageTaken,
                    Healing = record.HealingDone,
                    Survived = alive,
                    DiedAt = alive ? seconds : Mathf.Min(record.SecondsAlive, seconds),
                    HpLeft = alive && hero.MaxHP > 0 ? (float)hero.CurrentHP / hero.MaxHP : 0f,
                });
            }

            lane.Board.SetBattleOn(false);
            lane.IsOver = true;
        }

        // every hero, summons included, and every item on the board or worn
        private static void Clear(Lane lane)
        {
            BattleBoard board = lane.Board;
            if (board == null) return;

            board.SetBattleOn(false);
            foreach (Hero hero in board.HeroesOnBoard.OfType<Hero>().Where(h => h != null).ToList())
            {
                foreach (IEquipment item in hero.WornItems.ToList())
                {
                    if (item is Component worn && worn != null) Destroy(worn.gameObject);
                }
            }
            foreach (IEquipment item in board.Items.TakeAll())
            {
                if (item is Component lying && lying != null) Destroy(lying.gameObject);
            }

            board.ClearTeam(TeamEnum.Blue);
            board.ClearTeam(TeamEnum.Red);
            lane.Team.Clear();
            lane.IsOver = false;
        }

        // =================================== data ===================================
        private class Lane
        {
            public readonly BattleBoard Board;
            public readonly HeroSeed Seed;
            public readonly List<Hero> Team = new List<Hero>();     // the comp's heroes - not their summons
            public Fight Fight;
            public LaneStep Step = LaneStep.Idle;
            public float StartTime;
            public bool IsOver;

            public Lane(BattleBoard board, HeroSeed seed)
            {
                Board = board;
                Seed = seed;
            }
        }

        private enum Outcome { None, Win, Lose, Timeout, Draw }

        private enum LaneStep { Idle, Spawning, Starting, Fighting }

        // one fight's numbers
        private class Fight
        {
            public readonly BattlePlacementSO Comp, Stage;
            public readonly int StageNumber;
            public readonly bool Aligning;
            public Outcome Outcome;
            public float Seconds;
            public readonly List<HeroLine> Heroes = new List<HeroLine>();
            public string Error;

            public Fight(BattlePlacementSO comp, BattlePlacementSO stage, int stageNumber, bool aligning)
            {
                Comp = comp;
                Stage = stage;
                StageNumber = stageNumber;
                Aligning = aligning;
            }
        }

        private class HeroLine
        {
            public string Name;
            public int Damage, DamageTaken, Healing;
            public bool Survived;
            public float DiedAt, HpLeft;
        }
    }
}
