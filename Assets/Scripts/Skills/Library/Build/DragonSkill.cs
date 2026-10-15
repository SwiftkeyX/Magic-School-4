using System.Collections.Generic;
using MagicSchool.Contracts;
using static MagicSchool.Skills.SkillFactory;

namespace MagicSchool.Skills
{
    internal class DragonSkill : SkillDefinition
    {
        private static float DamageRatio => Tuned(300f);
        private static float BurnTotal => Tuned(150f);       // over the whole burn, not per tick
        private static float BurnDuration => Tuned(3f);
        private static float BurnInterval => Tuned(1f);
        private static float ConeLength => Tuned(3.5f);       // world units from his tip outward
        private static float ConeWidth => Tuned(3.5f);        // world units across the open end

        public static SkillDefinition Build(TemplateActionRegistrySO registry) => new DragonSkill(registry);

        private DragonSkill(TemplateActionRegistrySO registry) : base(registry) { }

        public override string SkillName => "Dragon Breath";

        public override string Description
            => $"Breathes a cone of fire at the densest cluster: {DamageRatio}% AP, then burns them for "
             + $"{BurnTotal}% AP over {BurnDuration} seconds.";

        // ============================== active ==============================
        protected override SkillFlow BuildActiveFlow(TemplateActionRegistrySO registry)
        {
            // tip on the dragon, pointed at the densest pack
            SkillPart breath = Part(registry,
                source: ActionSourceEnum.Self,
                action: TemplateActionEnum.TriangleAOE,
                target: AimTargetEnum.ClusteredCircle,
                tuning: TuneAOE(length: ConeLength, width: ConeWidth, offset: AOEOffsetEnum.Tip),
                Damage(EffectRecipientEnum.EnemiesInArea, (StatEnum.AP, DamageRatio)),
                Burn(EffectRecipientEnum.EnemiesInArea, BurnInterval, BurnDuration, (StatEnum.AP, BurnTotal))
            );

            return Flow(onStart: breath);
        }
    }
}
