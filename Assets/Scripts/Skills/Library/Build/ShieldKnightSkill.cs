using System.Collections.Generic;
using MagicSchool.Contracts;
using static MagicSchool.Skills.SkillFactory;

namespace MagicSchool.Skills
{
    internal class ShieldKnightSkill : SkillDefinition
    {
        private static float BraceDuration => Tuned(2f);
        private static float TickInterval => Tuned(0.5f);
        private static float HealAmount => Tuned(200f);
        private static float SlamDamage => Tuned(120f);
        private static float DamageReductionPercent => Tuned(25f);

        public static SkillDefinition Build(TemplateActionRegistrySO registry) => new ShieldKnightSkill(registry);

        private ShieldKnightSkill(TemplateActionRegistrySO registry) : base(registry) { }

        public override string SkillName => "Guardian's Roar";

        public override string Description
            => $"Braces for {BraceDuration} seconds, healing steadily for {HealAmount} and taking "
             + $"{DamageReductionPercent}% less damage. The moment the brace ends he slams the ground "
             + $"for {SlamDamage}% AP to every enemy around him.";

        // ============================== active ==============================
        protected override SkillFlow BuildActiveFlow(TemplateActionRegistrySO registry)
        {
            return Flow(onStart: Brace(registry), onExpired: Slam(registry));
        }

        private static SkillPart Brace(TemplateActionRegistrySO registry)
        {
            SkillPart brace = Part(
                registry: registry,
                source: ActionSourceEnum.Self,
                action: TemplateActionEnum.Cast,
                target: AimTargetEnum.Self,
                tuning: Tune(castTime: BraceDuration),

                HealOverTime(
                    recipient: EffectRecipientEnum.Self,
                    duration: BraceDuration,
                    interval: TickInterval,
                    ratios: (StatEnum.AP, HealAmount)),

                Apply(
                    recipient: EffectRecipientEnum.Self,
                    modifier: BundleRefresh(
                        duration: BraceDuration,
                        modifiers: Buff(
                            modifier: ModifierEnum.DamageReduction,
                            ratios: DamageReductionPercent))
                )
            );

            return brace;
        }

        private static SkillPart Slam(TemplateActionRegistrySO registry)
        {
            SkillPart slam = Part(
                registry: registry,
                source: ActionSourceEnum.Self,
                action: TemplateActionEnum.CircleAOE,
                target: AimTargetEnum.Self,

                Damage(
                    recipient: EffectRecipientEnum.EnemiesInArea,
                    ratios: (StatEnum.AP, SlamDamage))
            );

            return slam;
        }
    }
}
