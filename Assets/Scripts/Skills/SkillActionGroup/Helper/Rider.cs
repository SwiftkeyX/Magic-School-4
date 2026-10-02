using UnityEngine;

namespace MagicSchool.Skills
{
    /// <summary>
    /// Rider = A template action that rides on another template action.
    /// Rider tell which template action is the host, which is the rider.
    /// The host is the leading part out of the 2 that were played in: Together(host, rider)
    /// 
    /// Functionality:
    /// 1) Once the host is dead, make rider dead too.
    /// 2) The rider faces the way its host does.
    /// </summary>
    internal class Rider
    {
        private readonly TemplateAction _me;
        private readonly TemplateAction _host;

        // a factory for Rider.
        // no host means this template action is the lead one, or was played alone.
        internal static Rider RideOn(TemplateAction me, TemplateAction host)
        {
            if (me == null || host == null) return null;

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
        private void HostIsDead(Vector3 position)
        {
            _host.OnExpired -= HostIsDead;

            _me.EndNow();
        }
    }
}
