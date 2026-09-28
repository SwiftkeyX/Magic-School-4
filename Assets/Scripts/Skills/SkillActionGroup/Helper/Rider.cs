using System.Linq;
using UnityEngine;
using MagicSchool.Contracts;

namespace MagicSchool.Skills
{
    /// <summary>
    /// Rider = A template action that rides on another template action.
    /// 
    /// Rider tell which template action is the host, which is the rider.
    /// It's only use now:
    /// 1) Once the host is dead, rider also dead.
    /// </summary>
    internal class Rider
    {
        private readonly TemplateAction _me;
        private readonly TemplateAction _host;

        internal static Rider FindHostFor(TemplateAction me, ICombatant caster)
        {
            if (me == null || caster == null) return null;

            // FLAGGING: It's only use by Grimm now. So it only find 1 template action, "Move"
            // But it could use other template action as a host too. Let's see what'll happen.
            
            // Find all "Move" 
            Move[] findPossibleHost = Object.FindObjectsByType<Move>();

            // Find Host by checking that, this template action have the same caster.
            Move host = findPossibleHost.FirstOrDefault(templateAction => ReferenceEquals(templateAction.Caster, caster));

            if (host == null) return null;

            return new Rider(me, host);
        }

        internal Vector3 HostFacing => _host == null ? Vector3.zero : _host.Facing;

        private Rider(TemplateAction me, TemplateAction host)
        {
            _me = me;
            _host = host;

            _host.OnExpired += HostIsDead;
        }

        // Host is dead, now I dies too.
        private void HostIsDead(SkillStepContext context)
        {
            _host.OnExpired -= HostIsDead;

            _me.EndNow();
        }
    }
}
