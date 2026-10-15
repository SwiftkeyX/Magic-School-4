using MagicSchool.Contracts;
using static MagicSchool.Skills.SkillFactory;

namespace MagicSchool.Skills
{
    internal class GoblinSlingerSkill : SkillDefinition
    {
        private static float DamageRatio => Tuned(300f);
        private static int TargetCount => Tuned(2);

        public static SkillDefinition Build(TemplateActionRegistrySO registry) => new GoblinSlingerSkill(registry);

        private GoblinSlingerSkill(TemplateActionRegistrySO registry) : base(registry) { }

        public override string SkillName => "Aimed Shot";

        public override string Description
            => $"Fires at the {TargetCount} furthest enemies for {DamageRatio}% AD each.";

        // ============================== active ==============================
        protected override SkillFlow BuildActiveFlow(TemplateActionRegistrySO registry)
        {
            ProjectileTuning tune = TuneProjectile(castTime: 0f);

            // one stone per target, all at once.
            // homing, so a stone reaches the back line instead of stopping on the first enemy in its way
            SkillPart shoot = Part(
                registry: registry,
                source: ActionSourceEnum.Self,
                action: TemplateActionEnum.FireTimingRunnerHomingProjectile,
                target: AimTargetEnum.FurthestEach,
                tuning: TuneFireTimingRunnerProjectile(TargetCount, FireTimingModeEnum.AtOnce, innerTuning: tune),

                Damage(EffectRecipientEnum.SameToAimTarget, (StatEnum.ATK, DamageRatio))
            );

            return Flow(onStart: shoot);
        }
    }
}
