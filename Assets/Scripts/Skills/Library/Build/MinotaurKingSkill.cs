using MagicSchool.Contracts;
using static MagicSchool.Skills.SkillFactory;

namespace MagicSchool.Skills
{
    // The first boss (stage 5), kept easy on purpose: one big body and one readable skill.
    // Ground Slam only reaches the heroes right next to him, so the lesson is "don't pile your melee on him".
    internal class MinotaurKingSkill : SkillDefinition
    {
        private static float DamageRatio => Tuned(150f);
        private static float StunDuration => Tuned(1f);
        private static float SlamDiameter => Tuned(3f);      // the ring of hexes around him, not the ring after (~2 away)

        public static SkillDefinition Build(TemplateActionRegistrySO registry) => new MinotaurKingSkill(registry);

        private MinotaurKingSkill(TemplateActionRegistrySO registry) : base(registry) { }

        public override string SkillName => "Ground Slam";

        public override string Description
            => $"Slams the ground, dealing {AD(DamageRatio)} to every enemy next to him and stunning them for {StunDuration} second.";

        // ============================== active ==============================
        protected override SkillFlow BuildActiveFlow(TemplateActionRegistrySO registry)
        {
            SkillPart slam = Part(registry,
                source: ActionSourceEnum.Self,
                action: TemplateActionEnum.CircleAOE,
                target: AimTargetEnum.Self,
                tuning: TuneAOE(length: SlamDiameter, width: SlamDiameter),
                Damage(EffectRecipientEnum.EnemiesInArea, (StatEnum.ATK, DamageRatio)),
                Apply(EffectRecipientEnum.EnemiesInArea, BundleRefresh(StunDuration, Status(ModifierEnum.Stun)))
            );

            return Flow(onStart: slam);
        }
    }
}
