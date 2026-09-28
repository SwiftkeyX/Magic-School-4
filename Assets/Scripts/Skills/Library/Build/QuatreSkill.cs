using System.Collections.Generic;
using MagicSchool.Contracts;
using static MagicSchool.Skills.SkillFactory;

namespace MagicSchool.Skills
{
    internal static class QuatreSkill
    {
        private const float DamageRatio = 744f;

        public static SkillDefinition Build(TemplateActionRegistrySO registry)
        {
            SkillActionGroup shot = ActionGroup(registry,
                source: ActionSourceEnum.Self,
                action: TemplateActionEnum.PiercingProjectile,
                target: AimTargetEnum.Current,
                Damage(EffectRecipientEnum.EnemiesInPath, (StatEnum.ATK, DamageRatio))
            );

            return new SkillDefinition(
                skillName: "Skill",
                activeSteps: new List<SkillStep> { Step(trigger: TriggerEnum.OnCast, groups: shot) },
                description: $"Fires a shot that carries straight on through the target, dealing {DamageRatio}% AD to "
                             + "every enemy caught along its path.");
        }
    }
}
