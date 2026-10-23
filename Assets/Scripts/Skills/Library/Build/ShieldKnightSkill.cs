using System.Collections.Generic;
using UnityEngine;
using MagicSchool.Contracts;
using MagicSchool.StatScaling;
using static MagicSchool.Skills.SkillFactory;

namespace MagicSchool.Skills
{
    internal class ShieldKnightSkill : SkillDefinition
    {
        private static float BraceDuration => Tuned(2f);
        private static float TickInterval => Tuned(0.5f);
        private static float HealAmount => Tuned(200f);
        private static float DamageReductionPercent => Tuned(25f);

        // the slam gives back this % of AP, as a % of the damage he took while bracing
        // e.g.     30 at 100 AP => the slam deals 30% of what he took; at 200 AP, 60%
        private static float SlamReturnPerAP => Tuned(30f);

        // the damage he took (HP lost, after DF and his damage reduction) since the brace began, and when it ends
        private float _takenWhileBracing;
        private float _braceEndsAt = float.NegativeInfinity;

        // the allies standing on this mark feed him: their damage heals him (See AlignMark)
        private const string GuardMark = "Guard";
        public override IReadOnlyList<string> SpecialMarks => new[] { GuardMark };
        private static float HealFromAllyDamage => Tuned(10f);

        public static SkillDefinition Build(TemplateActionRegistrySO registry) => new ShieldKnightSkill(registry);

        private ShieldKnightSkill(TemplateActionRegistrySO registry) : base(registry) { }

        public override string SkillName => "Guardian's Roar";

        public override string Description
            => $"Braces for {BraceDuration} seconds, healing steadily for {HealAmount}% AP and taking "
             + $"{DamageReductionPercent}% less damage. The moment the brace ends he slams the ground, dealing every enemy "
             + $"around him ({SlamReturnPerAP}% AP)% of the damage he took while bracing.";

        public override string PassiveDescription
            => $"Heals for {HealFromAllyDamage}% of the damage the allies on his {GuardMark} mark deal.";

        // ============================== passive ==============================
        public override bool HasAutoAttackPassive => true;

        public override void OnMarkedAllyDealtDamage(string mark, ICombatant ally, DamageEvent hit)
        {
            if (mark != GuardMark) return;

            Omnivamp.HealFromDamage(Caster, hit.Outcome.Landed, HealFromAllyDamage, source: ally);
        }

        // ============================== active ==============================
        // the brace begins: start counting what he takes
        public override bool OnCast()
        {
            if (!base.OnCast()) return false;

            _takenWhileBracing = 0f;
            _braceEndsAt = Time.time + BraceDuration;
            return true;
        }

        public override void OnDamageTaken(DamageEvent hit)
        {
            if (Time.time <= _braceEndsAt) _takenWhileBracing += hit.Outcome.Landed;
        }

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

        // (SlamReturnPerAP% AP)% of the damage taken while bracing: the ratio gives SlamReturnPerAP% of AP as a fraction
        // (30% x 100 AP / 100 = 0.3), and the amplifier multiplies it by the damage taken (an amplifier of n - 1 is x n)
        private SkillPart Slam(TemplateActionRegistrySO registry)
        {
            SkillPart slam = Part(
                registry: registry,
                source: ActionSourceEnum.Self,
                action: TemplateActionEnum.CircleAOE,
                target: AimTargetEnum.Self,

                Damage(
                    recipient: EffectRecipientEnum.EnemiesInArea,
                    ratios: (StatEnum.AP, SlamReturnPerAP / 100f)
                ).AmplifiedBy(_ => _takenWhileBracing - 1f)
            );

            return slam;
        }
    }
}
