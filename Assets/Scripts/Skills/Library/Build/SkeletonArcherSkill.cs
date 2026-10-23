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
        private static float LongShotBonusPerAlly => Tuned(75f);

        private const string LongShotMark = "Long Shot";
        public override IReadOnlyList<string> SpecialMarks => new[] { LongShotMark };

        public static SkillDefinition Build(TemplateActionRegistrySO registry) => new SkeletonArcherSkill(registry);

        private SkeletonArcherSkill(TemplateActionRegistrySO registry) : base(registry) { }

        public override string SkillName => "Bone Barrage";

        public override string Description
            => $"Fires {BarrageCount} barrages of {ShotsPerBarrage} shots at the furthest enemy, picking the furthest again "
             + $"for every barrage. Each shot lands for {AD(ADDamagePerShot)} + {AP(MGDamagePerShot)}. "
             + $"A shot that lands {LongShotHexes}+ hexes away deals {LongShotBonusPerAlly}% more for every ally on his {LongShotMark} mark.";

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

        // e.g. 3 allies on his Long Shot mark => +45%
        private static float LongShotBonus(IEffectable caster)
            => caster is IAligned aligned ? aligned.AlliesOnSpecialMark(LongShotMark).Count * LongShotBonusPerAlly / 100f : 0f;

    }
}