using System.Collections.Generic;
using MagicSchool.Contracts;
using static MagicSchool.Skills.SkillFactory;

namespace MagicSchool.Skills
{
    internal class SwordsmanSkill : SkillDefinition
    {
        private const float ChargeTime = 3f;
        private const float DamageRatio = 999f;
        private const float OmnivampPercent = 100f;
        // outlasts the charge, so the lifesteal is still up when the slash lands
        private const float OmnivampDuration = ChargeTime + 0.5f;

        public static SkillDefinition Build(TemplateActionRegistrySO registry) => new SwordsmanSkill(registry);

        private SwordsmanSkill(TemplateActionRegistrySO registry) : base(registry) { }

        public override string SkillName => "Charged Slash";

        public override string Description
            => $"Charges up for {ChargeTime} seconds, then slashes the current target for "
             + $"{DamageRatio}% AD, healing for {OmnivampPercent}% of the damage dealt.";

        // ============================== active ==============================
        protected override List<SkillStep> Active(TemplateActionRegistrySO registry)
        {
            // step 0 - the lifesteal, put on as the cast begins
            SkillActionGroup focus = ActionGroup(registry,
                source: ActionSourceEnum.Self,
                action: TemplateActionEnum.Cast,
                target: AimTargetEnum.Self,
                Apply(EffectRecipientEnum.Self,
                      Bundle(OmnivampDuration, Buff(ModifierEnum.Omnivamp, OmnivampPercent)))
            );

            // step 1 - the charge. Fires alongside step 0 and holds him for ChargeTime. A Cast lives as
            // long as its longest modifier, so ManaBlocked is what makes it last - and he gains no mana
            // while charging.
            SkillActionGroup charge = ActionGroup(registry,
                source: ActionSourceEnum.Self,
                action: TemplateActionEnum.Cast,
                target: AimTargetEnum.Self,
                tuning: Tune(castTime: ChargeTime),
                Apply(EffectRecipientEnum.Self, Bundle(ChargeTime, Status(ModifierEnum.ManaBlocked)))
            );

            // step 2 - the slash, when the charge ends. A Cast aimed at the current target hits only it.
            SkillActionGroup slash = ActionGroup(registry,
                source: ActionSourceEnum.Self,
                action: TemplateActionEnum.Cast,
                target: AimTargetEnum.Current,
                Damage(EffectRecipientEnum.SameToAimTarget, (StatEnum.ATK, DamageRatio))
            );

            return new List<SkillStep>
            {
                Step(trigger: TriggerEnum.OnCast, groups: focus),
                Step(trigger: TriggerEnum.OnCastStart, groups: charge),
                Step(trigger: TriggerEnum.OnExpired, groups: slash),
            };
        }
    }
}
