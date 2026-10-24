using System.Collections.Generic;
using MagicSchool.Contracts;
using static MagicSchool.Skills.SkillFactory;

namespace MagicSchool.Skills
{
    internal class EttinSkill : SkillDefinition
    {
        private static float FlatBonusHP => Tuned(100f);
        private static float BonusHPFromAP => Tuned(200f);
        private static float BonusDuration => Tuned(-1f);    // the rest of the fight
        private static float MaxHPPerAttackFromAP => Tuned(5f);   // % of his max HP per 100 AP: 5% at 100 AP, 10% at 200

        public static SkillDefinition Build(TemplateActionRegistrySO registry) => new EttinSkill(registry);

        private EttinSkill(TemplateActionRegistrySO registry) : base(registry) { }

        public override string SkillName => "Thick Skulls";

        public override string Description
            => $"Gains {FlatBonusHP} + {AP(BonusHPFromAP)} bonus health for the rest of the fight. "
             + "Stacks every cast.";

        public override string PassiveDescription
            => $"Auto attacks deal an extra {AP(MaxHPPerAttackFromAP)}% of his max HP as damage.";

        // ============================== active ==============================
        protected override SkillFlow BuildActiveFlow(TemplateActionRegistrySO registry)
        {
            // a stack bundle, so every cast adds its bonus health on top of the last
            SkillPart skulls = Part(registry,
                source: ActionSourceEnum.Self,
                action: TemplateActionEnum.Cast,
                target: AimTargetEnum.Self,
                Apply(EffectRecipientEnum.Self,
                      BundleStack(
                        BonusDuration,
                        Buff(
                            ModifierEnum.BonusHP,
                            FlatBonusHP,
                            (StatEnum.AP, BonusHPFromAP)
                        )
                ))
            );

            return Flow(onStart: skulls);
        }

        // ============================== passive ==============================
        protected override SkillFlow BuildAutoAttackFlow(TemplateActionRegistrySO registry)
        {
            // every auto attack also hits the target for a share of his own max HP, and the share grows with his AP.
            // "max HP x AP" is a product, which a ratio cannot write, so the HP ratio is multiplied by AP / 100 at each hit.
            SkillPart onAutoAttack = Part(registry,
                source: ActionSourceEnum.Self,
                action: TemplateActionEnum.Cast,
                target: AimTargetEnum.Current,
                Damage(
                    EffectRecipientEnum.SameToAimTarget,
                    (StatEnum.MaxHP, MaxHPPerAttackFromAP)
                ).AmplifiedBy(PerHundredAP)
            );

            return Flow(onStart: onAutoAttack);
        }

        // AmplifiedBy adds to 1x, so 100 AP is +0 (the ratio as it is), 200 AP is +1 (double), 0 AP is -1 (nothing)
        private static float PerHundredAP(IEffectable caster)
            => caster is IHeroStats stats ? stats.Magic / 100f - 1f : 0f;
    }
}
