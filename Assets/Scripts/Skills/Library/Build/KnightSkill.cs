using System.Collections.Generic;
using MagicSchool.Contracts;
using static MagicSchool.Skills.SkillFactory;

namespace MagicSchool.Skills
{
    internal class KnightSkill : SkillDefinition
    {
        private const float StunDuration = 2f;
        private const float LandingDamage = 200f;
        private const float LandingDiameter = 4.5f;
        private const int JumpRange = 4;

        private const float LandingRadius = LandingDiameter / 2f;

        public static SkillDefinition Build(TemplateActionRegistrySO registry) => new KnightSkill(registry);

        private KnightSkill(TemplateActionRegistrySO registry) : base(registry) { }

        public override string SkillName => "Valiant Leap";

        public override string Description
            => "Leaps into the densest part of the enemy formation. The landing deals "
             + $"{LandingDamage}% AP to everyone caught around him and leaves them stunned for "
             + $"{StunDuration} seconds.";

        // ============================== active ==============================
        protected override List<SkillFlow> Active(TemplateActionRegistrySO registry)
            => new List<SkillFlow> { Jump(registry), Landing(registry) };

        private static SkillFlow Jump(TemplateActionRegistrySO registry)
        {
            SkillPart jump = Part(
                registry: registry,
                source: ActionSourceEnum.Self,
                action: TemplateActionEnum.Move,
                target: AimTargetEnum.ClusteredCircle,
                tuning: TuneMove(range: JumpRange, spread: LandingRadius)
            );

            return Flow(trigger: TriggerEnum.OnCast, groups: jump);
        }

        private static SkillFlow Landing(TemplateActionRegistrySO registry)
        {
            ICustomModifier stun = Bundle(
                duration: StunDuration,
                modifiers: (
                    Status(ModifierEnum.Stun)
                )
            );

            SkillPart landing = Part(
                registry: registry,
                source: ActionSourceEnum.Self,
                action: TemplateActionEnum.CircleAOE,
                target: AimTargetEnum.Self,
                tuning: TuneAOE(length: LandingDiameter, width: LandingDiameter),

                Damage(
                    recipient: EffectRecipientEnum.EnemiesInArea,
                    ratios: (StatEnum.AP, LandingDamage)
                ),
                Apply(
                    recipient: EffectRecipientEnum.EnemiesInArea,
                    modifier: stun
                )
            );

            return Flow(TriggerEnum.OnExpired, landing);
        }
    }
}