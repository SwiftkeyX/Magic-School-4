using System.Collections.Generic;
using MagicSchool.Contracts;
using static MagicSchool.Skills.SkillFactory;

namespace MagicSchool.Skills
{
    internal class WarClericSkill : SkillDefinition
    {
        private static float BurnPerTick => Tuned(60f);
        private static float HealPerTick => Tuned(50f);
        private static float TickInterval => Tuned(1f);
        private static float Duration => Tuned(3f);

        // DamageOverTime / HealOverTime 
        private static float Ticks => Duration / TickInterval;
        private static float TotalBurn => BurnPerTick * Ticks;
        private static float TotalHeal => HealPerTick * Ticks;

        // area
        private static int AreaHexes => Tuned(2);
        private static float AreaDiameter => AreaHexes * 2f + 0.5f;

        public static SkillDefinition Build(TemplateActionRegistrySO registry) => new WarClericSkill(registry);

        private WarClericSkill(TemplateActionRegistrySO registry) : base(registry) { }

        public override string SkillName => "Holy Fire";

        public override string Description
            => $"Consecrates the ground within {AreaHexes} hexes of himself for {Duration} seconds. Every "
             + $"{TickInterval} second, enemies inside burn for {BurnPerTick}% AP and allies inside, himself "
             + $"included, heal for {HealPerTick}% AP.";

        // ============================== active ==============================
        protected override SkillFlow BuildActiveFlow(TemplateActionRegistrySO registry)
        {
            SkillPart ground = Part(
                registry: registry,
                source: ActionSourceEnum.Self,
                action: TemplateActionEnum.ZoneAOE,
                target: AimTargetEnum.Self,
                tuning: TuneAOE(length: AreaDiameter, width: AreaDiameter),

                Burn(
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

            return Flow(onStart: ground);
        }
    }
}
