using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using MagicSchool.Contracts;
using MagicSchool.Combat.Heroes;

namespace MagicSchool.Core.Benchmark
{
    // the benchmark was separated into 3 tests. (read HeroBenchmark.cs)
    public partial class HeroBenchmark
    {
        // Damage test = a hero vs a dummy, for 30s. DPS at 15s and 30s
        private IEnumerator DamageTest(GameManager game, List<Lane> lanes, HeroDataSO dummy)
        {
            // spawn a tested hero and dummy
            yield return Spawn(lanes, dummy, DummyHex);

            // start battle on every lane
            List<Lane> live = lanes.Where(l => l.Result.Error == null).ToList();
            BeginFight(game, live);

            float start = Time.time;
            bool sampledShort = false;
            bool sampledLong = false;

            // testing until the record is finished
            float totalTime = 0f;
            while ((sampledShort && sampledLong) == false)
            {
                // don't let dummy die
                foreach (Lane lane in live) KeepAlive(lane);

                // record at 15 sec mark
                if (!sampledShort && totalTime >= ShortSeconds)
                {
                    foreach (Lane lane in live)
                    {
                        lane.Result.Dps15 = game.Recorder.RoundOf(lane.Hero).DamageDealt / totalTime;
                    }

                    sampledShort = true;
                }

                // record another time at 30 sec mark
                if (!sampledLong && totalTime >= LongSeconds)
                {
                    foreach (Lane lane in live)
                    {
                        ICombatRecord record = game.Recorder.RoundOf(lane.Hero);
                        lane.Result.Dps30 = record.DamageDealt / totalTime;
                        lane.Result.Hps30 = record.HealingDone / totalTime;
                        lane.Result.AutoShare = record.DamageDealt > 0 ? (float)record.AutoAttackDamage / record.DamageDealt : 0f;
                    }

                    sampledLong = true;
                }

                totalTime = Time.time - start;
                yield return null;
            }

            EndFight(lanes);
        }

        // AoE test = a hero vs a cluster of 4 dummies. 
        private IEnumerator AoeTest(GameManager game, List<Lane> lanes, HeroDataSO dummy)
        {
            // spawn a tested hero and a cluster of dummies
            yield return Spawn(lanes, dummy, ClusterHexes);

            // start battle on every lane
            List<Lane> live = lanes.Where(l => l.Result.Error == null).ToList();
            BeginFight(game, live);

            float start = Time.time;
            bool sampledShort = false;
            bool sampledLong = false;

            // testing until the record is finished
            float totalTime = 0f;
            while ((sampledShort && sampledLong) == false)
            {
                // don't let any dummy die
                foreach (Lane lane in live) KeepAlive(lane);

                // record at 15 sec mark
                if (!sampledShort && totalTime >= ShortSeconds)
                {
                    foreach (Lane lane in live)
                    {
                        lane.Result.AoeDps15 = game.Recorder.RoundOf(lane.Hero).DamageDealt / totalTime;
                    }

                    sampledShort = true;
                }

                // record another time at 30 sec mark 
                if (!sampledLong && totalTime >= LongSeconds)
                {
                    foreach (Lane lane in live)
                    {
                        lane.Result.AoeDps30 = game.Recorder.RoundOf(lane.Hero).DamageDealt / totalTime;
                        lane.Result.AoeGain = lane.Result.Dps30 > 0f ? lane.Result.AoeDps30 / lane.Result.Dps30 : 0f;
                    }

                    sampledLong = true;
                }

                totalTime = Time.time - start;
                yield return null;
            }

            EndFight(lanes);
        }

        // tank test = a hero vs a dummy, while a pressure continuously damage a tested hero, until it dies or 30s pass.
        private IEnumerator TankTest(GameManager game, List<Lane> lanes, HeroDataSO dummy)
        {
            // spawn a tested hero and dummy
            yield return Spawn(lanes, dummy, DummyHex);

            // start battle on every lane
            List<Lane> live = lanes.Where(l => l.Result.Error == null).ToList();
            BeginFight(game, live);

            float start = Time.time;
            bool sampled = false;

            // ...
            foreach (Lane lane in live) lane.NextHit = PressureInterval;

            // testing until the record is finished - every hero has died, or 30 sec has passed
            float totalTime = 0f;
            while (sampled == false)
            {
                foreach (Lane lane in live)
                {
                    // tested hero is dead, continue 
                    if (lane.Hero.CurrentHP <= 0) continue;

                    // don't let dummy die
                    KeepAlive(lane);

                    // pressure hit the tested hero
                    while (totalTime >= lane.NextHit && lane.Hero.CurrentHP > 0)
                    {
                        lane.Hero.TakeDamage(PressurePerHit, lane.Dummy, DamageKindEnum.AutoAttack);
                        lane.NextHit += PressureInterval;
                    }
                }

                // record once every hero is dead, or at 30 sec mark
                bool allDead = live.All(l => l.Hero.CurrentHP <= 0);
                if (allDead || totalTime >= LongSeconds)
                {
                    foreach (Lane lane in live)
                    {
                        ICombatRecord record = game.Recorder.RoundOf(lane.Hero);
                        Result result = lane.Result;
                        result.Survived = lane.Hero.CurrentHP > 0;
                        result.TimeToDie = result.Survived ? totalTime : record.SecondsAlive;
                        result.HpLeft = lane.Hero.MaxHP > 0 ? (float)lane.Hero.CurrentHP / lane.Hero.MaxHP : 0f;
                        result.DamageTaken = record.DamageTaken;
                        result.Mitigated = record.DamageMitigated;
                        result.HealingReceived = record.HealingReceived;
                    }

                    sampled = true;
                }

                totalTime = Time.time - start;
                yield return null;
            }

            EndFight(lanes);
        }
    }
}
