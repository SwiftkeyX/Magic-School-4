using System.Collections.Generic;
using MagicSchool.Contracts;
using static MagicSchool.Skills.SkillFactory;

namespace MagicSchool.Skills
{
    internal class LumenSkill
    {
        private const float ExplodeDmg = 240f;
        private const float BlastSize = 4.5f;
        private static readonly float BlastRadius = Reach(BlastSize);

        public static SkillDefinition Build(TemplateActionRegistrySO registry)
        {
            return new SkillDefinition(
                skillName: "Skill",
                activeSteps: new List<SkillStep> { Shoot(registry), Explode(registry) },
                description: "Fires a homing shot into the densest cluster of enemies. "
                           + $"it bursts where it lands, dealing {ExplodeDmg}% AD to everyone inside the blast.");
        }

        private static SkillStep Shoot(TemplateActionRegistrySO registry)
        {
            SkillActionGroup shoot = ActionGroup(
                registry: registry,
                source: ActionSourceEnum.Self,
                action: TemplateActionEnum.HomingProjectile,
                target: AimTargetEnum.ClusteredCircle,
                tuning: TuneProjectile(spread: BlastRadius)
            );

            return Step(trigger: TriggerEnum.OnCast, groups: shoot);
        }

        private static SkillStep Explode(TemplateActionRegistrySO registry)
        {
            SkillActionGroup explode = ActionGroup(
                registry: registry,
                source: ActionSourceEnum.WhereProjectileHit,
                action: TemplateActionEnum.CircleAOE,
                target: AimTargetEnum.WhereProjectileHit,
                tuning: TuneAOE(size: BlastSize),

                Damage(
                    recipient: EffectRecipientEnum.EnemiesInArea,
                    ratios: (StatEnum.ATK, ExplodeDmg)
                )
            );

            return Step(trigger: TriggerEnum.OnHit, groups: explode);
        }
    }
}