using System.Collections.Generic;
using UnityEngine;
using MagicSchool.Contracts;
using static MagicSchool.Skills.SkillFactory;

namespace MagicSchool.Skills
{
    // The brace lasts BraceDuration plus GuardSeconds per ally on his Guard mark (counted in OnCast, fixed at fight
    // start like every mark), and heals HealAmount plus GuardHeal per guard in all: 2 guards => a 4s brace healing
    // 300% AP. The heal is built for the 2s brace, so on a longer one each tick is scaled to land on that total
    // (HealPerTickScale; 2 guards => 300 x 2 / (200 x 4) = 0.75).
    // It counts the HP he loses while bracing (after DF and his damage reduction); the slam returns SlamReturnPerAP%
    // of it per 100 AP (30 at 100 AP => 30%, 60% at 200). The ratio gives the fraction of AP and the amplifier
    // multiplies it by the damage taken (an amplifier of n - 1 is x n).
    internal class ShieldKnightSkill : SkillDefinition
    {
        private static float BraceDuration => Tuned(2f);
        private static float TickInterval => Tuned(0.5f);
        private static float HealAmount => Tuned(200f);
        private static float DamageReductionPercent => Tuned(25f);

        private static float SlamReturnPerAP => Tuned(30f);

        private const string GuardMark = "Guard";
        public override IReadOnlyList<string> SpecialMarks => new[] { GuardMark };
        private static float GuardSeconds => Tuned(1f);
        private static float GuardHeal => Tuned(50f);

        private int _guards;
        private float _braceSeconds = BraceDuration;
        private Tuning _braceTuning;
        private HealSkillEffect _braceHeal;

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

        public override bool HasAutoAttackPassive => true;

        public override bool OnCast()
        {
            _guards = Caster?.AlliesOnSpecialMark(GuardMark).Count ?? 0;
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

        private float HealPerTickScale()
            => BraceDuration * (HealAmount + GuardHeal * _guards) / (HealAmount * _braceSeconds);

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
