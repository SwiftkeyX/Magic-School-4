using System.Collections.Generic;
using MagicSchool.Contracts;
using static MagicSchool.Skills.SkillFactory;

namespace MagicSchool.Skills
{
    internal static class EttinSkill
    {
        private const float FlatBonusHP = 250f;
        private const float BonusHPFromAP = 100f;
        private const float BonusDuration = -1f;    // the rest of the fight
        private const float MaxHPPerAttack = 5f;

        public static SkillDefinition Build(TemplateActionRegistrySO registry)
        {
            // FLAGGING: the sheet says this stacks every cast, but the same modifier instance is
            // refreshed rather than added again, so a second cast gives nothing more. Needs the
            // stack-vs-refresh rule in ModifierResolver.
            SkillActionGroup skulls = ActionGroup(registry,
                source: ActionSourceEnum.Self,
                action: TemplateActionEnum.Cast,
                target: AimTargetEnum.Self,
                Apply(EffectRecipientEnum.Self,
                      Bundle(
                        BonusDuration, 
                        Buff(
                            ModifierEnum.BonusHP, 
                            FlatBonusHP, 
                            (StatEnum.AP, BonusHPFromAP)
                        )
                ))
            );

            // every auto attack also hits the target for a share of his own max HP
            SkillActionGroup onAttack = ActionGroup(registry,
                source: ActionSourceEnum.Self,
                action: TemplateActionEnum.Cast,
                target: AimTargetEnum.Current,
                Damage(
                    EffectRecipientEnum.SameToAimTarget, 
                    (StatEnum.MaxHP, MaxHPPerAttack)
                )
            );

            return new SkillDefinition(
                skillName: "Thick Skulls",
                activeSteps: new List<SkillStep> { Step(trigger: TriggerEnum.OnCast, groups: skulls) },
                passiveSteps: new List<SkillStep> { Step(trigger: TriggerEnum.OnAttack, groups: onAttack) },
                description: $"Gains {FlatBonusHP} + {BonusHPFromAP}% AP bonus health for the rest of the fight.",
                passiveDescription: $"Auto attacks deal an extra {MaxHPPerAttack}% of his max HP.");
        }
    }
}
