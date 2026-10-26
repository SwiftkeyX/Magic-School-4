using System.Collections.Generic;
using MagicSchool.Contracts;
using static MagicSchool.Skills.SkillFactory;

namespace MagicSchool.Skills
{
    // The lifesteal goes on as the cast begins and outlasts the charge, so it is still up when the slash lands. The
    // charge is a Cast holding him for ChargeTime; a Cast lives as long as its longest modifier, so ManaBlocked is
    // what makes it last, and he gains no mana while charging. The charge leads, so the slash waits on it, not on the
    // lifesteal. A Cast aimed at the current target hits only it.
    internal class SwordsmanSkill : SkillDefinition
    {
        private static float ChargeTime => Tuned(3f);
        private static float DamageRatio => Tuned(999f);
        private static float OmnivampPercent => Tuned(100f);
        private static float OmnivampDuration => ChargeTime + 0.5f;

        public static SkillDefinition Build(TemplateActionRegistrySO registry) => new SwordsmanSkill(registry);

        private SwordsmanSkill(TemplateActionRegistrySO registry) : base(registry) { }

        public override string SkillName => "Charged Slash";

        public override string Description
            => $"Charges up for {ChargeTime} seconds, then slashes the current target for "
             + $"{AD(DamageRatio)}, healing for {OmnivampPercent}% of the damage dealt.";

        protected override SkillFlow BuildActiveFlow(TemplateActionRegistrySO registry)
        {
            SkillPart focus = Part(registry,
                source: ActionSourceEnum.Self,
                action: TemplateActionEnum.Cast,
                target: AimTargetEnum.Self,
                Apply(EffectRecipientEnum.Self,
                      BundleRefresh(OmnivampDuration, Buff(ModifierEnum.Omnivamp, OmnivampPercent)))
            );

            SkillPart charge = Part(registry,
                source: ActionSourceEnum.Self,
                action: TemplateActionEnum.Cast,
                target: AimTargetEnum.Self,
                tuning: Tune(castTime: ChargeTime),
                Apply(EffectRecipientEnum.Self, BundleRefresh(ChargeTime, Status(ModifierEnum.ManaBlocked)))
            );

            SkillPart slash = Part(registry,
                source: ActionSourceEnum.Self,
                action: TemplateActionEnum.Cast,
                target: AimTargetEnum.Current,
                Damage(EffectRecipientEnum.SameToAimTarget, (StatEnum.ATK, DamageRatio))
            );

            return Flow(onStart: Together(charge, focus), onExpired: slash);
        }
    }
}
