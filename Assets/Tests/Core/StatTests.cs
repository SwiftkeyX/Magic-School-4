using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using MagicSchool.Contracts;
using MagicSchool.Combat.Heroes;
using MagicSchool.Combat.Heroes.Stats;
using MagicSchool.Modifiers;
using MagicSchool.StatScaling;

namespace MagicSchool.Core.Tests
{
    // Stat and ModifierResolver together: how current HP follows max HP when modifiers come and go
    public class StatTests
    {
        private HeroDataSO _data;

        [SetUp] public void SetUp() => _data = ScriptableObject.CreateInstance<HeroDataSO>();   // 500 HP by default
        [TearDown] public void TearDown() => Object.DestroyImmediate(_data);

        private static CustomModifier BonusHP(float amount, float duration, bool keepsHealth = false)
            => new CustomModifier(duration,
                new List<IModifier> { new StatModifier(ModifierEnum.BonusHP, new List<StatRatio> { amount }) },
                isStack: true, keepsHealth: keepsHealth);

        [Test]
        public void StarAndLevelRaiseBaseStats()
        {
            Assert.AreEqual(500 * 1.8f, new Stat(_data, star: 2).GetBaseStat(StatEnum.MaxHP), 0.01f);
            Assert.AreEqual(500 * 2f, new Stat(_data, level: 11).GetBaseStat(StatEnum.MaxHP), 0.01f);
        }

        [Test]
        public void ModifierRaisesStatAndExpires()
        {
            var stat = new Stat(_data);
            stat.AddModifier(BonusHP(500f, 1f), 1f, null, null);
            Assert.AreEqual(1000, stat.MaxHP);
            Assert.AreEqual(1000, stat.CurrentHP);      // current HP follows max HP up

            stat.TickModifiers(2f);
            Assert.AreEqual(500, stat.MaxHP);
            Assert.AreEqual(500, stat.CurrentHP);       // and back down
        }

        [Test]
        public void LosingMaxHPNeverKillsAndIsOwedBack()
        {
            var stat = new Stat(_data);
            stat.SetCurrentHP(20);
            stat.AddModifier(BonusHP(-80f, 1f), 1f, null, null);
            Assert.AreEqual(1, stat.CurrentHP);

            stat.TickModifiers(2f);
            Assert.AreEqual(20, stat.CurrentHP);        // 20, not 81
        }

        [Test]
        public void KeepsHealthModifierLeavesCurrentHPWhenItExpires()
        {
            var stat = new Stat(_data);
            stat.SetCurrentHP(400);
            stat.AddModifier(BonusHP(200f, 1f, keepsHealth: true), 1f, null, null);
            Assert.AreEqual(600, stat.CurrentHP);

            stat.TickModifiers(2f);
            Assert.AreEqual(500, stat.MaxHP);
            Assert.AreEqual(500, stat.CurrentHP);       // kept, but capped at the new max
        }
    }
}
