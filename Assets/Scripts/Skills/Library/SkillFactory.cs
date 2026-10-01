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

        // the same, re-applied on a timer - Orc Blademaster's spin, Pip's patch
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

        // the group of modifiers - everything in it shares one duration. 
        // -1f is permanent.
        public static ICustomModifier Bundle(float duration, params IModifier[] modifiers)
            => new CustomModifier(duration, modifiers);

        // the same to Bundle() above, but the duration is asked for every time the bundle was used - this make the duration can be adjust during combat after init once.
        //   Bundle(() => CurrentStun, Status(Stun))    -> "a stun as long as CurrentStun is at that moment"
        // e.g. DireWolf's stun is increased during the combat.
        public static ICustomModifier Bundle(System.Func<float> duration, params IModifier[] modifiers)
            => new CustomModifier(duration, modifiers);

        // a modifier that gives a stat bonus:
        //   Buff(DamageReduction, 20f)                     -> "+20% DR"
        //   Buff(ATK, (StatEnum.AP, 50f))                  -> "Buff ATK = 50% of the caster's AP"
        //   Buff(DefendShred, 20f, (StatEnum.AP, 20f))     -> "Reduce DF = 20 flat, plus 20% AP on top"
        public static IModifier Buff(ModifierEnum modifier, params StatRatio[] ratios)
            => new StatModifier(modifier, ratios);

        // same to Buff() above, but: 
        // 1) buff will be given to other hero. 
        // 2) buff will derived from the "source" parameter (normally it would derived from the caster).
        // e.g. Dryad's skill buff ally base on their attack speed by +25% => This mean the skill is derived from ally, not the caster itself.
        // another pattern, you should know:
        // e.g. Dryad's skill (alternative) buff ally base on Dryad's AP by +50%AP => This mean the skill is derive from the caster
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
        public static SkillActionGroup ActionGroup(TemplateActionRegistrySO registry, ActionSourceEnum source,
                                                   TemplateActionEnum action, AimTargetEnum target,
                                                   params SkillEffect[] effects)
            => Group(registry, source, action, target, conditions: null, tuning: null, effects: effects);

        // the same, with this hero's numbers for the action
        public static SkillActionGroup ActionGroup(TemplateActionRegistrySO registry, ActionSourceEnum source,
                                                   TemplateActionEnum action, AimTargetEnum target,
                                                   Tuning tuning,
                                                   params SkillEffect[] effects)
            => Group(registry, source, action, target, conditions: null, tuning: tuning, effects: effects);

        // a template action with condition, and this hero's numbers for it if it needs them.
        // conditin need to be true, in order this template action to play.
        public static SkillActionGroup ActionGroupWhen(TemplateActionRegistrySO registry, ActionSourceEnum source,
                                                       TemplateActionEnum action, AimTargetEnum target,
                                                       List<SkillCondition> conditions, Tuning tuning = null,
                                                       params SkillEffect[] effects)
            => Group(registry, source, action, target, conditions: conditions, tuning: tuning, effects: effects);

        private static SkillActionGroup Group(TemplateActionRegistrySO registry, ActionSourceEnum source,
                                              TemplateActionEnum action, AimTargetEnum target,
                                              List<SkillCondition> conditions, Tuning tuning,
                                              SkillEffect[] effects)
            => new SkillActionGroup(source, registry.Get(action), target,
                                    conditions: conditions, effects: new List<SkillEffect>(effects), tuning: tuning);

        // a step inside template action
        // step are working in order, each step will be played if it was trigger.
        public static SkillStep Step(TriggerEnum trigger, params SkillActionGroup[] groups)
            => new SkillStep(trigger, new List<SkillActionGroup>(groups));
        
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
                                          float? duration = null, float? spread = null)
            => new MoveTuning { CastTime = castTime, Range = range, Duration = duration, Spread = spread };

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
