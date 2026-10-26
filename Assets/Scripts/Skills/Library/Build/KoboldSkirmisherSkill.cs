using MagicSchool.Contracts;
using static MagicSchool.Skills.SkillFactory;

namespace MagicSchool.Skills
{
    internal class KoboldSkirmisherSkill : SkillDefinition
    {
        private static float DamagePerShot => Tuned(100f);
        private static int ShotCount => Tuned(3);
        private static float IntervalBetweenShot => Tuned(0.1f);
        private static int RandomPoolRadius => Tuned(99);
        private static float TotalCastTime => IntervalBetweenShot * (ShotCount - 1);

        public static SkillDefinition Build(TemplateActionRegistrySO registry) => new KoboldSkirmisherSkill(registry);

        private KoboldSkirmisherSkill(TemplateActionRegistrySO registry) : base(registry) { }

        public override string SkillName => "Scatter Volley";

        public override string Description
            => $"Fires {ShotCount} quick shots at random enemies, {AD(DamagePerShot)} each, "
             + "never hitting the same one twice until every enemy has been hit once.";

        protected override SkillFlow BuildActiveFlow(TemplateActionRegistrySO registry)
        {
            ProjectileTuning tune = TuneProjectile(castTime: 0f);

            SkillPart volley = Part(
                registry: registry,
                source: ActionSourceEnum.Self,
                action: TemplateActionEnum.FireTimingRunnerHomingProjectile,
                target: AimTargetEnum.Random,
                tuning: TuneFireTimingRunnerProjectile(ShotCount, FireTimingModeEnum.Sequence, IntervalBetweenShot, tune,
                                                       randomPoolRadius: RandomPoolRadius, castTime: TotalCastTime),

                Damage(EffectRecipientEnum.SameToAimTarget, (StatEnum.ATK, DamagePerShot))
            );

            return Flow(onStart: volley);
        }
    }
}
