using System.Collections.Generic;
using MagicSchool.Contracts;
using static MagicSchool.Skills.SkillFactory;

namespace MagicSchool.Skills
{
    internal class DragonSkill : SkillDefinition
    {
        private const float DamageRatio = 300f;
        private const float BurnTotal = 150f;       // over the whole burn, not per tick
        private const float BurnDuration = 3f;
        private const float BurnInterval = 1f;
        private const float ConeLength = 3.5f;       // world units from his tip outward
        private const float ConeWidth = 3.5f;        // world units across the open end

        public static SkillDefinition Build(TemplateActionRegistrySO registry) => new DragonSkill(registry);

        private DragonSkill(TemplateActionRegistrySO registry) : base(registry) { }

        public override string SkillName => "Dragon Breath";

        public override string Description
            => $"Breathes a cone of fire at the densest cluster: {DamageRatio}% AP, then burns them for "
             + $"{BurnTotal}% AP over {BurnDuration} seconds.";

        // ============================== active ==============================
        protected override List<SkillStep> Active(TemplateActionRegistrySO registry)
        {
            // tip on the dragon, pointed at the densest pack
            SkillActionGroup breath = ActionGroup(registry,
                source: ActionSourceEnum.Self,
                action: TemplateActionEnum.TriangleAOE,
                target: AimTargetEnum.ClusteredCircle,
                tuning: TuneAOE(length: ConeLength, width: ConeWidth, offset: AOEOffsetEnum.Tip),
                Damage(EffectRecipientEnum.EnemiesInArea, (StatEnum.AP, DamageRatio)),
                DamageOverTime(EffectRecipientEnum.EnemiesInArea, BurnInterval, BurnDuration, (StatEnum.AP, BurnTotal))
            );

            return new List<SkillStep> { Step(trigger: TriggerEnum.OnCast, groups: breath) };
        }
    }
}
