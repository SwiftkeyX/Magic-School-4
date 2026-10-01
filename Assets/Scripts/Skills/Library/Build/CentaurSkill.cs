

using System.Collections.Generic;
using MagicSchool.Contracts;
using static MagicSchool.Skills.SkillFactory;

namespace MagicSchool.Skills
{
    internal class CentaurSkill : SkillDefinition
    {
        private const int ChargeRange = 4;              // hexes he can cross
        private const float HitboxHalfWidth = 1.25f;
        private const float KnockedUpDuration = 2f;
        private const float CollideDamage = 200f;

        public static SkillDefinition Build(TemplateActionRegistrySO registry) => new CentaurSkill(registry);

        private CentaurSkill(TemplateActionRegistrySO registry) : base(registry) { }

        public override string SkillName => "Trample";

        public override string Description
            => $"Charges up to {ChargeRange} hexes straight through the enemy line, dealing "
             + $"{CollideDamage}% AP to everyone he ploughs into on the way and stunning them.";

        // ============================== active ==============================
        protected override SkillFlow BuildActiveFlow(TemplateActionRegistrySO registry)
            => Flow(onStart: Together(Move(registry), AOE(registry)));

        private static SkillPart Move(TemplateActionRegistrySO registry)
        {
            SkillPart move = Part(
                registry: registry,
                source: ActionSourceEnum.Self,
                action: TemplateActionEnum.Move,
                target: AimTargetEnum.ClusteredLaser,
                tuning: TuneMove(range: ChargeRange, spread: HitboxHalfWidth)
            );

            return move;
        }

        private static SkillPart AOE(TemplateActionRegistrySO registry)
        {
            ICustomModifier knockup = BundleRefresh(
                duration: KnockedUpDuration,
                modifiers: (
                    Status(ModifierEnum.Stun)
                )
            );

            // Make sure the AOE's lifetime is long enough until Move dies
            SkillPart charge = Part(
                registry: registry,
                source: ActionSourceEnum.Self,
                action: TemplateActionEnum.HalfCircleAOESticky,
                target: AimTargetEnum.Self,
                tuning: TuneAOE(sticky: true),

                Damage(
                    recipient: EffectRecipientEnum.EnemiesInPath,
                    ratios: (StatEnum.AP, CollideDamage)
                ),
                Apply(
                    recipient: EffectRecipientEnum.EnemiesInPath,
                    modifier: knockup
                )
            );

            return charge;
        }
    }
}