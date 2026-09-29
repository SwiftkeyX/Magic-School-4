using System.Collections.Generic;
using MagicSchool.Contracts;
using static MagicSchool.Skills.SkillFactory;

namespace MagicSchool.Skills
{
    // ASKING: I start to see the problem from each skill factory being static.
    // The class itself do the job well. But those class would benefit a lot from being a real instance on a hero.
    // For example, this class Bandit can calculate the AS steal in the scene, and steal the actual number for real.
    // The werewolf too, he was using his 3 beat combo and his helper class, I begin to think that's the fix in the wrong point.
    // if the werewolf have its skill a real instance, it would a be a lot simpler to do his 3 beat combo.

    // Additionally, the system is kinda confusing espectially the HeroSkill.cs.
    // That's hard to understand.
    internal static class BanditSkill
    {
        private const float DamageRatio = 160f;
        private const float StealPercent = 30f;
        private const float StealDuration = -1f;   // until the end of the fight

        public static SkillDefinition Build(TemplateActionRegistrySO registry)
        {
            // the target loses 30% of its own attack speed
            SkillActionGroup strike = ActionGroup(registry,
                source: ActionSourceEnum.Self,
                action: TemplateActionEnum.Cast,
                target: AimTargetEnum.Current,
                Damage(EffectRecipientEnum.SameToAimTarget, (StatEnum.ATK, DamageRatio)),
                Apply(
                    EffectRecipientEnum.SameToAimTarget,
                    Bundle(
                        StealDuration,

                        // ASKING: Buff kinda misleading. It does Debuff here. We may need a better name.
                        // Could we have both Buff() and Debuff(), the difference is Debuff will turn the buff value into minus automatically.
                        Buff(
                            ModifierEnum.AS,
                            ScalingSourceEnum.Recipient,
                            (StatEnum.AS, -StealPercent, ScaleFromEnum.Base)
                )))
            );

            // FIXLATER: Bandit's buff should be stacked. Now it's not.
            // and he gains it - as 30% of his own attack speed, since a buff can only read the caster
            // or the one receiving it, not a third hero. Its own step: a step plays only one group.
            SkillActionGroup pocket = ActionGroup(registry,
                source: ActionSourceEnum.Self,
                action: TemplateActionEnum.Cast,
                target: AimTargetEnum.Self,
                Apply(EffectRecipientEnum.Self,
                      Bundle(
                        StealDuration, 
                        Buff(
                            ModifierEnum.AS, 
                            (StatEnum.AS, StealPercent, ScaleFromEnum.Base)
                )))
            );

            return new SkillDefinition(
                skillName: "Pickpocket",
                activeSteps: new List<SkillStep>
                {
                    Step(trigger: TriggerEnum.OnCast, groups: strike),
                    Step(trigger: TriggerEnum.OnExpired, groups: pocket),
                },
                description: $"Strikes the current target for {DamageRatio}% AD and steals {StealPercent}% of its "
                             + "attack speed until the end of the fight.");
        }
    }
}
