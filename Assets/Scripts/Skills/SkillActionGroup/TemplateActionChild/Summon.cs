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

        private SkillPart _act;
        private float _duration = 5f;
        private float _interval = 1f;

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

            DestroyMe();
        }

        protected override bool ResolveSource(ActionSourceEnum source)
        {
            _source = _me.transform.position;

            return true;
        }

        protected override bool ResolveAimTarget(AimTargetEnum aimTarget)
        {
            if (_act == null)
            {
                Debug.LogWarning("[Summon] has no act for its companion. Give it one with TuneSummon(act: ...)", this);
                return false;
            }

            if (aimTarget == AimTargetEnum.Self)
            {
                _owner = _me;
            }

            else if (aimTarget == AimTargetEnum.NearestAlly)
            {
                _owner = _me.FindNearestAlly() ?? _me;
            }

            else if (aimTarget == AimTargetEnum.LowestHealthAlly)
            {
                _owner = _me.FindLowestHealthAlly() ?? _me;
            }

            else if (aimTarget == AimTargetEnum.Assigned)
            {
                _owner = _assignedTarget;
                if (_owner == null) return false;
            }

            else _owner = _me;

            _aimTarget = _owner.transform.position;

            return true;
        }

        protected override Vector3 GetSpawnPosition() => _owner.transform.position;
    }
}
