using System.Collections.Generic;
using MagicSchool.Contracts;
using static MagicSchool.Skills.SkillFactory;

namespace MagicSchool.Skills
{
    internal class KnightSkill : SkillDefinition
    {
        private static float StunDuration => Tuned(2f);
        private static float LandingDamage => Tuned(200f);
        private static float LandingDiameter => Tuned(4.5f);
        private static int JumpRange => Tuned(4);

        private static float LandingRadius => LandingDiameter / 2f;

        public static SkillDefinition Build(TemplateActionRegistrySO registry) => new KnightSkill(registry);

        private KnightSkill(TemplateActionRegistrySO registry) : base(registry) { }

        public override string SkillName => "Valiant Leap";

        public override string Description
            => "Leaps into the densest part of the enemy formation. The landing deals "
             + $"{AP(LandingDamage)} to everyone caught around him and leaves them stunned for "
             + $"{StunDuration} seconds.";

        // ============================== active ==============================
        protected override SkillFlow BuildActiveFlow(TemplateActionRegistrySO registry)
            => Flow(onStart: Jump(registry), onExpired: Landing(registry));

        private static SkillPart Jump(TemplateActionRegistrySO registry)
        {
            SkillPart jump = Part(
                registry: registry,
                source: ActionSourceEnum.Self,
                action: TemplateActionEnum.Move,
                target: AimTargetEnum.ClusteredCircle,
                tuning: TuneMove(range: JumpRange, spread: LandingRadius)
            );

            return jump;
        }

        private static SkillPart Landing(TemplateActionRegistrySO registry)
        {
            ICustomModifier stun = BundleRefresh(
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

            return landing;
        }
    }
}