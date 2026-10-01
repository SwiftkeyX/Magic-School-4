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
        protected override SkillFlow BuildActiveFlow(TemplateActionRegistrySO registry)
        {
            // the lifesteal, put on as the cast begins
            SkillPart focus = Part(registry,
                source: ActionSourceEnum.Self,
                action: TemplateActionEnum.Cast,
                target: AimTargetEnum.Self,
                Apply(EffectRecipientEnum.Self,
                      BundleRefresh(OmnivampDuration, Buff(ModifierEnum.Omnivamp, OmnivampPercent)))
            );

            // the charge. Starts together with the lifesteal and holds him for ChargeTime. A Cast lives as
            // long as its longest modifier, so ManaBlocked is what makes it last - and he gains no mana
            // while charging.
            SkillPart charge = Part(registry,
                source: ActionSourceEnum.Self,
                action: TemplateActionEnum.Cast,
                target: AimTargetEnum.Self,
                tuning: Tune(castTime: ChargeTime),
                Apply(EffectRecipientEnum.Self, BundleRefresh(ChargeTime, Status(ModifierEnum.ManaBlocked)))
            );

            // the slash, when the charge ends. A Cast aimed at the current target hits only it.
            SkillPart slash = Part(registry,
                source: ActionSourceEnum.Self,
                action: TemplateActionEnum.Cast,
                target: AimTargetEnum.Current,
                Damage(EffectRecipientEnum.SameToAimTarget, (StatEnum.ATK, DamageRatio))
            );

            // the charge leads, so the slash waits on it and not on the lifesteal
            return Flow(onStart: Together(charge, focus), onExpired: slash);
        }
    }
}
