using System.Collections.Generic;
using MagicSchool.Contracts;
using MagicSchool.StatScaling;
using MagicSchool.Modifiers;

namespace MagicSchool.Skills
{
    // The skill DSL. Damage(recipient, (StatEnum.ATK, 1000f)) is 1000% AD; a ratio with no stat is flat. Skill damage
    // does not crit unless built with the crit variant. DamageOverTime / burn split their ratio across the ticks, so
    // pass the total. Amplified variants add the amplifier when every condition holds (Naga: +30% to a wounded
    // target).
    // Buff(stat, 20f) is flat; Buff(stat, (StatEnum.AP, 50f)) is 50% of the caster's AP; the From variant scales off
    // another unit's stats (Dryad: +25% of the ally's own AS). Debuff lowers. Shield(500f) or Shield((StatEnum.DF,
    // 300f)). A Bundle is modifiers sharing one duration (-1 = the rest of the fight): BundleRefresh replaces the old
    // one and restarts its timer, BundleStack adds another with its own, and a Func duration is read at every use
    // (Dire Wolf's growing stun). A fresh bundle per event is how a skill stacks gains.
    // Flow(onStart, onHit, onExpired): one skill; onHit plays when onStart hits, onExpired when it expires, and
    // nothing follows them. Together(a, b) starts parts at once and only the first, the lead, decides what plays next.
    // Repeat(flow, times) or a Func of rounds. Tune lengths and widths are world units (a circle 4.5 across is 4.5 by
    // 4.5).
    internal static class SkillFactory
    {
        public static AttackSkillEffect Damage(EffectRecipientEnum recipient, params StatRatio[] ratios)
            => new AttackSkillEffect(recipient, ratios);

        public static AttackSkillEffect DamageWhen(EffectRecipientEnum recipient, List<SkillCondition> conditions,
                                                   float amplifier, params StatRatio[] ratios)
            => new AttackSkillEffect(recipient, ratios, conditions: conditions, amplifier: amplifier);

        public static AttackSkillEffect DamageCanCrit(EffectRecipientEnum recipient, params StatRatio[] ratios)
            => new AttackSkillEffect(recipient, ratios, canCrit: true);

        public static AttackSkillEffect DamageOverTime(EffectRecipientEnum recipient, float interval, float duration,
                                                       params StatRatio[] ratios)
            => new AttackSkillEffect(recipient, ratios, new Cadence(interval, duration));

        public static AttackSkillEffect Burn(EffectRecipientEnum recipient, float interval, float duration,
                                             params StatRatio[] ratios)
            => new AttackSkillEffect(recipient, ratios, new Cadence(interval, duration), kind: DamageKindEnum.Burn);

        public static HealSkillEffect Heal(EffectRecipientEnum recipient, params StatRatio[] ratios)
            => new HealSkillEffect(recipient, ratios);

        public static HealSkillEffect HealOverTime(EffectRecipientEnum recipient, float duration, float interval,
                                                   params StatRatio[] ratios)
            => new HealSkillEffect(recipient, ratios, duration, new Cadence(interval, duration));

        public static ModifierSkillEffect Apply(EffectRecipientEnum recipient, ICustomModifier modifier, float amplifier = 0f)
            => new ModifierSkillEffect(recipient, modifier, amplifier: amplifier);

        public static ModifierSkillEffect ApplyOverTime(EffectRecipientEnum recipient, float interval, float duration,
                                                        ICustomModifier modifier)
            => new ModifierSkillEffect(recipient, modifier, new Cadence(interval, duration));

        public static ModifierSkillEffect ApplyWhen(EffectRecipientEnum recipient, ICustomModifier modifier,
                                                    List<SkillCondition> conditions, float amplifier)
            => new ModifierSkillEffect(recipient, modifier, conditions: conditions, amplifier: amplifier);

        public static ICustomModifier BundleRefresh(float duration, params IModifier[] modifiers)
            => new CustomModifier(duration, modifiers, isStack: false);
        
        public static ICustomModifier BundleStack(float duration, params IModifier[] modifiers)
            => new CustomModifier(duration, modifiers, isStack: true);

        public static ICustomModifier BundleRefresh(System.Func<float> duration, params IModifier[] modifiers)
            => new CustomModifier(duration, modifiers, isStack: false);

        public static ICustomModifier BundleStack(System.Func<float> duration, params IModifier[] modifiers)
            => new CustomModifier(duration, modifiers, isStack: true);

        public static IModifier Buff(ModifierEnum modifier, params StatRatio[] ratios)
            => new StatModifier(modifier, ratios);

        public static IModifier Buff(ModifierEnum modifier, ScalingSourceEnum source, params StatRatio[] ratios)
            => new StatModifier(modifier, ratios, source);

        public static IModifier Debuff(ModifierEnum modifier, params StatRatio[] ratios)
            => new StatModifier(modifier, Negate(ratios));

        public static IModifier Debuff(ModifierEnum modifier, ScalingSourceEnum source, params StatRatio[] ratios)
            => new StatModifier(modifier, Negate(ratios), source);

        private static StatRatio[] Negate(StatRatio[] ratios)
        {
            var negated = new StatRatio[ratios.Length];
            for (int i = 0; i < ratios.Length; i++)
            {
                StatRatio ratio = ratios[i];
                negated[i] = ratio.IsFlat
                    ? new StatRatio(-ratio.Amount)
                    : new StatRatio(ratio.Stat.Value, -ratio.Amount, ratio.ScaleFrom ?? ScaleFromEnum.Total);
            }
            return negated;
        }

        public static IModifier Status(ModifierEnum status)
            => new StatusModifier(status);

        public static IModifier Shield(params StatRatio[] ratios)
            => new StatModifier(ModifierEnum.Shield, ratios);

        public static SkillPart Part(TemplateActionRegistrySO registry, ActionSourceEnum source,
                                                   TemplateActionEnum action, AimTargetEnum target,
                                                   params SkillEffect[] effects)
            => Group(registry, source, action, target, conditions: null, tuning: null, effects: effects);

        public static SkillPart Part(TemplateActionRegistrySO registry, ActionSourceEnum source,
                                                   TemplateActionEnum action, AimTargetEnum target,
                                                   Tuning tuning,
                                                   params SkillEffect[] effects)
            => Group(registry, source, action, target, conditions: null, tuning: tuning, effects: effects);

        public static SkillPart PartWhen(TemplateActionRegistrySO registry, ActionSourceEnum source,
                                                       TemplateActionEnum action, AimTargetEnum target,
                                                       List<SkillCondition> conditions, Tuning tuning = null,
                                                       params SkillEffect[] effects)
            => Group(registry, source, action, target, conditions: conditions, tuning: tuning, effects: effects);

        private static SkillPart Group(TemplateActionRegistrySO registry, ActionSourceEnum source,
                                              TemplateActionEnum action, AimTargetEnum target,
                                              List<SkillCondition> conditions, Tuning tuning,
                                              SkillEffect[] effects)
            => new SkillPart(source, registry.Get(action), target,
                                    conditions: conditions, effects: new List<SkillEffect>(effects), tuning: tuning);

        public static SkillFlow Flow(PartSet onStart, PartSet onHit = default, PartSet onExpired = default)
            => new SkillFlow(onStart.Parts, onHit.Parts, onExpired.Parts);

        public static PartSet Together(params SkillPart[] parts)
            => new PartSet(parts);

        public static SkillFlow Repeat(SkillFlow flow, int times)
            => new SkillFlow(flow.OnStart, flow.OnHit, flow.OnExpired, times);

        public static SkillFlow Repeat(SkillFlow flow, System.Func<int> times)
            => new SkillFlow(flow.OnStart, flow.OnHit, flow.OnExpired, times);

        public static Tuning Tune(float? castTime = null, bool? walkWhileCasting = null)
            => new Tuning { CastTime = castTime, WalkWhileCasting = walkWhileCasting };

        public static AOETuning TuneAOE(float? castTime = null, float? duration = null,
                                        float? length = null, float? width = null,
                                        bool? sticky = null, AOEOffsetEnum? offset = null, int? range = null,
                                        bool? walkWhileCasting = null)
            => new AOETuning { CastTime = castTime, Duration = duration, Length = length, Width = width,
                               Sticky = sticky, Offset = offset, Range = range, WalkWhileCasting = walkWhileCasting };

        public static MoveTuning TuneMove(float? castTime = null, int? range = null,
                                          float? duration = null, float? spread = null, bool? maxRange = null)
            => new MoveTuning { CastTime = castTime, Range = range, Duration = duration, Spread = spread,
                                MaxRange = maxRange };

        public static SummonTuning TuneSummon(SkillPart act, float? duration = null, float? interval = null,
                                              float? castTime = null)
            => new SummonTuning { Act = act, Duration = duration, Interval = interval, CastTime = castTime };

        public static ProjectileTuning TuneProjectile(float? castTime = null, int? range = null,
                                                      float? spread = null, float? size = null, float? falloff = null,
                                                      float? speed = null)
            => new ProjectileTuning { CastTime = castTime, Range = range, Spread = spread, Size = size, Falloff = falloff,
                                      Speed = speed };

        public static FireTimingRunnerTuning TuneFireTimingRunner(int count, FireTimingModeEnum mode,
                                                                   float interval = 0f, Tuning innerTuning = null,
                                                                   float? castTime = null, float? pauseAfter = null)
            => new FireTimingRunnerTuning { Count = count, Mode = mode, Interval = interval, InnerTuning = innerTuning,
                                            CastTime = castTime, PauseAfter = pauseAfter };

        public static FireTimingRunnerProjectileTuning TuneFireTimingRunnerProjectile(int count, FireTimingModeEnum mode,
                                                                   float interval = 0f, Tuning innerTuning = null,
                                                                   int? randomPoolRadius = null, float? castTime = null,
                                                                   float? pauseAfter = null)
            => new FireTimingRunnerProjectileTuning { Count = count, Mode = mode, Interval = interval,
                                                      InnerTuning = innerTuning, RandomPoolRadius = randomPoolRadius,
                                                      CastTime = castTime, PauseAfter = pauseAfter };
    }
}
