using System.Collections.Generic;
using MagicSchool.Contracts;
using static MagicSchool.Skills.SkillFactory;

namespace MagicSchool.Skills
{
    internal class EttinSkill : SkillDefinition
    {
        private const float FlatBonusHP = 250f;
        private const float BonusHPFromAP = 100f;
        private const float BonusDuration = -1f;    // the rest of the fight
        private const float MaxHPPerAttack = 5f;

        public static SkillDefinition Build(TemplateActionRegistrySO registry) => new EttinSkill(registry);

        private EttinSkill(TemplateActionRegistrySO registry) : base(registry) { }

        public override string SkillName => "Thick Skulls";

        public override string Description
            => $"Gains {FlatBonusHP} + {BonusHPFromAP}% AP bonus health for the rest of the fight. "
             + "Stacks every cast.";

        public override string PassiveDescription
            => $"Auto attacks deal an extra {MaxHPPerAttack}% of his max HP.";

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
            // every auto attack also hits the target for a share of his own max HP
            SkillPart onAutoAttack = Part(registry,
                source: ActionSourceEnum.Self,
                action: TemplateActionEnum.Cast,
                target: AimTargetEnum.Current,
                Damage(
                    EffectRecipientEnum.SameToAimTarget,
                    (StatEnum.MaxHP, MaxHPPerAttack)
                )
            );

            return Flow(onStart: onAutoAttack);
        }
    }
}
