using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using MagicSchool.Contracts;
using MagicSchool.Combat.Align;
using MagicSchool.Combat.Heroes;
using MagicSchool.Combat.Placements;

namespace MagicSchool.Core.Benchmark
{
    public partial class HeroBenchmark
    {
        // ================================= Lane =================================
        /// <summary>
        /// Lane is its the copy of the real BattleBoard. We seed hero on a lane to run a benchmark test.
        /// Several lane can be spawned at once to quickly test the HeroBenchmark.cs
        /// </summary>
        private class Lane
        {
            public readonly BattleBoard Board;
            public readonly HeroSeed Seed;

            public Result Result;
            public Hero Hero;
            public readonly List<Hero> Dummies = new List<Hero>();
            public float NextHit;

            public Lane(BattleBoard board, HeroSeed seed)
            {
                Board = board;
                Seed = seed;
            }

            public Hero Dummy => Dummies.Count > 0 ? Dummies[0] : null;
        }

        private static List<Lane> CreateLanes(GameManager game, int count)
        {
            var lanes = new List<Lane>();

            for (int i = 0; i < count; i++)
            {
                BattleBoard board = CopyBoard(game, i, LaneSpacing, out HeroSeed seed);
                lanes.Add(new Lane(board, seed));
            }

            return lanes;
        }

        internal static BattleBoard CopyBoard(GameManager game, int index, float spacing, out HeroSeed seed)
        {
            Transform original = game.Board.transform;
            Vector3 position = original.position + new Vector3(0f, -spacing * (index + 1), 0f);
            GameObject copy = Instantiate(original.gameObject, position, original.rotation);
            copy.name = $"BenchBoard {index + 1}";

            BattleBoard board = copy.GetComponent<BattleBoard>();
            seed = new HeroSeed(null, board);

            new HeroSpawner(new HeroMover(), game.TemplateActions, game.Recorder, seed: seed);
            return board;
        }

        private static void DestroyLanes(List<Lane> lanes)
        {
            foreach (Lane lane in lanes)
            {
                if (lane.Board == null) continue;

                lane.Board.SetBattleOn(false);
                ClearBoard(lane.Board);
                Destroy(lane.Board.gameObject);
            }
        }

        private static IEnumerator Spawn(List<Lane> lanes, HeroDataSO dummy, int maxMarks, params HexNumber[] dummyHexes)
        {
            bool marked = maxMarks > 0;
            foreach (Lane lane in lanes)
            {
                lane.Board.SetBattleOn(false);
                ClearBoard(lane.Board);
                lane.Hero = null;
                lane.Dummies.Clear();
            }
            yield return null;

            foreach (Lane lane in lanes)
            {
                List<HeroPlacement> placements = new List<HeroPlacement>();
                placements.Add(new HeroPlacement(lane.Result.Hero, TeamEnum.Blue, HeroHex));
                if (marked)
                {
                    foreach (HexNumber hex in MarkFill.Of(lane.Result.Hero, lane.Board, maxMarks).StandIns)
                        placements.Add(new HeroPlacement(dummy, TeamEnum.Blue, hex));
                }
                foreach (HexNumber hex in dummyHexes) placements.Add(new HeroPlacement(dummy, TeamEnum.Red, hex));

                lane.Seed.SwitchSeed(BattlePlacementSO.CreateRuntime(placements));
                lane.Seed.SpawnTeamOnBoard(TeamEnum.Blue);
                if (!marked) lane.Seed.SpawnTeamOnBoard(TeamEnum.Red);
            }
            yield return null;

            if (marked)
            {
                foreach (Lane lane in lanes)
                {
                    FillMarks(lane, maxMarks);
                    lane.Seed.SpawnTeamOnBoard(TeamEnum.Red);
                }
                yield return null;
            }

            foreach (Lane lane in lanes)
            {
                lane.Hero = lane.Board.HeroesOnBoard.OfType<Hero>().FirstOrDefault(h => h != null && h.Team == TeamEnum.Blue);
                lane.Dummies.AddRange(lane.Board.HeroesOnBoard.OfType<Hero>().Where(h => h != null && h.Team == TeamEnum.Red));

                if (lane.Hero == null || lane.Dummies.Count != dummyHexes.Length) lane.Result.Error = "spawn failed";
            }
        }

        // ================================= marks =================================
        // The hero alone can never have its marks filled, so on paper it is weaker than in a fight.
        // Marked, the hero gets everything its OWN orange marks pay it (pattern 1, as a bought hero starts):
        //      a self bonus per ally         - a stand-in ally on each of those hexes
        //      a self bonus per empty hex    - those hexes put in the deploy zone and left empty (Centaur)
        // then aligning is given as a fight start gives it, and the stand-ins leave before the fight: the bonuses stay
        // for the rest of it (as they do in a real fight, fixed when it starts), and nobody is in the way, drawing
        // the dummies' attacks or adding damage. What needs allies to DO something - a heal link, Troll's Feed,
        // Reaper's Reap, the green bonus an ally gets - is the team comp benchmark's to measure.
        // At most `max` hexes are paid, the first ones in the order the marks were authored: e.g. a team of 5 has 4 allies to
        // spare, and the hexes an empty-hex mark counts are hexes those allies are not standing on.
        private readonly struct MarkFill
        {
            public readonly List<HexNumber> StandIns;
            public readonly List<HexNumber> LeftEmpty;

            private MarkFill(List<HexNumber> standIns, List<HexNumber> leftEmpty)
            {
                StandIns = standIns;
                LeftEmpty = leftEmpty;
            }

            public int Count => StandIns.Count + LeftEmpty.Count;

            public static MarkFill Of(HeroDataSO hero, BattleBoard board, int max)
            {
                var standIns = new List<HexNumber>();
                var leftEmpty = new List<HexNumber>();
                if (hero == null || max <= 0 || !board.Hexs.TryGetValue(HeroHex, out Hex standingOn)) return new MarkFill(standIns, leftEmpty);

                IReadOnlyList<AlignMark> marks = hero.AlignMarksOf(0);
                for (int i = 0; i < marks.Count; i++)
                {
                    AlignMark mark = marks[i];
                    if (AligningHex.ColourOf(marks, i) != AligningHex.Orange || !mark.HasSelfBonus) continue;

                    foreach (Hex hex in mark.GetMarkedHexes(standingOn))
                    {
                        if (standIns.Count + leftEmpty.Count >= max) return new MarkFill(standIns, leftEmpty);
                        if (hex == standingOn || standIns.Contains(hex.Number) || leftEmpty.Contains(hex.Number)) continue;

                        (mark.selfWhenEmpty ? leftEmpty : standIns).Add(hex.Number);
                    }
                }

                return new MarkFill(standIns, leftEmpty);
            }
        }

        private static void FillMarks(Lane lane, int maxMarks)
        {
            BattleBoard board = lane.Board;
            MarkFill fill = MarkFill.Of(lane.Result.Hero, board, maxMarks);
            Hero hero = board.HeroesOnBoard.OfType<Hero>().FirstOrDefault(h => h != null && h.Team == TeamEnum.Blue && h.Data == lane.Result.Hero);
            if (hero == null) return;

            board.DeployZone.Set(fill.LeftEmpty);
            new AlignBonuses().Refresh(board, TeamEnum.Blue);
            board.DeployZone.Set(new List<HexNumber>());

            foreach (Hero standIn in board.HeroesOnBoard.OfType<Hero>().Where(h => h != null && h != hero && h.Team == TeamEnum.Blue).ToList())
                board.Remove(standIn);
            AlignBonuses.ClearSpecialMarks(hero);

            lane.Result.Marks = fill.Count;
        }

        private static void ClearBoard(BattleBoard board)
        {
            board.ClearTeam(TeamEnum.Blue);
            board.ClearTeam(TeamEnum.Red);
        }

        private static void BeginFight(GameManager game, List<Lane> lanes)
        {
            game.Recorder.BeginRound();

            foreach (Lane lane in lanes)
            {
                if (lane.Result.Error != null) continue;

                lane.Board.SetBattleOn(true);
                lane.Hero.TriggerOnCombatStart();
                foreach (Hero dummy in lane.Dummies) dummy.TriggerOnCombatStart();
            }
        }

        private static void EndFight(List<Lane> lanes)
        {
            foreach (Lane lane in lanes) lane.Board.SetBattleOn(false);
        }

        private static void KeepAlive(Lane lane)
        {
            foreach (Hero dummy in lane.Dummies)
            {
                if (dummy == null) continue;

                if (dummy.StateType == HeroStateEnum.Dead)
                {
                    lane.Result.DummyRevives++;
                    dummy.ResetForNewStage();
                    continue;
                }

                if (dummy.CurrentHP < dummy.MaxHP * DummyRefillBelow) dummy.Heal(dummy.MaxHP, null);
            }
        }
    }
}
