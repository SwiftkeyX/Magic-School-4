using UnityEngine;
using MagicSchool.Contracts;

namespace MagicSchool.Combat.Heroes.States
{
    internal class HeroAttack : HeroState
    {
        public override HeroStateEnum StateType => HeroStateEnum.Attack;
        private const int ManaPerAttack = 10;
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
            // Snap back in case we leave Attack mid-dash, so the hero doesn't get left off-hex-center.
            _me.transform.position = _me.CurrentHex.transform.position;
        }

        public override void OnUpdate()
        {
            // get current target. if current target is not available, return new target
            _currentTarget = _me.FindCurrentTarget();

            // CheckSwitchState may switch the state. Then, attack state shouldn't continue.
            if (_me.StateType != StateType) return;

            // guard - nothing left to hit
            if (_currentTarget == null) { CheckSwitchState(); return; }

            // attack again if aa is reset. The timer is always runs in the background
            if (_me.IsAttackReady)
            {
                // fire OnAttack event
                _me.TriggerPassiveSkill(TriggerEnum.OnAttack);

                // this hero auto attack
                PerformAutoAttack();
            }

            // update attack animation
            UpdateAttackAnimation();

            CheckSwitchState();
        }

        protected override void CheckSwitchState()
        {
            // guard
            if (_currentTarget == null) { _me.ChangeState(HeroStateEnum.Idle); return; }

            // resume attack
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

        private void PerformAutoAttack()
        {
            // other action may replace normal auto attack e.g. Vharn
            if (!_me.HasStatus(ModifierEnum.AutoAttackWasReplaced))
            {
                // FLAGGING: attack animation got skip by skill which is not intended
                // apply damage to target
                _currentTarget.TakeDamage(
                    damage: _me.AttackDamage,
                    source: _me,
                    kind: DamageKindEnum.AutoAttack
                );

                // attack animation: dash toward the enemy, then back to where we started
                AttackAnimation();
            }

            // aa is now on cooldown
            _me.SpendAttack();

            // after attack gain mana
            if (!_me.HasStatus(ModifierEnum.ManaBlocked)) _me.GainMana(ManaPerAttack);
        }

        private void AttackAnimation()
        {
            // attack animation: dash toward the enemy, then back to where we started
            _dashStart = _me.transform.position;
            Vector3 toEnemy = _currentTarget.transform.position - _dashStart;
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
