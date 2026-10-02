using System.Collections.Generic;
using MagicSchool.Contracts;
using MagicSchool.StatScaling;
using MagicSchool.Modifiers;

namespace MagicSchool.Skills
{
    internal static class SkillFactory
    {
        // ================================== damage ==================================
        // e.g. Damage(EnemiesInPath, (StatEnum.ATK, 1000f))  ->  1000% AD to everyone in the path
        public static AttackSkillEffect Damage(EffectRecipientEnum recipient, params StatRatio[] ratios)
            => new AttackSkillEffect(recipient, ratios);

        // the same to Damage(), but it can crit. Skill damage does not crit by default.
        // e.g.     Fencer's strikes can crit
        public static AttackSkillEffect DamageCanCrit(EffectRecipientEnum recipient, params StatRatio[] ratios)
            => new AttackSkillEffect(recipient, ratios, canCrit: true);

        // the same to Damage(), re-applied on a timer - Orc Blademaster's spin, Pip's patch
        public static AttackSkillEffect DamageOverTime(EffectRecipientEnum recipient, float interval, float duration,
                                                       params StatRatio[] ratios)
            => new AttackSkillEffect(recipient, ratios, new Cadence(interval, duration));

        // ================================== heal ==================================
        public static HealSkillEffect Heal(EffectRecipientEnum recipient, params StatRatio[] ratios)
            => new HealSkillEffect(recipient, ratios);

        public static HealSkillEffect HealOverTime(EffectRecipientEnum recipient, float duration, float interval,
                                                   params StatRatio[] ratios)
            => new HealSkillEffect(recipient, ratios, duration, new Cadence(interval, duration));

        // ================================== modifiers ==================================
        // apply a group of modifiers on the recipient
        public static ModifierSkillEffect Apply(EffectRecipientEnum recipient, ICustomModifier modifier, float amplifier = 0f)
            => new ModifierSkillEffect(recipient, modifier, amplifier: amplifier);

        // same to Apply(), but does apply over time
        public static ModifierSkillEffect ApplyOverTime(EffectRecipientEnum recipient, float interval, float duration,
                                                        ICustomModifier modifier)
            => new ModifierSkillEffect(recipient, modifier, new Cadence(interval, duration));

        // same to Apply(), but amplified only when every condition met 
        // e.g. Naga's skill is amplifield when the target is poison
        public static ModifierSkillEffect ApplyWhen(EffectRecipientEnum recipient, ICustomModifier modifier,
                                                    List<SkillCondition> conditions, float amplifier)
            => new ModifierSkillEffect(recipient, modifier, conditions: conditions, amplifier: amplifier);

        /// <summary>
        /// Bundle is a group of modifiers - everything in it shares one duration. 
        /// </summary>
        
        // the modifier is refresh if applied again
        public static ICustomModifier BundleRefresh(float duration, params IModifier[] modifiers)
            => new CustomModifier(duration, modifiers, isStack: false);
        
        // the modifier is stacked if applied again
        public static ICustomModifier BundleStack(float duration, params IModifier[] modifiers)
            => new CustomModifier(duration, modifiers, isStack: true);

        // the same to BundleRefresh() / BundleStack() above, but the duration is asked for every time the bundle was used
        // this make the duration can be adjust during combat after init once.
        //      BundleRefresh(() => CurrentStun, Status(Stun))    -> "a stun as long as CurrentStun is at that moment"
        //      e.g. DireWolf's stun is increased during the combat.
        public static ICustomModifier BundleRefresh(System.Func<float> duration, params IModifier[] modifiers)
            => new CustomModifier(duration, modifiers, isStack: false);

        public static ICustomModifier BundleStack(System.Func<float> duration, params IModifier[] modifiers)
            => new CustomModifier(duration, modifiers, isStack: true);

        // a modifier that gives a stat bonus:
        //   Buff(DamageReduction, 20f)                     -> "+20% DR"
        //   Buff(ATK, (StatEnum.AP, 50f))                  -> "Buff ATK = 50% of the caster's AP"
        //   Buff(DefendShred, 20f, (StatEnum.AP, 20f))     -> "Reduce DF = 20 flat, plus 20% AP on top"
        public static IModifier Buff(ModifierEnum modifier, params StatRatio[] ratios)
            => new StatModifier(modifier, ratios);

        // same to Buff() above, but: 
        // 1) buff will be given to other hero. 
        // 2) buff will derived from the "source" parameter (normally it would derived from the caster).
        //      e.g. Dryad's skill buff ally base on their attack speed by +25% => This mean the skill is derived from ally, not the caster itself.
        // another pattern, you should know:
        //      e.g. Dryad's skill (alternative) buff ally base on Dryad's AP by +50%AP => This mean the skill is derive from the caster
        public static IModifier Buff(ModifierEnum modifier, ScalingSourceEnum source, params StatRatio[] ratios)
            => new StatModifier(modifier, ratios, source);

        // counterpart to Buff(). this one reduce stat instead.
        //   Debuff(DefendShred, (StatEnum.AP, 20f))       -> "Reduce DF by 20% of the caster's AP"
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

        // a modifier that give status 
        // e.g. Wound, Stun, Transformed
        public static IModifier Status(ModifierEnum status)
            => new StatusModifier(status);

        // a shield - absorb damage before HP, gone when duration expired
        //   Shield(500f)                    -> "a 500 shield"
        //   Shield((StatEnum.DF, 300f))     -> "a shield worth 300% of the caster's DF"
        public static IModifier Shield(params StatRatio[] ratios)
            => new StatModifier(ModifierEnum.Shield, ratios);

        // ================================== ActionGroup ==================================
        // a template action
        public static SkillPart Part(TemplateActionRegistrySO registry, ActionSourceEnum source,
                                                   TemplateActionEnum action, AimTargetEnum target,
                                                   params SkillEffect[] effects)
            => Group(registry, source, action, target, conditions: null, tuning: null, effects: effects);

        // the same, with this hero's numbers for the action
        public static SkillPart Part(TemplateActionRegistrySO registry, ActionSourceEnum source,
                                                   TemplateActionEnum action, AimTargetEnum target,
                                                   Tuning tuning,
                                                   params SkillEffect[] effects)
            => Group(registry, source, action, target, conditions: null, tuning: tuning, effects: effects);

        // a template action with condition, and this hero's numbers for it if it needs them.
        // conditin need to be true, in order this template action to play.
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

        // ================================== Flow ==================================
        // one flow = one skill, read SkillFlow.cs
        //   onStart     the parts the skill starts with
        //   onHit       the parts played when `onStart` hits something
        //   onExpired   the parts played when `onStart` expires
        // e.g. Flow(onStart: shot, onHit: blast)
        //      Flow(onStart: Together(charge, hitbox), onExpired: slash)
        public static SkillFlow Flow(PartSet onStart, PartSet onHit = default, PartSet onExpired = default)
            => new SkillFlow(onStart.Parts, onHit.Parts, onExpired.Parts);

        // several parts in one slot of a flow. They start together, and the first one leads:
        // only lead matter for what'll be played afterward. 
        public static PartSet Together(params SkillPart[] parts)
            => new PartSet(parts);

        // repeat the same flow [x] total time.
        // e.g.     Repeat(Flow(onStart: dash, onExpired: stab), times: 3)
        public static SkillFlow Repeat(SkillFlow flow, int times)
            => new SkillFlow(flow.OnStart, flow.OnHit, flow.OnExpired, times);

        // the same to Repeat(), but the number of rounds is asked for every time the flow is played.
        // e.g.     Repeat(Flow(onStart: strike), () => StrikeCount)    -> "as many strikes as StrikeCount is at that moment"
        public static SkillFlow Repeat(SkillFlow flow, System.Func<int> times)
            => new SkillFlow(flow.OnStart, flow.OnHit, flow.OnExpired, times);

        // ================================== Tune ==================================
        // tuning a template action
        public static Tuning Tune(float? castTime = null)
            => new Tuning { CastTime = castTime };

        // length / width are world units - a circle 4.5 across is length: 4.5, width: 4.5
        public static AOETuning TuneAOE(float? castTime = null, float? duration = null,
                                        float? length = null, float? width = null,
                                        bool? sticky = null, AOEOffsetEnum? offset = null, int? range = null)
            => new AOETuning { CastTime = castTime, Duration = duration, Length = length, Width = width,
                               Sticky = sticky, Offset = offset, Range = range };

        public static MoveTuning TuneMove(float? castTime = null, int? range = null,
                                          float? duration = null, float? spread = null, bool? maxRange = null)
            => new MoveTuning { CastTime = castTime, Range = range, Duration = duration, Spread = spread,
                                MaxRange = maxRange };

        public static ProjectileTuning TuneProjectile(float? castTime = null, int? range = null,
                                                      float? spread = null, float? size = null)
            => new ProjectileTuning { CastTime = castTime, Range = range, Spread = spread, Size = size };

        public static FireTimingRunnerTuning TuneFireTimingRunner(int count, FireTimingModeEnum mode,
                                                                   float interval = 0f, Tuning innerTuning = null,
                                                                   float? castTime = null)
            => new FireTimingRunnerTuning { Count = count, Mode = mode, Interval = interval, InnerTuning = innerTuning,
                                            CastTime = castTime };

        public static FireTimingRunnerProjectileTuning TuneFireTimingRunnerProjectile(int count, FireTimingModeEnum mode,
                                                                   float interval = 0f, Tuning innerTuning = null,
                                                                   int? randomPoolRadius = null, float? castTime = null)
            => new FireTimingRunnerProjectileTuning { Count = count, Mode = mode, Interval = interval,
                                                      InnerTuning = innerTuning, RandomPoolRadius = randomPoolRadius,
                                                      CastTime = castTime };
    }
}
