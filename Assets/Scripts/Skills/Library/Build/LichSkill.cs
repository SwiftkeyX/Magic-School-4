using System.Collections.Generic;
using MagicSchool.Contracts;
using static MagicSchool.Skills.SkillFactory;

namespace MagicSchool.Skills
{
    internal static class LichSkill
    {
        private const float DamageRatio = 250f;
        private const float OmnivampPercent = 40f;
        private const float OmnivampDuration = 0.5f;
        private const float BeamLength = 10f;         
        private const float BeamWidth = 3f;          

        public static SkillDefinition Build(TemplateActionRegistrySO registry)
        {
            // ContactAOE and projectiles apply their effects per enemy hit, in list order - so the
            // lifesteal is on before each hit's damage lands, and re-applying the same instance
            // refreshes it rather than stacking.
            SkillActionGroup beam = ActionGroup(registry,
                source: ActionSourceEnum.Self,
                action: TemplateActionEnum.BoxAOE,
                target: AimTargetEnum.ClusteredLaser,
                tuning: TuneAOE(length: BeamLength, width: BeamWidth, offset: AOEOffsetEnum.Tip),
                Apply(EffectRecipientEnum.Self, Bundle(OmnivampDuration, Buff(ModifierEnum.Omnivamp, OmnivampPercent))),
                Damage(EffectRecipientEnum.EnemiesInPath, (StatEnum.AP, DamageRatio))
            );

            return new SkillDefinition(
                skillName: "Soul Drain",
                activeSteps: new List<SkillStep> { Step(trigger: TriggerEnum.OnCast, groups: beam) },
                description: $"Fires a piercing beam down the most crowded enemy lane: {DamageRatio}% AP to everything "
                             + $"in it. He heals for {OmnivampPercent}% of the damage dealt.");
        }
    }
}
