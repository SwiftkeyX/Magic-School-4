using MagicSchool.Contracts;
using static MagicSchool.Skills.SkillFactory;

namespace MagicSchool.Skills
{
    internal class BasiliskSkill : SkillDefinition
    {
        private static float BonusDF => Tuned(15f);
        private static float BonusDuration => Tuned(-1f);

        public static SkillDefinition Build(TemplateActionRegistrySO registry) => new BasiliskSkill(registry);

        private BasiliskSkill(TemplateActionRegistrySO registry) : base(registry) { }

        public override string SkillName => "Hardening Scales";

        public override string Description
            => $"Gains {BonusDF} DF for the rest of the fight. Stacks every cast.";

        protected override SkillFlow BuildActiveFlow(TemplateActionRegistrySO registry)
        {
            SkillPart scales = Part(registry,
                source: ActionSourceEnum.Self,
                action: TemplateActionEnum.Cast,
                target: AimTargetEnum.Self,
                Apply(
                    EffectRecipientEnum.Self,
                    BundleStack(BonusDuration, Buff(ModifierEnum.DF, BonusDF))
                )
            );

            return Flow(onStart: scales);
        }
    }
}
