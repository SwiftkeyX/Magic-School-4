using System.Collections.Generic;
using MagicSchool.Contracts;
using static MagicSchool.Skills.SkillFactory;

namespace MagicSchool.Skills
{
    internal class OrcBlademasterSkill : SkillDefinition
    {
        private const float DamagePerTick = 80f;
        private const float TickInterval = 0.5f;
        private const float Duration = 4f;

        public static SkillDefinition Build(TemplateActionRegistrySO registry) => new OrcBlademasterSkill(registry);

        private OrcBlademasterSkill(TemplateActionRegistrySO registry) : base(registry) { }

        public override string SkillName => "Blade Storm";

        public override string Description
            => $"Whips up a storm around himself that deals {DamagePerTick}% AD to every enemy standing "
             + $"in it, split over {Duration} seconds and ticking every {TickInterval} seconds.";

        // ============================== active ==============================
        protected override SkillFlow BuildActiveFlow(TemplateActionRegistrySO registry)
        {
            SkillPart spin = Part(
                registry: registry,
                source: ActionSourceEnum.Self,
                action: TemplateActionEnum.ZoneAOE,
                target: AimTargetEnum.Self,
                tuning: TuneAOE(castTime: Duration, sticky: true),

                DamageOverTime(
                    recipient: EffectRecipientEnum.EnemiesInArea,
                    interval: TickInterval,
                    duration: Duration,
                    ratios: (StatEnum.ATK, DamagePerTick))
            );

            return Flow(onStart: spin);
        }
    }
}
