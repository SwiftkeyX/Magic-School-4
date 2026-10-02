using MagicSchool.Contracts;
using static MagicSchool.Skills.SkillFactory;

namespace MagicSchool.Skills
{
    internal class RogueSkill : SkillDefinition
    {
        private const int Shadowsteps = 3;

        // dash randomly
        private const int DashRange = 1;
        private const float DashDuration = 0.15f;

        // charge into cluster
        private const int ChargeRange = 4;
        private const float ChargeDuration = 0.3f;
        private const float HitboxHalfWidth = 1.25f;
        private const float CollideDamage = 200f;

        public static SkillDefinition Build(TemplateActionRegistrySO registry) => new RogueSkill(registry);

        private RogueSkill(TemplateActionRegistrySO registry) : base(registry) { }

        public override string SkillName => "Shadowstep";

        public override string Description
            => $"Shadowsteps {Shadowsteps} times. Each step: dashes {DashRange} hex in a random direction, then "
             + $"dashes along the densest line of enemies, dealing {CollideDamage}% AP to everyone he passes.";

        // ============================== active ==============================
        protected override SkillFlow BuildActiveFlow(TemplateActionRegistrySO registry)
            => Repeat(
                Flow(
                    onStart: Dash(registry), 
                    onExpired: Together(Charge(registry), Hitbox(registry))
                ),
                times: Shadowsteps);

        private static SkillPart Dash(TemplateActionRegistrySO registry)
        {
            SkillPart dash = Part(
                registry: registry,
                source: ActionSourceEnum.Self,
                action: TemplateActionEnum.Move,
                target: AimTargetEnum.Random,
                tuning: TuneMove(range: DashRange, duration: DashDuration)
            );

            return dash;
        }

        private static SkillPart Charge(TemplateActionRegistrySO registry)
        {
            SkillPart charge = Part(
                registry: registry,
                source: ActionSourceEnum.Self,
                action: TemplateActionEnum.Move,
                target: AimTargetEnum.ClusteredLaser,
                tuning: TuneMove(range: ChargeRange, duration: ChargeDuration, spread: HitboxHalfWidth)
            );

            return charge;
        }

        private static SkillPart Hitbox(TemplateActionRegistrySO registry)
        {
            SkillPart hitbox = Part(
                registry: registry,
                source: ActionSourceEnum.Self,
                action: TemplateActionEnum.HalfCircleAOESticky,
                target: AimTargetEnum.Self,
                tuning: TuneAOE(sticky: true),

                Damage(
                    recipient: EffectRecipientEnum.EnemiesInPath,
                    ratios: (StatEnum.AP, CollideDamage)
                )
            );

            return hitbox;
        }
    }
}
