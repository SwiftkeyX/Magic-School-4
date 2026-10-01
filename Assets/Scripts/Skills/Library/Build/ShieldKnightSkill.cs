using System.Collections.Generic;
using MagicSchool.Contracts;
using static MagicSchool.Skills.SkillFactory;

namespace MagicSchool.Skills
{
    internal class ShieldKnightSkill : SkillDefinition
    {
        private const float BraceDuration = 2f;
        private const float TickInterval = 0.5f;
        private const float HealAmount = 200f;
        private const float SlamDamage = 120f;
        private const float DamageReductionPercent = 25f;

        public static SkillDefinition Build(TemplateActionRegistrySO registry) => new ShieldKnightSkill(registry);

        private ShieldKnightSkill(TemplateActionRegistrySO registry) : base(registry) { }

        public override string SkillName => "Guardian's Roar";

        public override string Description
            => $"Braces for {BraceDuration} seconds, healing steadily for {HealAmount} and taking "
             + $"{DamageReductionPercent}% less damage. The moment the brace ends he slams the ground "
             + $"for {SlamDamage}% AP to every enemy around him.";

        // ============================== active ==============================
        protected override List<SkillFlow> Active(TemplateActionRegistrySO registry)
        {
            return new List<SkillFlow> { Brace(registry), Slam(registry) };
        }

        private static SkillFlow Brace(TemplateActionRegistrySO registry)
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
                    modifier: Bundle(
                        duration: BraceDuration,
                        modifiers: Buff(
                            modifier: ModifierEnum.DamageReduction,
                            ratios: DamageReductionPercent))
                )
            );

            return Flow(trigger: TriggerEnum.OnCast, groups: brace);
        }

        private static SkillFlow Slam(TemplateActionRegistrySO registry)
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

            return Flow(trigger: TriggerEnum.OnExpired, groups: slam);
        }
    }
}
