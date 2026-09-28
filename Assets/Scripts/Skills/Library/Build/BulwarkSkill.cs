using System.Collections.Generic;
using MagicSchool.Contracts;
using static MagicSchool.Skills.SkillFactory;

namespace MagicSchool.Skills
{
    /// <summary>
    /// Bulwark: braces himself - bonus health and healing over a couple of seconds, plus damage
    /// reduction - and when that wears off, slams the ground for damage around him.
    ///
    /// Two steps: the second one is triggered by the first expiring, not by anything the hero does.
    ///
    /// Ported from Assets/Data/Heroes/Skills/Bulwark.asset.
    /// </summary>
    internal static class BulwarkSkill
    {
        // he is braced and locked out of attacking for the same stretch, so it is said once
        private const float BraceDuration = 2f;
        private const float TickInterval = 0.5f;

        // percentages of the caster's stat, the way the sheet writes them
        private const float HealAmount = 200f;
        private const float SlamDamage = 120f;
        private const float DamageReductionPercent = 25f;   // sheet: 25/25/35%

        public static SkillDefinition Build(TemplateActionRegistrySO registry)
        {
            return new SkillDefinition(
                skillName: "Guardian's Roar",
                activeSteps: new List<SkillStep> { Brace(registry), Slam(registry) },
                description: $"Braces for {BraceDuration} seconds, healing steadily for {HealAmount} and taking "
                             + $"{DamageReductionPercent}% less damage. The moment the brace ends he slams the ground "
                             + $"for {SlamDamage}% AP to every enemy around him.");
        }

        // the cast itself - everything here lands on Bulwark
        private static SkillStep Brace(TemplateActionRegistrySO registry)
        {
            SkillActionGroup brace = ActionGroup(
                registry: registry,
                source: ActionSourceEnum.Self,
                action: TemplateActionEnum.Cast,
                target: AimTargetEnum.Self,
                tuning: Tune(castTime: BraceDuration),

                // sheet: Bulwark heals off AP
                HealOverTime(
                    recipient: EffectRecipientEnum.Self,
                    duration: BraceDuration,
                    interval: TickInterval,
                    ratios: (StatEnum.AP, HealAmount)),

                Apply(
                    recipient: EffectRecipientEnum.Self,
                    modifier: Bundle(
                        duration: BraceDuration,
                        modifiers: Buff(
                            modifier: ModifierEnum.DamageReduction,
                            ratios: DamageReductionPercent))
                )
            );

            return Step(trigger: TriggerEnum.OnCast, groups: brace);
        }

        // fired when the brace above expires
        private static SkillStep Slam(TemplateActionRegistrySO registry)
        {
            SkillActionGroup slam = ActionGroup(
                registry: registry,
                source: ActionSourceEnum.Self,
                action: TemplateActionEnum.CircleAOE,
                target: AimTargetEnum.Self,

                Damage(
                    recipient: EffectRecipientEnum.EnemiesInArea,
                    ratios: (StatEnum.AP, SlamDamage))
            );

            return Step(trigger: TriggerEnum.OnExpired, groups: slam);
        }
    }
}
