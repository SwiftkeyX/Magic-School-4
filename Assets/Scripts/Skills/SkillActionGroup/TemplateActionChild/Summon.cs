using UnityEngine;
using MagicSchool.Contracts;

namespace MagicSchool.Skills
{
    /// <summary>
    /// Summon is the template action that spawn Companion.cs and gives it to a unit, the owner.
    /// What the companion does afterward is the companion's business (read Companion.cs)
    /// e.g.    Priest summons a fairy and gives it to an ally.
    /// </summary>
    internal class Summon : TemplateAction<SummonTuning> 
    {
        [SerializeField] private Companion _companionPrefab;

        private ICombatant _owner;

        // ======================================= tune able =======================================
        private SkillPart _act;             // what the companion plays on every interval
        private float _duration = 5f;       // how long the companion stays
        private float _interval = 1f;       // act cooldown

        // ======================================= override =======================================
        protected override void ApplyTypedTuning(SummonTuning summonTuning)
        {
            if (summonTuning.Act != null) _act = summonTuning.Act;
            if (summonTuning.Duration.HasValue) _duration = summonTuning.Duration.Value;
            if (summonTuning.Interval.HasValue) _interval = summonTuning.Interval.Value;
        }

        protected override void Play()
        {
            Companion companion = Instantiate(_companionPrefab);
            companion.Begin(_owner, _act, _duration, _interval);

            // after spawn companion, destroy itself
            DestroyMe();
        }

        // source mean nothing to Summon
        protected override bool ResolveSource(ActionSourceEnum source)
        {
            _source = _me.transform.position;

            return true;
        }

        // aim = the owner, the unit the companion is given to
        protected override bool ResolveAimTarget(AimTargetEnum aimTarget)
        {
            // guard
            if (_act == null)
            {
                Debug.LogWarning("[Summon] has no act for its companion. Give it one with TuneSummon(act: ...)", this);
                return false;
            }

            // give to me
            if (aimTarget == AimTargetEnum.Self)
            {
                _owner = _me;
            }

            // give to the closest ally.
            else if (aimTarget == AimTargetEnum.NearestAlly)
            {
                _owner = _me.FindNearestAlly() ?? _me;
            }

            // the owner was already chosen for it
            else if (aimTarget == AimTargetEnum.Assigned)
            {
                _owner = _assignedTarget;
                if (_owner == null) return false;
            }

            // else if () ...

            // fallback
            else _owner = _me;

            _aimTarget = _owner.transform.position;

            return true;
        }

        protected override Vector3 GetSpawnPosition() => _owner.transform.position;
    }
}
