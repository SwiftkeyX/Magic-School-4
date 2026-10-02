using System;
using System.Collections.Generic;
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
        private SkillFlow _onAttack;        // played on every auto attack. (most skills have none)

        protected ICombatant Caster { get; private set; }   // owner of this skill
        public float CastTime { get; private set; } // the animation's duration to play this specific skill

        protected SkillDefinition(TemplateActionRegistrySO registry)
        {
            _registry = registry;
        }

        // ============================================== init ==============================================
        /// The flows of the hero's skill. Built once from Init.
        /// BuildActiveFlow    the flow played when mana is full
        /// BuildAttackFlow    the flow played on every auto attack, if the skill has one
        protected abstract SkillFlow BuildActiveFlow(TemplateActionRegistrySO registry);
        protected virtual SkillFlow BuildAttackFlow(TemplateActionRegistrySO registry) => null;

        public virtual void Init(ICombatant caster)
        {
            Caster = caster;

            if (_registry != null)
            {
                _active = BuildActiveFlow(_registry);
                _onAttack = BuildAttackFlow(_registry);

                // prevent second init from rebuilt the skill
                _registry = null;
            }

            _active?.Init(caster);
            _onAttack?.Init(caster);
        }

        // ============================================== virtual ==============================================
        public abstract string SkillName { get; }
        public abstract string Description { get; }
        public virtual string PassiveDescription => "";
        public virtual bool HasActive => _active != null;
        public virtual bool HasAttackPassive => _onAttack != null;

        // ============================================== virtual hooks ==============================================
        // If mana is full, subclass do [x], and play the active flow.
        public virtual bool OnCast() => PlayFlow(_active);

        // If auto-attack, subclass do [x], and play the onAttack flow.
        public virtual bool OnAttack(ICombatant target) => PlayFlow(_onAttack);

        // At combat start, play OnCombatStart.
        public virtual bool OnCombatStart() => false;

        // If any hero dies, play OnHeroDied.
        // e.g. Troll gains health whenever an enemy dies
        public virtual void OnHeroDied(ICombatant dead) { }

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

            return PlayRound(flow);
        }

        // Play a single part without depending on a flow.
        // no flow played after this.
        protected bool PlayOnePart(SkillPart part, ICombatant assignedTarget = null)
        {
            if (!Play(part, new TemplateActionCallbacks { OnSkillHit = part.OnSkillHit }, null, assignedTarget)) return false;

            CastTime = CastTimeOf(part);
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
            if (round >= flow.Times) return;

            // guard
            if (Caster is UnityEngine.Object hero && hero == null) return;
            if (!Caster.IsAlive) return;

            PlayRound(flow, round + 1);
        }

        // read PlayFlow()
        private bool PlayParts(IReadOnlyList<SkillPart> parts,
        Action<Vector3> onHit,
        Action<Vector3> onExpired,
        Vector3? previousPosition)
        {
            float longestCast = 0f;

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

                bool played = false;
                if (SkillCondition.Ask(part.Conditions, Caster) != ConditionResultEnum.ConditionIsNotMet)
                {
                    // give the callback to template action
                    // let the template action decide when to use the callback to play each skill part
                    // e.g.     projectile (a template action) hit a hero, so it call OnHit.
                    played = Play(part, callbacks, previousPosition);
                }

                if (isLeading && !played) return false;
                if (played) longestCast = Mathf.Max(longestCast, CastTimeOf(part));
            }

            CastTime = longestCast;
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
        private bool Play(SkillPart part, TemplateActionCallbacks callbacks, Vector3? previousPosition, ICombatant assignedTarget = null)
        {
            // guard
            if (Caster is UnityEngine.Object hero && hero == null) return false;

            return TemplateAction.TryPlay(part, Caster, callbacks, previousPosition, assignedTarget);
        }

        // get the cast time of the specify skill part
        // cast time = duration before the skill part is played
        // e.g.     0.5 sec before shooting a projectile
        private static float CastTimeOf(SkillPart part)
            => part.Tuning?.CastTime ?? part.TemplateAction.CastTime;
    }
}
