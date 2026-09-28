using System.Collections.Generic;
using MagicSchool.Contracts;
using static MagicSchool.Skills.SkillFactory;

namespace MagicSchool.Skills
{
    /// <summary>
    /// Solace: a projectile at the current target that does nothing itself - when it lands, an AOE
    /// goes off where it hit.
    ///
    /// Two steps: the second is triggered by the first hitting something, and it spawns and aims at
    /// the projectile's landing point rather than at the hero or a target.
    ///
    /// Ported from Assets/Data/Heroes/Skills/Solace.asset.
    /// </summary>
    internal static class SolaceSkill
    {
        private const float ExplosionDamage = 200f;

        public static SkillDefinition Build(TemplateActionRegistrySO registry)
        {
            return new SkillDefinition(
                skillName: "Detonating Bolt",
                activeSteps: new List<SkillStep> { Dart(registry), Explosion(registry) },
                description: "Throws a bolt at the current target. The bolt detonates where it "
                             + $"lands, dealing {ExplosionDamage}% AP to everyone caught in the blast.");
        }

        // carries no effects of its own - it only exists to land somewhere
        private static SkillStep Dart(TemplateActionRegistrySO registry)
        {
            SkillActionGroup dart = ActionGroup(
                registry: registry,
                source: ActionSourceEnum.Self,
                action: TemplateActionEnum.FirstHitProjectile,
                target: AimTargetEnum.Current
            );

            return Step(trigger: TriggerEnum.OnCast, groups: dart);
        }

        private static SkillStep Explosion(TemplateActionRegistrySO registry)
        {
            SkillActionGroup blast = ActionGroup(
                registry: registry,
                source: ActionSourceEnum.WhereProjectileHit,
                action: TemplateActionEnum.CircleAOE,
                target: AimTargetEnum.WhereProjectileHit,
                // sheet: Solace is AP
                Damage(
                    recipient: EffectRecipientEnum.EnemiesInArea,
                    ratios: (StatEnum.AP, ExplosionDamage))
            );

            return Step(trigger: TriggerEnum.OnHit, groups: blast);
        }
    }
}
