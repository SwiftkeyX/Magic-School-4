using System.Collections.Generic;
using MagicSchool.Contracts;
using static MagicSchool.Skills.SkillFactory;

namespace MagicSchool.Skills
{
    // 2 barrages of 4 shots, 25% each, keep the old total of 4 x 50%. One barrage is one round; each shot aims at the
    // furthest enemy when fired, so the second barrage goes to whoever is furthest then, not to one the first already
    // killed. A shot deals full damage 3+ hexes away and CloseShotDamage% closer, and (30% + 10% per star) more per
    // ally on his Long Shot mark; his auto attacks follow both rules (a 2-star, 2 allies, 2 hexes away => (1 + 100%) x
    // 50% = 100% AD).
    internal class SkeletonArcherSkill : SkillDefinition
    {
        private static float ADDamagePerShot => Tuned(25f);
        private static float MGDamagePerShot => Tuned(25f);
        private static int BarrageCount => Tuned(2);
        private static int ShotsPerBarrage => Tuned(4);
        private static float IntervalBetweenShot => Tuned(0.1f);
        private static float PauseBetweenBarrage => Tuned(1.5f);
        private static float BarrageCastTime => IntervalBetweenShot * (ShotsPerBarrage - 1);

        private static int LongShotHexes => Tuned(3);
        private static float CloseShotDamage => Tuned(50f);

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
             + $"A shot that lands closer than {LongShotHexes} hexes deals only {CloseShotDamage}%. "
             + $"Every shot deals {PerAllyText()} more for every ally on his {LongShotMark} mark. "
             + "Both apply to his auto attacks too.";

        public override float AutoAttackDamageMultiplier(ICombatant target)
        {
            bool far = Caster is IHexMeasurable me && target != null && me.IsAtLeastHexesAway(target, LongShotHexes);
            return (1f + LongShotBonus(Caster)) * (far ? 1f : CloseShotDamage / 100f);
        }

        private string PerAllyText()
        {
            IHeroStats stats = TextStats;
            if (TextFormula || stats == null) return $"[{LongShotBonusPerAlly}% + {LongShotBonusPerAllyPerStar}% per star]";
            return $"{PerAlly(stats.Star):0.##}%";
        }

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

                Damage(
                    recipient: EffectRecipientEnum.SameToAimTarget,
                    (StatEnum.ATK, ADDamagePerShot), (StatEnum.AP, MGDamagePerShot)
                ).AmplifiedBy(LongShotBonus)
                 .FullOnlyWhen(new FarFromCasterCondition(LongShotHexes), CloseShotDamage / 100f)
            );

            return Flow(onStart: shoot);
        }

        private static float LongShotBonus(IEffectable caster)
        {
            if (!(caster is IAligned aligned)) return 0f;
            int star = caster is IHeroStats stats ? stats.Star : 1;
            return aligned.AlliesOnSpecialMark(LongShotMark).Count * PerAlly(star) / 100f;
        }

        private static float PerAlly(int star) => LongShotBonusPerAlly + LongShotBonusPerAllyPerStar * star;

    }
}