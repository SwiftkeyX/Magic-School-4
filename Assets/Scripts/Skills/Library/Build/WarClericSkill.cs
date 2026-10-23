using System.Collections.Generic;
using MagicSchool.Contracts;
using static MagicSchool.Skills.SkillFactory;

namespace MagicSchool.Skills
{
    internal class WarClericSkill : SkillDefinition
    {
        private static float BurnPerTick => Tuned(60f);
        private static float HealPerTick => Tuned(25f);
        private static float TickInterval => Tuned(1f);
        private static float Duration => Tuned(3f);

        // DamageOverTime / HealOverTime 
        private static float Ticks => Duration / TickInterval;
        private static float TotalBurn => BurnPerTick * Ticks;
        private static float TotalHeal => HealPerTick * Ticks;

        // area
        private static int AreaHexes => Tuned(2);
        private static float AreaDiameter => AreaHexes * 2f + 0.5f;

        // the heal is [HealPerTick x (allies on his Sanctuary mark + 1)]% AP
        private const string SanctuaryMark = "Sanctuary";
        public override IReadOnlyList<string> SpecialMarks => new[] { SanctuaryMark };

        public static SkillDefinition Build(TemplateActionRegistrySO registry) => new WarClericSkill(registry);

        private WarClericSkill(TemplateActionRegistrySO registry) : base(registry) { }

        public override string SkillName => "Holy Fire";

        public override string Description
            => $"Consecrates the ground within {AreaHexes} hexes of himself for {Duration} seconds. Every "
             + $"{TickInterval} second, enemies inside burn for {AP(BurnPerTick)} and allies inside, himself "
             + $"included, heal for {AP(HealPerTick)}, "
             + $"{HealPerTick}% more for every ally on his {SanctuaryMark} mark.";

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
                    ratios: (StatEnum.AP, TotalHeal)
                ).AmplifiedBy(SanctuaryBonus)
            );

            return Flow(onStart: ground);
        }

        // e.g. 2 allies on his Sanctuary mark => x3 heal
        private static float SanctuaryBonus(IEffectable caster)
            => caster is IAligned aligned ? aligned.AlliesOnSpecialMark(SanctuaryMark).Count : 0f;
    }
}
