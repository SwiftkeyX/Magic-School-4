using System.Collections.Generic;
using MagicSchool.Contracts;
using static MagicSchool.Skills.SkillFactory;

namespace MagicSchool.Skills
{
    internal class WarClericSkill : SkillDefinition
    {
        private const float BurnPerSecond = 30f;
        private const float BurnDuration = 5f;      // how long an enemy keeps burning after the fire last touched him
        private const float HealPerTick = 50f;
        private const float TickInterval = 1f;
        private const float Duration = 3f;

        // HealOverTime 
        private const float Ticks = Duration / TickInterval;
        private const float TotalHeal = HealPerTick * Ticks;

        // area
        private const int AreaHexes = 2;
        private const float AreaDiameter = AreaHexes * 2f + 0.5f;

        public static SkillDefinition Build(TemplateActionRegistrySO registry) => new WarClericSkill(registry);

        private WarClericSkill(TemplateActionRegistrySO registry) : base(registry) { }

        public override string SkillName => "Holy Fire";

        public override string Description
            => $"Consecrates the ground within {AreaHexes} hexes of himself for {Duration} seconds. Enemies inside "
             + $"are set burning for {BurnPerSecond}% AP every second, and keep burning {BurnDuration} seconds after "
             + $"they leave. Every {TickInterval} second, allies inside, himself included, heal for {HealPerTick}% AP.";

        // ============================== active ==============================
        protected override SkillFlow BuildActiveFlow(TemplateActionRegistrySO registry)
        {
            SkillPart ground = Part(
                registry: registry,
                source: ActionSourceEnum.Self,
                action: TemplateActionEnum.ZoneAOE,
                target: AimTargetEnum.Self,
                tuning: TuneAOE(length: AreaDiameter, width: AreaDiameter),

                BurnInZone(
                    recipient: EffectRecipientEnum.EnemiesInArea,
                    duration: BurnDuration,
                    zoneInterval: TickInterval,
                    zoneDuration: Duration,
                    perSecond: (StatEnum.AP, BurnPerSecond)),

                HealOverTime(
                    recipient: EffectRecipientEnum.AlliesInArea,
                    duration: Duration,
                    interval: TickInterval,
                    ratios: (StatEnum.AP, TotalHeal))
            );

            return Flow(onStart: ground);
        }
    }
}
