using System.Collections.Generic;
using UnityEngine;
using MagicSchool.Contracts;
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

        // every ally on his Guard mark makes the brace this much longer, and its heal this much more in all (% AP)
        // e.g.     2 allies on it => a 4 second brace healing 300% AP
        private const string GuardMark = "Guard";
        public override IReadOnlyList<string> SpecialMarks => new[] { GuardMark };
        private static float GuardSeconds => Tuned(1f);
        private static float GuardHeal => Tuned(50f);

        // this cast's brace, set in OnCast from the allies on his Guard mark (fixed when the fight starts, like every mark)
        private int _guards;
        private float _braceSeconds = BraceDuration;
        private Tuning _braceTuning;
        private HealSkillEffect _braceHeal;

        // the damage he took (HP lost, after DF and his damage reduction) since the brace began, and when it ends
        private float _takenWhileBracing;
        private float _braceEndsAt = float.NegativeInfinity;

        public static SkillDefinition Build(TemplateActionRegistrySO registry) => new ShieldKnightSkill(registry);

        private ShieldKnightSkill(TemplateActionRegistrySO registry) : base(registry) { }

        public override string SkillName => "Guardian's Roar";

        public override string Description
            => $"Braces for {BraceDuration} seconds, healing steadily for {AP(HealAmount)} and taking "
             + $"{DamageReductionPercent}% less damage. The moment the brace ends he slams the ground, dealing every enemy "
             + $"around him {AP(SlamReturnPerAP)}% of the damage he took while bracing.";

        public override string PassiveDescription
            => $"Every ally on his {GuardMark} mark makes his brace {GuardSeconds} second longer and its heal {AP(GuardHeal)} more.";

        // shows the passive text in the inspector (there is no auto attack flow to play)
        public override bool HasAutoAttackPassive => true;

        // ============================== active ==============================
        // the brace begins: as long as his guards make it, and start counting what he takes
        public override bool OnCast()
        {
            _guards = Caster is IAligned aligned ? aligned.AlliesOnSpecialMark(GuardMark).Count : 0;
            _braceSeconds = BraceDuration + GuardSeconds * _guards;
            _braceTuning.CastTime = _braceSeconds;
            _braceHeal.Cadence.cadenceDuration = _braceSeconds;

            if (!base.OnCast()) return false;

            _takenWhileBracing = 0f;
            _braceEndsAt = Time.time + _braceSeconds;
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

        private SkillPart Brace(TemplateActionRegistrySO registry)
        {
            _braceTuning = Tune(castTime: BraceDuration);

            // built for the 2 second brace; a longer one ticks more often and each tick is scaled so the whole heal is
            // HealAmount + GuardHeal per guard (See HealPerTickScale)
            _braceHeal = HealOverTime(
                recipient: EffectRecipientEnum.Self,
                duration: BraceDuration,
                interval: TickInterval,
                ratios: (StatEnum.AP, HealAmount)
            ).AmplifiedBy(_ => HealPerTickScale() - 1f);

            SkillPart brace = Part(
                registry: registry,
                source: ActionSourceEnum.Self,
                action: TemplateActionEnum.Cast,
                target: AimTargetEnum.Self,
                tuning: _braceTuning,

                _braceHeal,

                Apply(
                    recipient: EffectRecipientEnum.Self,
                    modifier: BundleRefresh(
                        duration: () => _braceSeconds,
                        modifiers: Buff(
                            modifier: ModifierEnum.DamageReduction,
                            ratios: DamageReductionPercent))
                )
            );

            return brace;
        }

        // A tick heals HealAmount / (BraceDuration / TickInterval) as built. Over a brace of _braceSeconds the whole heal is
        // that x _braceSeconds / BraceDuration, so to land on HealAmount + GuardHeal x guards each tick is scaled by:
        // e.g.     no guard  => 1 (200% AP over 2s)        2 guards => 300 x 2 / (200 x 4) = 0.75 (300% AP over 4s)
        private float HealPerTickScale()
            => BraceDuration * (HealAmount + GuardHeal * _guards) / (HealAmount * _braceSeconds);

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
