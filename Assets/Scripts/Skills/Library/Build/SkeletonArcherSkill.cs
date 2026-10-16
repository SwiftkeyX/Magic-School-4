using System.Collections.Generic;
using MagicSchool.Contracts;
using static MagicSchool.Skills.SkillFactory;

namespace MagicSchool.Skills
{
    internal class SkeletonArcherSkill : SkillDefinition
    {
        private static float ADDamagePerShot => Tuned(75f);
        private static float MGDamagePerShot => Tuned(75f);
        private static int ShotCount => Tuned(4);
        private static float IntervalBetweenShot => Tuned(0.1f);
        private static float TotalCastTime => IntervalBetweenShot * (ShotCount - 1);

        // a long shot - its target 3+ hexes away when it lands - deals [this]% more for every ally on his first (orange) mark
        private static int LongShotHexes => Tuned(3);
        private static float LongShotBonusPerAlly => Tuned(15f);

        public static SkillDefinition Build(TemplateActionRegistrySO registry) => new SkeletonArcherSkill(registry);

        private SkeletonArcherSkill(TemplateActionRegistrySO registry) : base(registry) { }

        public override string SkillName => "Bone Barrage";

        public override string Description
            => $"Fires {ShotCount} shots in quick succession at the furthest enemy, each one landing for "
             + $"{ADDamagePerShot}% AD + {MGDamagePerShot}% AP. "
             + $"A shot that lands {LongShotHexes}+ hexes away deals {LongShotBonusPerAlly}% more for every ally on his first mark.";

        // ============================== active ==============================
        protected override SkillFlow BuildActiveFlow(TemplateActionRegistrySO registry)
            => Shoot(registry);

        private static SkillFlow Shoot(TemplateActionRegistrySO registry)
        {
            ProjectileTuning tune = TuneProjectile(castTime: 0f);

            SkillPart shoot = Part(
                registry: registry,
                source: ActionSourceEnum.Self,
                action: TemplateActionEnum.FireTimingRunnerFirstHitProjectile,
                target: AimTargetEnum.Furthest,
                tuning: TuneFireTimingRunnerProjectile(ShotCount, FireTimingModeEnum.Sequence, IntervalBetweenShot, tune,
                                                       castTime: TotalCastTime),

                DamageWhen(
                    recipient: EffectRecipientEnum.SameToAimTarget,
                    conditions: new List<SkillCondition> { new FarFromCasterCondition(LongShotHexes) },
                    amplifier: 0f,
                    (StatEnum.ATK, ADDamagePerShot), (StatEnum.AP, MGDamagePerShot)
                ).AmplifiedBy(LongShotBonus)
            );

            return Flow(onStart: shoot);
        }

        // e.g. 3 allies on his first mark => +45%
        private static float LongShotBonus(IEffectable caster)
            => caster is IAligned aligned ? aligned.AlliesOnMark(0) * LongShotBonusPerAlly / 100f : 0f;

    }
}