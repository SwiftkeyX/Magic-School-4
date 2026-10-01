using System.Collections.Generic;
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

        // the first step carries the cast time for the whole chain: he is busy until the last charge lands.
        // Each move ends on a frame, a little after its duration, so the six of them run longer than
        // their sum - the slack keeps him from walking off mid-chain.
        private const float SlackPerMove = 0.5f;
        private const float WholeCast = Shadowsteps * (DashDuration + ChargeDuration + SlackPerMove * 2f);

        public static SkillDefinition Build(TemplateActionRegistrySO registry) => new RogueSkill(registry);

        private RogueSkill(TemplateActionRegistrySO registry) : base(registry) { }

        public override string SkillName => "Shadowstep";

        public override string Description
            => $"Shadowsteps {Shadowsteps} times. Each step: dashes {DashRange} hex in a random direction, then "
             + $"dashes along the densest line of enemies, dealing {CollideDamage}% AP to everyone he passes.";

        // ============================== active ==============================
        // dash -> charge (+ its hitbox), three times over. Each one starts when the one before lands.
        protected override List<SkillStep> Active(TemplateActionRegistrySO registry)
        {
            var steps = new List<SkillStep>();

            for (int i = 0; i < Shadowsteps; i++)
            {
                bool isFirst = i == 0;

                steps.Add(Dash(registry, isFirst));
                steps.Add(Charge(registry));
                steps.Add(Hitbox(registry));
            }

            return steps;
        }

        private static SkillStep Dash(TemplateActionRegistrySO registry, bool isFirst)
        {
            SkillActionGroup dash = ActionGroup(
                registry: registry,
                source: ActionSourceEnum.Self,
                action: TemplateActionEnum.Move,
                target: AimTargetEnum.Random,
                tuning: TuneMove(
                    castTime: WholeCast,
                    range: DashRange,
                    duration: DashDuration)
            );

            return Step(trigger: isFirst ? TriggerEnum.OnCast : TriggerEnum.OnExpired, groups: dash);
        }

        private static SkillStep Charge(TemplateActionRegistrySO registry)
        {
            SkillActionGroup charge = ActionGroup(
                registry: registry,
                source: ActionSourceEnum.Self,
                action: TemplateActionEnum.Move,
                target: AimTargetEnum.ClusteredLaser,
                tuning: TuneMove(range: ChargeRange, duration: ChargeDuration, spread: HitboxHalfWidth)
            );

            return Step(trigger: TriggerEnum.OnExpired, groups: charge);
        }

        // rides the charge before it - lasts as long as the charge does, and no longer
        private static SkillStep Hitbox(TemplateActionRegistrySO registry)
        {
            SkillActionGroup hitbox = ActionGroup(
                registry: registry,
                source: ActionSourceEnum.Self,
                action: TemplateActionEnum.HalfCircleAOESticky,
                target: AimTargetEnum.Self,
                tuning: TuneAOE(sticky: true, duration: ChargeDuration),

                Damage(
                    recipient: EffectRecipientEnum.EnemiesInPath,
                    ratios: (StatEnum.AP, CollideDamage)
                )
            );

            return Step(TriggerEnum.OnCastStart, hitbox);
        }
    }
}
