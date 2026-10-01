using System.Collections.Generic;
using MagicSchool.Contracts;
using static MagicSchool.Skills.SkillFactory;

namespace MagicSchool.Skills
{
    internal class SkeletonArcherSkill : SkillDefinition
    {
        private const float ADDamagePerShot = 125f;
        private const float MGDamagePerShot = 125f;
        private const int ShotCount = 4;
        private const float IntervalBetweenShot = 0.1f;
        private const float TotalCastTime = IntervalBetweenShot * (ShotCount - 1);

        public static SkillDefinition Build(TemplateActionRegistrySO registry) => new SkeletonArcherSkill(registry);

        private SkeletonArcherSkill(TemplateActionRegistrySO registry) : base(registry) { }

        public override string SkillName => "Bone Barrage";

        public override string Description
            => $"Fires {ShotCount} shots in quick succession at the furthest enemy, each one landing for "
             + $"{ADDamagePerShot}% AD + {MGDamagePerShot}% AP.";

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

                Damage(
                    recipient: EffectRecipientEnum.SameToAimTarget,
                    (StatEnum.ATK, ADDamagePerShot), (StatEnum.AP, MGDamagePerShot)
                )
            );

            return Flow(onStart: shoot);
        }

    }
}