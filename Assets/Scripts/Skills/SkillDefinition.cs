using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using MagicSchool.Contracts;
using UnityEngine;

namespace MagicSchool.Skills
{
    /// <summary>
    /// A whole container for 1 skill.
    /// A skill is a combination of:
    /// 1) the SkillFlow: to create active skill, auto-attck skill, and more if added. (read SKillFlow.cs for more info)
    /// 2) the hook: to create a passive skill, and helper for active skill (read virtual hooks section in here)
    /// </summary>
    // A skill's numbers are Tuned(...) properties (Skill Tuner, See SkillTuning), read once when the skill is built.
    // Descriptions write amounts through AD() / AP() / DF() / MaxHP(), never "{X}% AP", so the inspector shows the
    // number for the hero's stats, or with Alt held the formula: "deals 310 damage" / "deals [200% AP] damage"; with
    // no stats (a preview) the formula is shown.
    // Hooks: OnCast (mana full), OnAutoAttack, OnPickAutoAttackTarget (Goblin Archer's random enemy),
    // AutoAttackDamageMultiplier (before the crit roll; Skeleton Archer), OnCombatStart, OnHeroDied (anyone else's
    // death), OnDeath (the caster's own; Big Slime splits), OnHeroBurned, OnMarkedAllyDealtDamage (wired by
    // AlignBonuses.Refresh, fixed at fight start), OnDamageTaken (after the hit is worked out, so the outcome is
    // final). SpecialMarks names the marks the skill reads; the Align Editor puts each in every pattern.
    // Flows are two deep: onStart, then optional onHit / onExpired, then nothing. A round finishes when onStart
    // expires if nothing follows, when the leading follow-up part expires, or at once if that part could not play;
    // onHit never finishes a round, since a skill can miss. A repeating active keeps Cast going until its rounds are
    // done. A part played outside a flow (from a hook) must be initialized in Init.
    public abstract class SkillDefinition
    {
        private TemplateActionRegistrySO _registry;

        private SkillFlow _active;
        private SkillFlow _onAutoAttack;

        protected ISkillCaster Caster { get; private set; }
        public float CastTime { get; private set; }
        public bool CanWalkWhileCasting { get; private set; }

        public bool IsRepeating { get; private set; }

        public bool IsMovingCaster => Move.IsMoving(Caster);

        protected SkillDefinition(TemplateActionRegistrySO registry)
        {
            _registry = registry;
        }

        protected abstract SkillFlow BuildActiveFlow(TemplateActionRegistrySO registry);
        protected virtual SkillFlow BuildAutoAttackFlow(TemplateActionRegistrySO registry) => null;

        public virtual void Init(ISkillCaster caster)
        {
            Caster = caster;

            if (_registry != null)
            {
                _active = BuildActiveFlow(_registry);
                _onAutoAttack = BuildAutoAttackFlow(_registry);

                _registry = null;
            }

            _active?.Init(caster);
            _onAutoAttack?.Init(caster);
        }

        protected static float Tuned(float value, [CallerMemberName] string name = "", [CallerFilePath] string file = "")
            => SkillTuning.Get(file, name, value, isInt: false);

        protected static int Tuned(int value, [CallerMemberName] string name = "", [CallerFilePath] string file = "")
            => Mathf.RoundToInt(SkillTuning.Get(file, name, value, isInt: true));

        public abstract string SkillName { get; }
        public abstract string Description { get; }
        public virtual string PassiveDescription => "";

        public string DescribeActive(IHeroStats stats, bool formula) => Describing(stats, formula, () => Description);
        public string DescribePassive(IHeroStats stats, bool formula) => Describing(stats, formula, () => PassiveDescription);

        private IHeroStats _textStats;
        private bool _textFormula;

        private string Describing(IHeroStats stats, bool formula, Func<string> text)
        {
            _textStats = stats;
            _textFormula = formula;
            try { return text(); }
            finally { _textStats = null; _textFormula = false; }
        }

        protected string AD(float percent) => Scaled(percent, "AD", stats => stats.AttackDamage);
        protected string AP(float percent) => Scaled(percent, "AP", stats => stats.Magic);
        protected string DF(float percent) => Scaled(percent, "DF", stats => stats.Defence);
        protected string MaxHP(float percent) => Scaled(percent, "max HP", stats => stats.MaxHP);

        protected IHeroStats TextStats => _textStats ?? Caster;
        protected bool TextFormula => _textFormula;

        private string Scaled(float percent, string stat, Func<IHeroStats, int> read)
        {
            IHeroStats stats = _textStats ?? Caster;
            if (_textFormula || stats == null) return $"[{percent:0.##}% {stat}]";

            return Mathf.RoundToInt(read(stats) * percent / 100f).ToString();
        }

        public virtual IReadOnlyList<string> SpecialMarks => NoSpecialMarks;
        private static readonly string[] NoSpecialMarks = new string[0];
        public virtual bool HasActive => _active != null;
        public virtual bool HasAutoAttackPassive => _onAutoAttack != null;

        public virtual bool OnCast() => PlayFlow(_active);

        public virtual bool OnAutoAttack(ICombatant target) => PlayFlow(_onAutoAttack);

        public virtual ICombatant OnPickAutoAttackTarget(ICombatant target) => target;

        public virtual float AutoAttackDamageMultiplier(ICombatant target) => 1f;

        public virtual bool OnCombatStart() => false;

        public virtual void OnHeroDied(ICombatant dead) { }

        public virtual void OnDeath() { }

        public virtual void OnHeroBurned(ICombatant burned) { }

        public virtual void OnMarkedAllyDealtDamage(string mark, ICombatant ally, DamageEvent hit) { }

        public virtual void OnDamageTaken(DamageEvent hit) { }

        protected bool PlayFlow(SkillFlow flow)
        {
            if (flow == null) return false;

            if (flow == _active && flow.Times > 1) IsRepeating = true;

            if (PlayRound(flow)) return true;

            FlowFinished(flow);

            return false;
        }

        protected bool PlayOnePart(SkillPart part, ICombatant assignedTarget = null)
        {
            if (Play(part, new TemplateActionCallbacks { OnSkillHit = part.OnSkillHit }, null, assignedTarget) == null) return false;

            CastTime = CastTimeOf(part);
            CanWalkWhileCasting = WalksWhileCasting(part);
            return true;
        }

        private bool PlayRound(SkillFlow flow, int round = 1)
        {
            Action finished = () => RoundFinished(flow, round);

            return PlayParts(flow.OnStart, Follow(flow.OnHit), FollowLast(flow.OnExpired, finished), previousPosition: null);
        }

        private void RoundFinished(SkillFlow flow, int round)
        {
            if (HasNextRound(flow, round) && PlayRound(flow, round + 1)) return;

            FlowFinished(flow);
        }

        private bool HasNextRound(SkillFlow flow, int round)
        {
            if (!Caster.IsAlive) return false;

            if (round >= flow.Times) return false;

            if (Caster is UnityEngine.Object hero && hero == null) return false;

            return true;
        }

        private void FlowFinished(SkillFlow flow)
        {
            if (flow == _active) IsRepeating = false;
        }

        private bool PlayParts(IReadOnlyList<SkillPart> parts,
        Action<Vector3> onHit,
        Action<Vector3> onExpired,
        Vector3? previousPosition)
        {
            float longestCast = 0f;
            bool canWalk = false;
            TemplateAction leader = null;

            for (int i = 0; i < parts.Count; i++)
            {
                SkillPart part = parts[i];
                bool isLeading = i == 0;

                var callbacks = new TemplateActionCallbacks
                {
                    OnHit = isLeading ? onHit : null,
                    OnExpired = isLeading ? onExpired : null,
                    OnSkillHit = part.OnSkillHit,
                };

                TemplateAction played = null;
                if (SkillCondition.Ask(part.Conditions, Caster) != ConditionResultEnum.ConditionIsNotMet)
                {
                    played = Play(part, callbacks, previousPosition, host: leader);
                }

                if (isLeading && played == null) return false;
                if (isLeading) leader = played;
                if (played != null) longestCast = Mathf.Max(longestCast, CastTimeOf(part));
                if (played != null) canWalk |= WalksWhileCasting(part);
            }

            CastTime = longestCast;
            CanWalkWhileCasting = canWalk;
            return true;
        }

        private Action<Vector3> Follow(IReadOnlyList<SkillPart> parts)
        {
            if (parts.Count == 0) return null;

            return position => PlayParts(parts, onHit: null, onExpired: null, previousPosition: position);
        }

        private Action<Vector3> FollowLast(IReadOnlyList<SkillPart> parts, Action finished)
        {
            if (parts.Count == 0) return _ => finished();

            return position =>
            {
                bool played = PlayParts(parts, onHit: null, onExpired: _ => finished(), previousPosition: position);

                if (!played) finished();
            };
        }

        private TemplateAction Play(SkillPart part, TemplateActionCallbacks callbacks, Vector3? previousPosition,
                                    ICombatant assignedTarget = null, TemplateAction host = null)
        {
            if (Caster is UnityEngine.Object hero && hero == null) return null;

            return TemplateAction.Spawn(part, Caster, callbacks, previousPosition, assignedTarget, host);
        }

        private static float CastTimeOf(SkillPart part)
            => part.Tuning?.CastTime ?? part.TemplateAction.CastTime;

        private static bool WalksWhileCasting(SkillPart part) => part.Tuning?.WalkWhileCasting ?? false;
    }
}
