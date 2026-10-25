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
    public abstract class SkillDefinition
    {
        private TemplateActionRegistrySO _registry;

        private SkillFlow _active;          // played when mana is full
        private SkillFlow _onAutoAttack;        // played on every auto attack. (most skills have none)

        protected ICombatant Caster { get; private set; }   // owner of this skill
        public float CastTime { get; private set; } // the animation's duration to play this specific skill
        public bool CanWalkWhileCasting { get; private set; }   // a part just played lets the caster walk during CastTime

        public bool IsRepeating { get; private set; }

        protected SkillDefinition(TemplateActionRegistrySO registry)
        {
            _registry = registry;
        }

        // ============================================== init ==============================================
        /// The flows of the hero's skill. Built once from Init.
        /// BuildActiveFlow    the flow played when mana is full
        /// BuildAutoAttackFlow    the flow played on every auto attack, if the skill has one
        protected abstract SkillFlow BuildActiveFlow(TemplateActionRegistrySO registry);
        protected virtual SkillFlow BuildAutoAttackFlow(TemplateActionRegistrySO registry) => null;

        public virtual void Init(ICombatant caster)
        {
            Caster = caster;

            if (_registry != null)
            {
                _active = BuildActiveFlow(_registry);
                _onAutoAttack = BuildAutoAttackFlow(_registry);

                // prevent second init from rebuilt the skill
                _registry = null;
            }

            _active?.Init(caster);
            _onAutoAttack?.Init(caster);
        }

        // ============================================== tuning ==============================================
        /// A skill number that Magic School 4 > Skill Tuner can change while the game runs (read SkillTuning.cs).
        /// e.g.     private static float DamageRatio => Tuned(200f);
        protected static float Tuned(float value, [CallerMemberName] string name = "", [CallerFilePath] string file = "")
            => SkillTuning.Get(file, name, value, isInt: false);

        protected static int Tuned(int value, [CallerMemberName] string name = "", [CallerFilePath] string file = "")
            => Mathf.RoundToInt(SkillTuning.Get(file, name, value, isInt: true));

        // ============================================== virtual ==============================================
        public abstract string SkillName { get; }
        public abstract string Description { get; }
        public virtual string PassiveDescription => "";

        // ============================================== description numbers ==============================================
        // A description writes an amount through AD() / AP() / DF() / MaxHP() rather than "{X}% AP", so it reads as the number
        // it comes to for whoever has the skill - or, with formula on (the inspector holds Alt), as the formula.
        // e.g.     $"deals {AP(DamageRatio)} damage"   =>  "deals 310 damage"   or   "deals [200% AP] damage"
        // stats: whose numbers to use - a hero passes itself, a shop preview its 1-star stats. Without any, the formula is shown.
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

        // whose numbers the description is written for, and whether as the formula - for a number AD() / AP() cannot write
        // e.g.     Skeleton Archer's long shot bonus, which grows with his star
        protected IHeroStats TextStats => _textStats ?? Caster as IHeroStats;
        protected bool TextFormula => _textFormula;

        private string Scaled(float percent, string stat, Func<IHeroStats, int> read)
        {
            IHeroStats stats = _textStats ?? Caster as IHeroStats;
            if (_textFormula || stats == null) return $"[{percent:0.##}% {stat}]";

            return Mathf.RoundToInt(read(stats) * percent / 100f).ToString();
        }

        // The special marks this skill reads, by name (See AlignMark). The Align Editor puts each one in every pattern of the hero.
        // e.g.     Rock Golem => { "Ward" }
        public virtual IReadOnlyList<string> SpecialMarks => NoSpecialMarks;
        private static readonly string[] NoSpecialMarks = new string[0];
        public virtual bool HasActive => _active != null;
        public virtual bool HasAutoAttackPassive => _onAutoAttack != null;

        // ============================================== virtual hooks ==============================================
        // If mana is full, subclass do [x], and play the active flow.
        public virtual bool OnCast() => PlayFlow(_active);

        // If auto attack, subclass do [x], and play the onAutoAttack flow.
        public virtual bool OnAutoAttack(ICombatant target) => PlayFlow(_onAutoAttack);

        // Normally a auto attack always default to current target.
        // But some skill may pick different target through this method. 
        // e.g. Goblin Archer auto attack a random enemy
        public virtual ICombatant OnPickAutoAttackTarget(ICombatant target) => target;

        // At combat start, play OnCombatStart.
        public virtual bool OnCombatStart() => false;

        // If any hero dies, play OnHeroDied.
        // e.g. Troll gains health whenever an enemy dies
        public virtual void OnHeroDied(ICombatant dead) { }

        // If the caster himself dies, play OnDeath.
        // e.g. Big Slime splits into Small Slimes
        public virtual void OnDeath() { }

        // If any hero takes a burn tick, play OnHeroBurned.
        // e.g. Imp gains attack speed whenever an enemy burns
        public virtual void OnHeroBurned(ICombatant burned) { }

        // If an ally standing on one of this skill's special marks deals damage, play OnMarkedAllyDealtDamage.
        // Wired by AlignBonuses.Refresh, so like every aligning bonus it follows where everyone stood when the fight started.
        // e.g. Shield Knight heals off the damage the allies on his Guard mark deal
        public virtual void OnMarkedAllyDealtDamage(string mark, ICombatant ally, DamageEvent hit) { }

        // If the caster itself takes a hit, play OnDamageTaken. Raised after the hit is worked out, so hit.Outcome is final.
        // e.g. Shield Knight counts what he takes while bracing, and slams it back
        public virtual void OnDamageTaken(DamageEvent hit) { }

        // add other hook
        // ...

        // ============================================== play ==============================================
        /// Play a specify flow.
        /// The flow will play each skill part which consist of OnStart, OnHit, and OnExpired in order. 
        /// OnStart, then follow by optional OnHit, and optional OnExpired.
        /// Vocab:
        ///     onStart     the parts that skill starts with.
        ///     onHit       the parts to play when `onStart` hits something. Optional.
        ///     onExpired   the parts to play when `onStart` expires. Optional.
        protected bool PlayFlow(SkillFlow flow)
        {
            if (flow == null) return false;

            // if the flow played is active and repeat, set the FLAG.
            if (flow == _active && flow.Times > 1) IsRepeating = true;

            if (PlayRound(flow)) return true;

            FlowFinished(flow);

            return false;
        }

        // Play a single part without depending on a flow.
        // no flow played after this.
        protected bool PlayOnePart(SkillPart part, ICombatant assignedTarget = null)
        {
            if (Play(part, new TemplateActionCallbacks { OnSkillHit = part.OnSkillHit }, null, assignedTarget) == null) return false;

            CastTime = CastTimeOf(part);
            CanWalkWhileCasting = WalksWhileCasting(part);
            return true;
        }

        // ============================================== private ==============================================
        // Play the flow once, start to finish. That is one round.
        // If 'round' is more than 1, repeat the flow for the 'round'.
        private bool PlayRound(SkillFlow flow, int round = 1)
        {
            // action to repeat the round
            Action finished = () => RoundFinished(flow, round);

            // play the flow, hand 'finished' to it too 
            return PlayParts(flow.OnStart, Follow(flow.OnHit), FollowLast(flow.OnExpired, finished), previousPosition: null);
        }

        // A round is finished, if the flow request repeat, play the next round. (read Repeat() in SkillFactory.cs)
        // if no repeat request, the flow'll end here.
        private void RoundFinished(SkillFlow flow, int round)
        {
            if (HasNextRound(flow, round) && PlayRound(flow, round + 1)) return;

            FlowFinished(flow);
        }

        private bool HasNextRound(SkillFlow flow, int round)
        {
            if (!Caster.IsAlive) return false;

            // the repeat count is all gone
            if (round >= flow.Times) return false;

            // guard
            if (Caster is UnityEngine.Object hero && hero == null) return false;

            return true;
        }

        // The flow has nothing left to play
        // e.g.     out of rounds to play, the caster died.
        private void FlowFinished(SkillFlow flow)
        {
            if (flow == _active) IsRepeating = false;
        }

        // read PlayFlow()
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

                // each skill part can be played by using this callback
                var callbacks = new TemplateActionCallbacks
                {
                    OnHit = isLeading ? onHit : null,
                    OnExpired = isLeading ? onExpired : null,
                    OnSkillHit = part.OnSkillHit,
                };

                TemplateAction played = null;
                if (SkillCondition.Ask(part.Conditions, Caster) != ConditionResultEnum.ConditionIsNotMet)
                {
                    // give the callback to template action
                    // let the template action decide when to use the callback to play each skill part
                    // e.g.     projectile (a template action) hit a hero, so it call OnHit.
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

        // Consume only the optional part of the SkillPart.cs (e.g. OnHit, OnExpired)
        // return the callback that can be used to play the specify SkillPart.
        // Currently, this project force the skill to only have 2 flow depth. 
        // So the Follow() won't play anything after itself.
        // e.g.     OnStart => OnHit        => None
        //                  => OnExpired    => None
        private Action<Vector3> Follow(IReadOnlyList<SkillPart> parts)
        {
            if (parts.Count == 0) return null;

            return position => PlayParts(parts, onHit: null, onExpired: null, previousPosition: position);
        }

        // Same to Follow(), but for the parts a round ends with. It is to tell when this flow end:
        // Example, 
        //      there are no parts to play            => finished when `onStart` expired
        //      there are parts to play               => finished when their leading part expired
        //      the leading part could not be played  => finished right away, nothing is left to wait for
        // NOTE,
        //      onHit never finishes a round: a skill can miss, and then it would never finish.
        private Action<Vector3> FollowLast(IReadOnlyList<SkillPart> parts, Action finished)
        {
            if (parts.Count == 0) return _ => finished();

            return position =>
            {
                bool played = PlayParts(parts, onHit: null, onExpired: _ => finished(), previousPosition: position);

                if (!played) finished();
            };
        }

        // spawn the part's template action (read TemplateAction.cs for more info)
        private TemplateAction Play(SkillPart part, TemplateActionCallbacks callbacks, Vector3? previousPosition,
                                    ICombatant assignedTarget = null, TemplateAction host = null)
        {
            // guard
            if (Caster is UnityEngine.Object hero && hero == null) return null;

            return TemplateAction.Spawn(part, Caster, callbacks, previousPosition, assignedTarget, host);
        }

        // get the cast time of the specify skill part
        // cast time = duration before the skill part is played
        // e.g.     0.5 sec before shooting a projectile
        private static float CastTimeOf(SkillPart part)
            => part.Tuning?.CastTime ?? part.TemplateAction.CastTime;

        // does the skill part let the caster walk during its cast time?
        private static bool WalksWhileCasting(SkillPart part) => part.Tuning?.WalkWhileCasting ?? false;
    }
}
