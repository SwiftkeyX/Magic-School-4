using UnityEngine;
using MagicSchool.Contracts;
using MagicSchool.StatScaling;

namespace MagicSchool.Combat.Heroes.States
{
    // When my target died this frame (often to the attack just made), pick the next one before deciding, or I walk
    // toward the corpse while another enemy stands in range. The skill may pick who an attack lands on (Goblin Archer)
    // and scale its damage before the crit roll (Skeleton Archer's Long Shot). Leaving Attack mid-dash snaps the hero
    // back to its hex centre.
    internal class HeroAttack : HeroState
    {
        public override HeroStateEnum StateType => HeroStateEnum.Attack;
        private const float DashDuration = 0.2f;
        private const float DashDistance = 0.5f;

        private ICombatant _currentTarget;

        private Vector3 _dashStart;
        private Vector3 _dashPeak;
        private float _dashElapsed = -1f;

        private readonly MovementConfig _movement;

        public HeroAttack(Hero hero, MovementConfig movement, Transition transition) : base(hero, transition)
        {
            _movement = movement;
        }

        public override void OnEnter()
        {
            _dashElapsed = -1f;
        }

        public override void OnExit()
        {
            _me.transform.position = _me.CurrentHex.transform.position;
        }

        public override void OnUpdate()
        {
            _currentTarget = _me.FindCurrentTarget();

            if (_me.StateType != StateType) return;

            if (_currentTarget == null) { CheckSwitchState(); return; }

            if (_me.IsAttackReady)
            {
                ICombatant attackTarget = _me.PickAutoAttackTarget(_currentTarget);

                _me.TriggerOnAutoAttack(attackTarget);

                PerformAutoAttack(attackTarget);
            }

            UpdateAttackAnimation();

            CheckSwitchState();
        }

        protected override void CheckSwitchState()
        {
            if (_currentTarget != null && !_currentTarget.IsAlive) _currentTarget = _me.FindCurrentTarget();

            if (_currentTarget == null) { _me.ChangeState(HeroStateEnum.Idle); return; }

            if (_transition.CanAttack(_currentTarget))
            {
                return;
            }

            if (_transition.CanWalk(_currentTarget))
            {
                _me.ChangeState(HeroStateEnum.Walk);
                return;
            }

            else
            {
                _me.ChangeState(HeroStateEnum.Idle);
                return;
            }
        }

        private void PerformAutoAttack(ICombatant target)
        {
            if (!_me.HasStatus(ModifierEnum.AutoAttackWasReplaced))
            {
                float damage = Crit.Roll(_me.AttackDamage * _me.AutoAttackDamageMultiplier(target), _me, out _);

                int landed = target.TakeDamage(
                    damage: Mathf.RoundToInt(damage),
                    source: _me,
                    kind: DamageKindEnum.AutoAttack
                );

                Omnivamp.Apply(_me, landed);

                AttackAnimation(target);
            }

            _me.SpendAttack();

            if (!_me.HasStatus(ModifierEnum.ManaBlocked)) _me.GainMana(Mathf.RoundToInt(_me.GetStat(StatEnum.ManaPerAttack)));
        }

        private void AttackAnimation(ICombatant target)
        {
            _dashStart = _me.transform.position;
            Vector3 toEnemy = target.transform.position - _dashStart;
            Vector3 direction = toEnemy.sqrMagnitude > 0f ? toEnemy.normalized : Vector3.zero;
            _dashPeak = _dashStart + direction * DashDistance;
            _dashElapsed = 0f;
        }

        private void UpdateAttackAnimation()
        {
            if (_dashElapsed < 0f) return;

            _dashElapsed += Time.deltaTime;
            float t = Mathf.Clamp01(_dashElapsed / DashDuration);
            _me.transform.position = Vector3.Lerp(_dashStart, _dashPeak, _movement.AttackCurve.Evaluate(t));

            if (t >= 1f) _dashElapsed = -1f;
        }
    }
}
