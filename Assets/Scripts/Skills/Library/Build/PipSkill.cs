using System.Collections.Generic;
using MagicSchool.Contracts;
using static MagicSchool.Skills.SkillFactory;

namespace MagicSchool.Skills
{
    internal static class PipSkill
    {
        private const float WoundDuration = 3f;
        private const float PoisonDamage = 300f;
        private const float PoisonInterval = 0.1f;
        private const float PoisonDuration = 3f;

        public static SkillDefinition Build(TemplateActionRegistrySO registry)
        {
            return new SkillDefinition(
                skillName: "Blinding Spore",
                activeSteps: new List<SkillStep> { Dart(registry), Patch(registry) },
                description: "Lobs a spore at the furthest enemy. It does no damage itself - where it lands a patch spreads "
                             + $"that wounds anyone standing in it and poisons them for {PoisonDamage}% AP over "
                             + $"{PoisonDuration} seconds.");
        }

        private static SkillStep Dart(TemplateActionRegistrySO registry)
        {
            SkillActionGroup dart = ActionGroup(
                registry: registry,
                source: ActionSourceEnum.Self,
                action: TemplateActionEnum.HomingProjectile,
                target: AimTargetEnum.Furthest
            );

            return Step(trigger: TriggerEnum.OnCast, groups: dart);
        }

        private static SkillStep Patch(TemplateActionRegistrySO registry)
        {
            SkillActionGroup patch = ActionGroup(
                registry: registry,
                source: ActionSourceEnum.WhereProjectileHit,
                action: TemplateActionEnum.CircleAOE,
                target: AimTargetEnum.WhereProjectileHit,

                Apply(
                    recipient: EffectRecipientEnum.EnemiesInArea,
                    modifier: Bundle(
                        duration: WoundDuration,
                        modifiers: Status(ModifierEnum.Wound)),
                    amplifier: 0.3f),

                // sheet: Pip is AP
                DamageOverTime(
                    recipient: EffectRecipientEnum.EnemiesInArea,
                    interval: PoisonInterval,
                    duration: PoisonDuration,
                    ratios: (StatEnum.AP, PoisonDamage))
            );

            return Step(trigger: TriggerEnum.OnHit, groups: patch);
        }
    }
}
