using NUnit.Framework;
using UnityEngine;
using MagicSchool.Contracts;
using MagicSchool.Combat.Heroes;
using MagicSchool.Economy;
using MagicSchool.StatScaling;

namespace MagicSchool.Core.Tests
{
    public class CombatMathTests
    {
        [Test]
        public void DefenseDividesDamage()
        {
            DamageOutcome hit = CombatMath.ResolveDamage(200, 100, 0f, 1000, 0, out int absorbed);
            Assert.AreEqual(100, hit.Landed);           // 200 / (1 + 100/100)
            Assert.AreEqual(100, hit.Mitigated);
            Assert.AreEqual(900, hit.NewHP);
            Assert.AreEqual(0, absorbed);
        }

        [Test]
        public void DamageReductionAppliesAfterDefense()
        {
            DamageOutcome hit = CombatMath.ResolveDamage(200, 100, 50f, 1000, 0, out _);
            Assert.AreEqual(50, hit.Landed);
        }

        [Test]
        public void ShieldAbsorbsBeforeHP()
        {
            DamageOutcome hit = CombatMath.ResolveDamage(100, 0, 0f, 1000, 30, out int absorbed);
            Assert.AreEqual(30, absorbed);
            Assert.AreEqual(70, hit.Landed);
        }

        [Test]
        public void HPStopsAtZeroAndTheRestIsOverkill()
        {
            DamageOutcome hit = CombatMath.ResolveDamage(100, 0, 0f, 40, 0, out _);
            Assert.AreEqual(0, hit.NewHP);
            Assert.AreEqual(40, hit.Landed);
            Assert.AreEqual(60, hit.Overkill);
        }

        [Test]
        public void WoundHalvesHealingAndMaxHPCapsIt()
        {
            HealOutcome wounded = CombatMath.ResolveHeal(100f, true, 500, 1000);
            Assert.AreEqual(50, wounded.Healed);

            HealOutcome capped = CombatMath.ResolveHeal(100f, false, 950, 1000);
            Assert.AreEqual(1000, capped.NewHP);
            Assert.AreEqual(50, capped.Healed);
        }

        [TestCase(100, 40f, 15f, ExpectedResult = 45)]
        [TestCase(20, 0f, 15f, ExpectedResult = 5)]
        [TestCase(10, 0f, 15f, ExpectedResult = 0)]      // never below 0
        [TestCase(-10, 50f, 15f, ExpectedResult = -10)]  // shredded DF is left alone
        [TestCase(100, 150f, 0f, ExpectedResult = 0)]    // pen capped at 100%
        public int PenetrationThenLethality(int defense, float pen, float lethality)
            => CombatMath.DFAfterPenetration(defense, pen, lethality);
    }

    public class CritTests
    {
        [TestCase(25f)]
        [TestCase(50f)]
        public void LongRunRateMatchesTheChance(float chance)
        {
            var attacker = new FakeStats(chance);
            Random.InitState(1234);

            const int hits = 40000;
            int crits = 0, sinceLast = 0, longestDry = 0;
            for (int i = 0; i < hits; i++)
            {
                Crit.Roll(100f, attacker, out bool isCrit);
                if (isCrit) { crits++; sinceLast = 0; }
                else longestDry = Mathf.Max(longestDry, ++sinceLast);
            }

            Assert.AreEqual(chance / 100f, crits / (float)hits, 0.01f);
            if (chance == 25f) Assert.LessOrEqual(longestDry, 11);   // C = 8.47%: the 12th hit is a sure crit
        }

        [Test]
        public void ACritDealsTheMultiplier()
        {
            float damage = Crit.Roll(100f, new FakeStats(100f), out bool isCrit);
            Assert.IsTrue(isCrit);
            Assert.AreEqual(100f * Crit.Multiplier, damage);

            Crit.Roll(100f, new FakeStats(0f), out bool never);
            Assert.IsFalse(never);
        }

        private sealed class FakeStats : IHeroStats
        {
            private readonly float _crit;
            public FakeStats(float crit) { _crit = crit; }
            public float GetStat(StatEnum type) => type == StatEnum.CritChance ? _crit : 0f;
            public float GetBaseStat(StatEnum type) => GetStat(type);
            public int CurrentHP => 1;
            public int MaxHP => 1;
            public int CurrentMana => 0;
            public int MaxMana => 0;
            public int AttackDamage => 0;
            public int Defence => 0;
            public int Magic => 0;
            public float AttackSpeed => 1f;
            public int Range => 1;
            public int Star => 1;
        }
    }

    public class HeroScalingTests
    {
        [Test]
        public void StarsMultiplyHPADAndAP()
        {
            Assert.AreEqual(1f, HeroStar.HpMultiplier(1), 1e-4f);
            Assert.AreEqual(1.8f * 1.8f, HeroStar.HpMultiplier(3), 1e-4f);
            Assert.AreEqual(1.5f, HeroStar.AtkMultiplier(2), 1e-4f);
            Assert.AreEqual(1.5f, HeroStar.ApMultiplier(2), 1e-4f);
            Assert.AreEqual(4, HeroStar.Copies(3));
        }

        [TestCase(1, ExpectedResult = 1f)]
        [TestCase(11, ExpectedResult = 2f)]
        [TestCase(30, ExpectedResult = 3.9f)]
        [TestCase(0, ExpectedResult = 1f)]      // an entry saved before levels reads as level 1
        [TestCase(99, ExpectedResult = 3.9f)]
        public float LevelAddsTenPercentEach(int level) => Mathf.Round(HeroLevel.Multiplier(level) * 100f) / 100f;

        [TestCase(HeroTierEnum.Common, ExpectedResult = 1)]
        [TestCase(HeroTierEnum.Uncommon, ExpectedResult = 2)]
        [TestCase(HeroTierEnum.Rare, ExpectedResult = 3)]
        public int PriceComesFromTier(HeroTierEnum tier) => HeroPrice.Of(tier);
    }

    public class WalletTests
    {
        [Test]
        public void SpendsOnlyWhatItHas()
        {
            var wallet = new Wallet(5);
            int raised = -1;
            wallet.OnGoldChanged += gold => raised = gold;

            Assert.IsFalse(wallet.TrySpend(6));
            Assert.AreEqual(-1, raised);
            Assert.IsTrue(wallet.TrySpend(5));
            Assert.AreEqual(0, wallet.Gold);
            Assert.AreEqual(0, raised);

            wallet.Earn(3);
            Assert.AreEqual(3, wallet.Gold);
        }
    }
}
