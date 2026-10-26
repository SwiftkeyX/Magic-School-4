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
        private IEnumerator DamageTest(GameManager game, List<Lane> lanes, HeroDataSO dummy, int maxMarks)
        {
            yield return Spawn(lanes, dummy, maxMarks, DummyHex);

            List<Lane> live = lanes.Where(l => l.Result.Error == null).ToList();
            BeginFight(game, live);

            float start = Time.time;
            bool sampledShort = false;
            bool sampledLong = false;

            float totalTime = 0f;
            while ((sampledShort && sampledLong) == false)
            {
                foreach (Lane lane in live) KeepAlive(lane);

                if (!sampledShort && totalTime >= ShortSeconds)
                {
                    foreach (Lane lane in live)
                    {
                        lane.Result.Dps15 = game.Recorder.RoundOf(lane.Hero).DamageDealt / totalTime;
                    }

                    sampledShort = true;
                }

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

        private IEnumerator AoeTest(GameManager game, List<Lane> lanes, HeroDataSO dummy, int maxMarks)
        {
            yield return Spawn(lanes, dummy, maxMarks, ClusterHexes);

            List<Lane> live = lanes.Where(l => l.Result.Error == null).ToList();
            BeginFight(game, live);

            float start = Time.time;
            bool sampledShort = false;
            bool sampledLong = false;

            float totalTime = 0f;
            while ((sampledShort && sampledLong) == false)
            {
                foreach (Lane lane in live) KeepAlive(lane);

                if (!sampledShort && totalTime >= ShortSeconds)
                {
                    foreach (Lane lane in live)
                    {
                        lane.Result.AoeDps15 = game.Recorder.RoundOf(lane.Hero).DamageDealt / totalTime;
                    }

                    sampledShort = true;
                }

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

        private IEnumerator TankTest(GameManager game, List<Lane> lanes, HeroDataSO dummy, int maxMarks)
        {
            yield return Spawn(lanes, dummy, maxMarks, DummyHex);

            List<Lane> live = lanes.Where(l => l.Result.Error == null).ToList();
            BeginFight(game, live);

            float start = Time.time;
            bool sampled = false;

            foreach (Lane lane in live) lane.NextHit = PressureInterval;

            float totalTime = 0f;
            while (sampled == false)
            {
                foreach (Lane lane in live)
                {
                    if (lane.Hero.CurrentHP <= 0) continue;

                    KeepAlive(lane);

                    while (totalTime >= lane.NextHit && lane.Hero.CurrentHP > 0)
                    {
                        lane.Hero.TakeDamage(PressurePerHit, lane.Dummy, DamageKindEnum.AutoAttack);
                        lane.NextHit += PressureInterval;
                    }
                }

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
