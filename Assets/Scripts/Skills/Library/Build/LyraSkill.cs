using System.Collections.Generic;
using MagicSchool.Contracts;
using static MagicSchool.Skills.SkillFactory;

namespace MagicSchool.Skills
{
    /// <summary>
    /// Lyra: a wave through the clustered enemy, damaging everyone it passes through.
    ///
    /// Sheet (Hero set 9): "Send a wave at the clustered enemy; damage falls off per enemy hit.
    /// Allies hit are buffed instead." Two of the three parts cannot be said yet: falloff isn't
    /// implemented (DamageRatio is a flat hit, not per-enemy), only the ally buff is done.
    /// </summary>
    internal static class LyraSkill
    {
        private const float DamageRatio = 170f;   // sheet: 170/255/420% AP
        private const float ASbuff = 25f;
        private const float WaveSize = 2f;        // Lyra's wave is the one projectile bigger than default

        public static SkillDefinition Build(TemplateActionRegistrySO registry)
        {
            return new SkillDefinition(
                skillName: "Skill",
                activeSteps: new List<SkillStep> { Wave(registry) },
                description: "Sends a wave through the thickest part of the enemy line. Enemies it passes through "
                           + $"take {DamageRatio}% AP; allies it passes through are left {ASbuff}% faster for the "
                           + "rest of the fight.");
        }

        private static SkillStep Wave(TemplateActionRegistrySO registry)
        {
            ICustomModifier ASBuff = Bundle(
                duration: -1f,
                Buff(
                    modifier: ModifierEnum.AS,
                    source: ScalingSourceEnum.Recipient,
                    ratios: (StatEnum.AS, ASbuff, ScaleFromEnum.Base))
            );

            SkillActionGroup wave = ActionGroup(
                registry: registry,
                source: ActionSourceEnum.Self,
                action: TemplateActionEnum.PiercingProjectile,
                target: AimTargetEnum.ClusteredLaser,   // A/B: swap to AimTargetEnum.Clustered for the old, radial pick
                tuning: TuneProjectile(size: WaveSize),

                // deal dmg to enemy & give buff to ally 
                Damage(
                    recipient: EffectRecipientEnum.EnemiesInPath,
                    ratios: (StatEnum.AP, DamageRatio)),
                Apply(
                    recipient: EffectRecipientEnum.AlliesInPath,
                    modifier: ASBuff
                )
            );

            return Step(trigger: TriggerEnum.OnCast, groups: wave);
        }
    }
}
