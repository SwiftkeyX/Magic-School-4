using System.Collections.Generic;
using MagicSchool.Contracts;
using static MagicSchool.Skills.SkillFactory;

namespace MagicSchool.Skills
{
    internal class SkeletonArcherSkill : SkillDefinition
    {
        // 2 barrages of 4 shots = 8 shots, so 25% each keeps the old total of 4 x 50%
        private static float ADDamagePerShot => Tuned(25f);
        private static float MGDamagePerShot => Tuned(25f);
        private static int BarrageCount => Tuned(2);
        private static int ShotsPerBarrage => Tuned(4);
        private static float IntervalBetweenShot => Tuned(0.1f);
        private static float PauseBetweenBarrage => Tuned(1.5f);
        private static float BarrageCastTime => IntervalBetweenShot * (ShotsPerBarrage - 1);

        // a long shot - its target 3+ hexes away when it lands - deals [this]% more for every ally on his Long Shot mark
        private static int LongShotHexes => Tuned(3);
        private static float LongShotBonusPerAlly => Tuned(30f);
        private static float LongShotBonusPerAllyPerStar => Tuned(10f);

        private const string LongShotMark = "Long Shot";
        public override IReadOnlyList<string> SpecialMarks => new[] { LongShotMark };

        public static SkillDefinition Build(TemplateActionRegistrySO registry) => new SkeletonArcherSkill(registry);

        private SkeletonArcherSkill(TemplateActionRegistrySO registry) : base(registry) { }

        public override string SkillName => "Bone Barrage";

        public override string Description
            => $"Fires {BarrageCount} barrages of {ShotsPerBarrage} shots at the furthest enemy, picking the furthest again "
             + $"for every barrage. Each shot lands for {AD(ADDamagePerShot)} + {AP(MGDamagePerShot)}. "
             + $"A shot that lands {LongShotHexes}+ hexes away deals {PerAllyText()} more for every ally on his {LongShotMark} mark.";

        // e.g. "50%" for a 2-star, or with Alt held "[30% + 10% per star]"
        private string PerAllyText()
        {
            IHeroStats stats = TextStats;
            if (TextFormula || stats == null) return $"[{LongShotBonusPerAlly}% + {LongShotBonusPerAllyPerStar}% per star]";
            return $"{PerAlly(stats.Star):0.##}%";
        }

        // ============================== active ==============================
        // one barrage is one round, played BarrageCount times. Every shot aims at the furthest enemy when it is fired,
        // so the second barrage goes to whoever is furthest then, not to a target the first one already killed.
        protected override SkillFlow BuildActiveFlow(TemplateActionRegistrySO registry)
            => Repeat(Shoot(registry), times: BarrageCount);

        private static SkillFlow Shoot(TemplateActionRegistrySO registry)
        {
            ProjectileTuning tune = TuneProjectile(castTime: 0f);

            SkillPart shoot = Part(
                registry: registry,
                source: ActionSourceEnum.Self,
                action: TemplateActionEnum.FireTimingRunnerFirstHitProjectile,
                target: AimTargetEnum.Furthest,
                tuning: TuneFireTimingRunnerProjectile(ShotsPerBarrage, FireTimingModeEnum.Sequence, IntervalBetweenShot, tune,
                                                       castTime: BarrageCastTime, pauseAfter: PauseBetweenBarrage),

                DamageWhen(
                    recipient: EffectRecipientEnum.SameToAimTarget,
                    conditions: new List<SkillCondition> { new FarFromCasterCondition(LongShotHexes) },
                    amplifier: 0f,
                    (StatEnum.ATK, ADDamagePerShot), (StatEnum.AP, MGDamagePerShot)
                ).AmplifiedBy(LongShotBonus)
            );

            return Flow(onStart: shoot);
        }

        // (30% + 10% per star) for every ally on his Long Shot mark
        // e.g. a 2-star with 2 allies on it => 2 x 50% = +100%
        private static float LongShotBonus(IEffectable caster)
        {
            if (!(caster is IAligned aligned)) return 0f;
            int star = caster is IHeroStats stats ? stats.Star : 1;
            return aligned.AlliesOnSpecialMark(LongShotMark).Count * PerAlly(star) / 100f;
        }

        private static float PerAlly(int star) => LongShotBonusPerAlly + LongShotBonusPerAllyPerStar * star;

    }
}