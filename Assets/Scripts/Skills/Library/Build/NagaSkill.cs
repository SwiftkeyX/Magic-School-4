using System.Collections.Generic;
using MagicSchool.Contracts;
using static MagicSchool.Skills.SkillFactory;

namespace MagicSchool.Skills
{
    internal class NagaSkill : SkillDefinition
    {
        private static float DamageRatio => Tuned(240f);
        private static float WoundDuration => Tuned(5f);
        private static float WoundedAmplifier => Tuned(0.3f);

        public static SkillDefinition Build(TemplateActionRegistrySO registry) => new NagaSkill(registry);

        private NagaSkill(TemplateActionRegistrySO registry) : base(registry) { }

        public override string SkillName => "Split Venom";

        public override string Description
            => $"Spits a homing bolt at the current enemy. It deals {AP(DamageRatio)} and leaves the "
             + $"target wounded, so everything that tries to heal them for the next {WoundDuration} "
             + "seconds does less. If target is already wounded, amplified damage by "
             + $"+{WoundedAmplifier * 100}%";

        // ============================== active ==============================
        protected override SkillFlow BuildActiveFlow(TemplateActionRegistrySO registry)
        {
            List<SkillCondition> amplifierCondition = new List<SkillCondition>
            {
                new HasStatusCondition(
                    subject:     ConditionSubjectEnum.Recipient,
                    status:      ModifierEnum.Wound,
                    wantPresent: true),
            };

            // homing projectile to furthest enemy
            SkillPart shootProjectile = Part(
                registry: registry,
                source: ActionSourceEnum.Self,
                action: TemplateActionEnum.HomingProjectile,
                target: AimTargetEnum.Current,

                DamageWhen(
                    recipient: EffectRecipientEnum.SameToAimTarget,
                    conditions: amplifierCondition,
                    amplifier: WoundedAmplifier,
                    ratios: (StatEnum.AP, DamageRatio)),

                Apply(
                    recipient: EffectRecipientEnum.SameToAimTarget,
                    modifier: BundleRefresh(
                        duration: WoundDuration,
                        modifiers: Status(ModifierEnum.Wound)))
            );

            return Flow(onStart: shootProjectile);
        }
    }
}
