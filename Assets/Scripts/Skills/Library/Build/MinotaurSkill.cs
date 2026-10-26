using MagicSchool.Contracts;
using static MagicSchool.Skills.SkillFactory;

namespace MagicSchool.Skills
{
    internal class MinotaurSkill : SkillDefinition
    {
        private static float DamageRatio => Tuned(120f);
        private static float StunDuration => Tuned(2f);

        public static SkillDefinition Build(TemplateActionRegistrySO registry) => new MinotaurSkill(registry);

        private MinotaurSkill(TemplateActionRegistrySO registry) : base(registry) { }

        public override string SkillName => "Gore";

        public override string Description
            => $"Rams the current target for {AD(DamageRatio)} and stuns it for {StunDuration} seconds.";

        protected override SkillFlow BuildActiveFlow(TemplateActionRegistrySO registry)
        {
            SkillPart gore = Part(registry,
                source: ActionSourceEnum.Current,
                action: TemplateActionEnum.CircleAOE,
                target: AimTargetEnum.Current,
                Damage(EffectRecipientEnum.EnemiesInArea, (StatEnum.ATK, DamageRatio)),
                Apply(EffectRecipientEnum.EnemiesInArea, BundleRefresh(StunDuration, Status(ModifierEnum.Stun)))
            );

            return Flow(onStart: gore);
        }
    }
}
