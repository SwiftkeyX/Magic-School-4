using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using MagicSchool.Contracts;
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
            public float NextHit;                   // specific to tank test. pressure hit time

            public Lane(BattleBoard board, HeroSeed seed)
            {
                Board = board;
                Seed = seed;
            }

            // assign hero to this lane
            public void Assign(HeroDataSO hero) => Result = new Result(hero);

            public Hero Dummy => Dummies.Count > 0 ? Dummies[0] : null;
        }

        // Create lane. Assign the board, seeder
        private static List<Lane> CreateLanes(GameManager game, int count)
        {
            var lanes = new List<Lane>();
            Transform original = game.Board.transform;

            for (int i = 0; i < count; i++)
            {
                Vector3 position = original.position + new Vector3(0f, -LaneSpacing * (i + 1), 0f);
                GameObject copy = Instantiate(original.gameObject, position, original.rotation);
                copy.name = $"BenchBoard {i + 1}";

                BattleBoard board = copy.GetComponent<BattleBoard>();
                var seed = new HeroSeed(null, board);

                // its own spawner: 
                // but the same recorder, so every lane's numbers land in one place
                new HeroSpawner(new HeroMover(), null, seed, game.TemplateActions, game.Recorder);

                lanes.Add(new Lane(board, seed));
            }

            return lanes;
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

        // ================================= spawning =================================
        // every lane spawn
        //  1) a hero on HeroHex 
        //  2) a dummy on each of dummyHexes
        private static IEnumerator Spawn(List<Lane> lanes, HeroDataSO dummy, params HexNumber[] dummyHexes)
        {
            // reset the board, hero, dummy
            foreach (Lane lane in lanes)
            {
                lane.Board.SetBattleOn(false);
                ClearBoard(lane.Board);
                lane.Hero = null;
                lane.Dummies.Clear();
            }
            yield return null;  // wait 1 frame

            // seed new hero, dummy
            foreach (Lane lane in lanes)
            {
                List<HeroPlacement> placements = new List<HeroPlacement>();
                placements.Add(new HeroPlacement(lane.Result.Hero, HeroHex));
                foreach (HexNumber hex in dummyHexes) placements.Add(new HeroPlacement(dummy, hex));

                lane.Seed.SwitchSeed(BattlePlacementSO.CreateRuntime(placements));
                lane.Seed.SpawnTeamOnBoard(TeamEnum.Blue);
                lane.Seed.SpawnTeamOnBoard(TeamEnum.Red);
            }
            yield return null; // wait 1 frame

            // find reference to tested hero and dummy
            foreach (Lane lane in lanes)
            {
                lane.Hero = lane.Board.HeroesOnBoard.OfType<Hero>().FirstOrDefault(h => h != null && h.Team == TeamEnum.Blue);
                lane.Dummies.AddRange(lane.Board.HeroesOnBoard.OfType<Hero>().Where(h => h != null && h.Team == TeamEnum.Red));

                if (lane.Hero == null || lane.Dummies.Count != dummyHexes.Length) lane.Result.Error = "spawn failed";
            }
        }

        private static void ClearBoard(BattleBoard board)
        {
            board.ClearTeam(TeamEnum.Blue);
            board.ClearTeam(TeamEnum.Red);
        }

        // This start battle on every lane.
        // Mimic what CombatState.OnEnter() does for a real fight.
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

        // counterpart to BeginFight()
        private static void EndFight(List<Lane> lanes)
        {
            foreach (Lane lane in lanes) lane.Board.SetBattleOn(false);
        }

        // Keep the dummy alive forever. In test, dummy shouldn't die. 
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
