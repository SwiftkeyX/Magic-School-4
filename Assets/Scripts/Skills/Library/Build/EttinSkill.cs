using System.Collections.Generic;
using MagicSchool.Contracts;
using static MagicSchool.Skills.SkillFactory;

namespace MagicSchool.Skills
{
    // His max HP bonus is a stack bundle, so each cast adds on top. Every auto attack also hits for a share of his max
    // HP that grows with AP (5% per 100 AP); "max HP x AP" is a product a ratio cannot write, so the HP ratio is Scaled by AP / 100 at each hit
    // (100 AP x1, 200 AP x2, 0 AP nothing).
    internal class EttinSkill : SkillDefinition
    {
        private static float FlatBonusHP => Tuned(100f);
        private static float BonusHPFromAP => Tuned(200f);
        private static float BonusDuration => Tuned(-1f);
        private static float MaxHPPerAttackFromAP => Tuned(5f);

        public static SkillDefinition Build(TemplateActionRegistrySO registry) => new EttinSkill(registry);

        private EttinSkill(TemplateActionRegistrySO registry) : base(registry) { }

        public override string SkillName => "Thick Skulls";

        public override string Description
            => $"Gains {FlatBonusHP} + {AP(BonusHPFromAP)} bonus health for the rest of the fight. "
             + "Stacks every cast.";

        public override string PassiveDescription
            => $"Auto attacks deal an extra {AP(MaxHPPerAttackFromAP)}% of his max HP as damage.";

        protected override SkillFlow BuildActiveFlow(TemplateActionRegistrySO registry)
        {
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

        protected override SkillFlow BuildAutoAttackFlow(TemplateActionRegistrySO registry)
        {
            SkillPart onAutoAttack = Part(registry,
                source: ActionSourceEnum.Self,
                action: TemplateActionEnum.Cast,
                target: AimTargetEnum.Current,
                Damage(
                    EffectRecipientEnum.SameToAimTarget,
                    (StatEnum.MaxHP, MaxHPPerAttackFromAP)
                ).Scaled(PerHundredAP)
            );

            return Flow(onStart: onAutoAttack);
        }

        private static float PerHundredAP(IEffectable caster)
            => caster is IHeroStats stats ? stats.Magic / 100f : 1f;
    }
}
