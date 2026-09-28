using System.Collections.Generic;
using MagicSchool.Contracts;
using static MagicSchool.Skills.SkillFactory;

namespace MagicSchool.Skills
{
    internal static class VesperSkill
    {
        private const float DamageRatio = 200f;
        private const float ShredDuration = -1f;
        private const float ShredFromAP = 20f;

        public static SkillDefinition Build(TemplateActionRegistrySO registry)
        {
            SkillActionGroup shootProjectile = ActionGroup(
                registry: registry,
                source: ActionSourceEnum.Self,
                action: TemplateActionEnum.FirstHitProjectile,
                target: AimTargetEnum.Current,

                Damage(
                    recipient: EffectRecipientEnum.SameToAimTarget,
                    ratios: (StatEnum.ATK, DamageRatio)),

                Apply(
                    recipient: EffectRecipientEnum.SameToAimTarget,
                    modifier: Bundle(
                        duration: ShredDuration,
                        modifiers: Buff(
                            modifier: ModifierEnum.DefendShred,
                            // 20% of the total AP is the total that reduce the target's DF. 
                            ratios: (StatEnum.AP, -ShredFromAP, ScaleFromEnum.Total))))
            );

            return new SkillDefinition(
                skillName: "Skill",
                activeSteps: new List<SkillStep> { Step(trigger: TriggerEnum.OnCast, groups: shootProjectile) },
                description: $"Fires a shot at the current target. The first enemy it finds takes {DamageRatio}% AD "
                             + $"and loses {ShredFromAP}% AP worth of armour for the rest of the fight.");
        }
    }
}
