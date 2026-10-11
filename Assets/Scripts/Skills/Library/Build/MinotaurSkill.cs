using MagicSchool.Contracts;
using static MagicSchool.Skills.SkillFactory;

namespace MagicSchool.Skills
{
    internal class MinotaurSkill : SkillDefinition
    {
        private const float DamageRatio = 120f;
        private const float StunDuration = 2f;

        public static SkillDefinition Build(TemplateActionRegistrySO registry) => new MinotaurSkill(registry);

        private MinotaurSkill(TemplateActionRegistrySO registry) : base(registry) { }

        public override string SkillName => "Gore";

        public override string Description
            => $"Rams the current target for {DamageRatio}% AD and stuns it for {StunDuration} seconds.";

        // ============================== active ==============================
        protected override SkillFlow BuildActiveFlow(TemplateActionRegistrySO registry)
        {
            // the horns - a box on the current target
            SkillPart gore = Part(registry,
                source: ActionSourceEnum.Current,
                action: TemplateActionEnum.BoxAOE,
                target: AimTargetEnum.Current,
                Damage(EffectRecipientEnum.EnemiesInArea, (StatEnum.ATK, DamageRatio)),
                Apply(EffectRecipientEnum.EnemiesInArea, BundleRefresh(StunDuration, Status(ModifierEnum.Stun)))
            );

            return Flow(onStart: gore);
        }
    }
}
