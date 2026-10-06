using System.Collections.Generic;
using MagicSchool.Contracts;
using static MagicSchool.Skills.SkillFactory;

namespace MagicSchool.Skills
{
    internal class DragonSkill : SkillDefinition
    {
        private const float DamageRatio = 300f;
        private const float BurnPerSecond = 30f;
        private const float BurnDuration = 5f;
        private const float ConeLength = 3.5f;       // world units from his tip outward
        private const float ConeWidth = 3.5f;        // world units across the open end

        public static SkillDefinition Build(TemplateActionRegistrySO registry) => new DragonSkill(registry);

        private DragonSkill(TemplateActionRegistrySO registry) : base(registry) { }

        public override string SkillName => "Dragon Breath";

        public override string Description
            => $"Breathes a cone of fire at the densest cluster: {DamageRatio}% AP, and sets them burning for "
             + $"{BurnPerSecond}% AP every second for {BurnDuration} seconds.";

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
                Burn(EffectRecipientEnum.EnemiesInArea, BurnDuration, (StatEnum.AP, BurnPerSecond))
            );

            return Flow(onStart: breath);
        }
    }
}
