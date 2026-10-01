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
            => $"Gains {FlatBonusHP} + {BonusHPFromAP}% AP bonus health for the rest of the fight.";

        public override string PassiveDescription
            => $"Auto attacks deal an extra {MaxHPPerAttack}% of his max HP.";

        // ============================== active ==============================
        protected override List<SkillFlow> Active(TemplateActionRegistrySO registry)
        {
            // FLAGGING: the sheet says this stacks every cast, but the same modifier instance is
            // refreshed rather than added again, so a second cast gives nothing more. Needs the
            // stack-vs-refresh rule in ModifierResolver.
            SkillPart skulls = Part(registry,
                source: ActionSourceEnum.Self,
                action: TemplateActionEnum.Cast,
                target: AimTargetEnum.Self,
                Apply(EffectRecipientEnum.Self,
                      Bundle(
                        BonusDuration,
                        Buff(
                            ModifierEnum.BonusHP,
                            FlatBonusHP,
                            (StatEnum.AP, BonusHPFromAP)
                        )
                ))
            );

            return new List<SkillFlow> { Flow(trigger: TriggerEnum.OnCast, groups: skulls) };
        }

        // ============================== passive ==============================
        protected override List<SkillFlow> Passive(TemplateActionRegistrySO registry)
        {
            // every auto attack also hits the target for a share of his own max HP
            SkillPart onAttack = Part(registry,
                source: ActionSourceEnum.Self,
                action: TemplateActionEnum.Cast,
                target: AimTargetEnum.Current,
                Damage(
                    EffectRecipientEnum.SameToAimTarget,
                    (StatEnum.MaxHP, MaxHPPerAttack)
                )
            );

            return new List<SkillFlow> { Flow(trigger: TriggerEnum.OnAttack, groups: onAttack) };
        }
    }
}
