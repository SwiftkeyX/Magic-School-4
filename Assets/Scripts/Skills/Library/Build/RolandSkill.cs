using System.Collections.Generic;
using MagicSchool.Contracts;
using static MagicSchool.Skills.SkillFactory;

namespace MagicSchool.Skills
{
    internal static class RolandSkill
    {
        private const float DamagePerTick = 80f;
        private const float TickInterval = 0.5f;
        // the spin lasts this long and he cannot attack through it, so it is one number
        private const float Duration = 4f;

        public static SkillDefinition Build(TemplateActionRegistrySO registry)
        {
            SkillActionGroup spin = ActionGroup(
                registry: registry,
                source: ActionSourceEnum.Self,
                action: TemplateActionEnum.ZoneAOE,
                target: AimTargetEnum.Self,
                tuning: TuneAOE(castTime: Duration, sticky: true),

                DamageOverTime(
                    recipient: EffectRecipientEnum.EnemiesInArea,
                    interval: TickInterval,
                    duration: Duration,
                    ratios: (StatEnum.ATK, DamagePerTick))
            );

            return new SkillDefinition(
                skillName: "Skill",
                activeSteps: new List<SkillStep> { Step(trigger: TriggerEnum.OnCast, groups: spin) },
                description: $"Whips up a storm around himself that deals {DamagePerTick}% AD to every enemy standing "
                             + $"in it, split over {Duration} seconds and ticking every {TickInterval} seconds.");
        }
    }
}
