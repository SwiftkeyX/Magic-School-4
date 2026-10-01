using System.Collections.Generic;
using MagicSchool.Contracts;
using static MagicSchool.Skills.SkillFactory;

namespace MagicSchool.Skills
{
    internal class WarClericSkill : SkillDefinition
    {
        private const float BurnPerTick = 60f;
        private const float HealPerTick = 50f;
        private const float TickInterval = 1f;
        private const float Duration = 3f;

        // DamageOverTime / HealOverTime 
        private const float Ticks = Duration / TickInterval;
        private const float TotalBurn = BurnPerTick * Ticks;
        private const float TotalHeal = HealPerTick * Ticks;

        // area
        private const int AreaHexes = 2;
        private const float AreaDiameter = AreaHexes * 2f + 0.5f;

        public static SkillDefinition Build(TemplateActionRegistrySO registry) => new WarClericSkill(registry);

        private WarClericSkill(TemplateActionRegistrySO registry) : base(registry) { }

        public override string SkillName => "Holy Fire";

        public override string Description
            => $"Consecrates the ground within {AreaHexes} hexes of himself for {Duration} seconds. Every "
             + $"{TickInterval} second, enemies inside burn for {BurnPerTick}% AP and allies inside, himself "
             + $"included, heal for {HealPerTick}% AP.";

        // ============================== active ==============================
        protected override List<SkillFlow> Active(TemplateActionRegistrySO registry)
        {
            SkillPart ground = Part(
                registry: registry,
                source: ActionSourceEnum.Self,
                action: TemplateActionEnum.ZoneAOE,
                target: AimTargetEnum.Self,
                tuning: TuneAOE(length: AreaDiameter, width: AreaDiameter),

                DamageOverTime(
                    recipient: EffectRecipientEnum.EnemiesInArea,
                    interval: TickInterval,
                    duration: Duration,
                    ratios: (StatEnum.AP, TotalBurn)),

                HealOverTime(
                    recipient: EffectRecipientEnum.AlliesInArea,
                    duration: Duration,
                    interval: TickInterval,
                    ratios: (StatEnum.AP, TotalHeal))
            );

            return new List<SkillFlow> { Flow(trigger: TriggerEnum.OnCast, groups: ground) };
        }
    }
}
