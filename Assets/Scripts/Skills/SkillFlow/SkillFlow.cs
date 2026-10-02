using System.Collections.Generic;
using MagicSchool.Contracts;

namespace MagicSchool.Skills
{

    /// <summary>
    /// skill is the combination of SkillPart.cs. A flow is the order played between those SkillPart.
    /// Exmaple,
    ///     Flow(onStart: shot, onHit: blast)                          Dwarf: shoot projectile. when it hit, create blast
    ///     Flow(onStart: frenzy, onExpired: howl)                     Dire Wolf: buff itself, and when the buff expired, do howl
    ///     Flow(onStart: Together(charge, hitbox))                    Centaur: charge forward, with a hitbox riding on itsef
    /// 
    /// Vocab:
    ///     onStart     the parts that skill starts with.
    ///     onHit       the parts to play when `onStart` hits something. Optional.
    ///     onExpired   the parts to play when `onStart` expires. Optional.
    ///     round       the flow played once start to finish, that is 1 round. 
    ///                 A flow default to plays only one round unless it is repeated.
    ///                 Repeat(Flow(onStart: dash, onExpired: stab), times: 3)   -> dash, stab, for three times 
    ///
    /// A part (SkillPart) is one piece of a skill:
    /// e.g. a shot, a blast, a dash, a buff
    /// </summary>
    public class SkillFlow
    {
        public IReadOnlyList<SkillPart> OnStart { get; }
        public IReadOnlyList<SkillPart> OnHit { get; }
        public IReadOnlyList<SkillPart> OnExpired { get; }
        public int Times { get; }       // how many rounds this flow plays

        public SkillFlow(IReadOnlyList<SkillPart> onStart, IReadOnlyList<SkillPart> onHit,
                         IReadOnlyList<SkillPart> onExpired, int times = 1)
        {
            OnStart = onStart;
            OnHit = onHit;
            OnExpired = onExpired;
            Times = times;
        }

        // ================================== init ==================================
        public void Init(IEffectable caster)
        {
            foreach (SkillPart part in OnStart) part?.Init(caster);
            foreach (SkillPart part in OnHit) part?.Init(caster);
            foreach (SkillPart part in OnExpired) part?.Init(caster);
        }
    }
}
