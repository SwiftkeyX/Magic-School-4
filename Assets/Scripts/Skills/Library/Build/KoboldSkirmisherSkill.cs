using MagicSchool.Contracts;
using static MagicSchool.Skills.SkillFactory;

namespace MagicSchool.Skills
{
    internal class KoboldSkirmisherSkill : SkillDefinition
    {
        private const float DamagePerShot = 100f;
        private const int ShotCount = 3;
        private const float IntervalBetweenShot = 0.1f;
        private const int RandomPoolRadius = 99;       // the whole board - any enemy can be picked
        private const float TotalCastTime = IntervalBetweenShot * (ShotCount - 1);

        public static SkillDefinition Build(TemplateActionRegistrySO registry) => new KoboldSkirmisherSkill(registry);

        private KoboldSkirmisherSkill(TemplateActionRegistrySO registry) : base(registry) { }

        public override string SkillName => "Scatter Volley";

        public override string Description
            => $"Fires {ShotCount} quick shots at random enemies, {DamagePerShot}% AD each, "
             + "never hitting the same one twice until every enemy has been hit once.";

        // ============================== active ==============================
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
