using System;
using UnityEngine;
using MagicSchool.Contracts;
using MagicSchool.Combat.Placements;
using MagicSchool.Skills;

namespace MagicSchool.Combat.Heroes.States
{
    // share transition condition for statemachine
    // CanWalk waits instead of stepping when an enemy is about to walk next to me, or when my target is stepping onto
    // a hex inside my range: a unit mid-step still counts as on the hex it left, so a ranged hero finishing its step a
    // frame early saw it out of range and walked on while it visibly stood in range (Skeleton Archer and a Minotaur
    // stepping toward each other). Idle checks every frame, so waiting attacks the moment the target lands. It heads
    // for where the target is arriving (See EnemyScan.StepsToReach).
    // When the only step I can take leads away from the target, a shorter way is blocked, usually by an ally: wait up
    // to one step's time in case it steps aside, but only when that ally is not attacking or casting (then it is not
    // moving soon) - otherwise take the longer way.
    internal class Transition
    {
        private readonly Hero _me;

        public Transition(Hero me, MovementConfig movement)
        {
            _me = me;
            _movement = movement;

            _isHexBlocked = hex => _me.IsHexReservedByOther(hex);
        }

        public bool CanAttack(ICombatant nearestEnemy)
        {
            return nearestEnemy != null && nearestEnemy.IsAlive && _me.CurrentHex.IsWithinRange(nearestEnemy.CurrentHex(), _me.Range);
        }

        private readonly MovementConfig _movement;
        private readonly Func<Hex, bool> _isHexBlocked;
        private float _holdSince = -1f;

        public bool CanWalk(ICombatant nearestEnemy)
        {
            if (IsEnemyArrivingNextToMe()) return false;

            if (IsTargetArrivingInRange(nearestEnemy)) return false;

            Hex targetHex = HexPathfinder.FindValidHexToTarget(_me.CurrentHex, nearestEnemy.ArrivingHex(), _isHexBlocked, _me.PathLean);
            if (targetHex == null) return false;

            if (ShouldWaitForBlocker(nearestEnemy, targetHex)) return false;

            targetHex.OnUnitReserved(_me);
            _holdSince = -1f;
            return true;
        }

        private bool IsEnemyArrivingNextToMe()
        {
            foreach (var neighbor in _me.CurrentHex.GetNeighbors())
            {
                ICombatant reserver = _me.WhoReservedThisHex(neighbor);
                if (reserver != null && reserver.Team != _me.Team) return true;
            }

            return false;
        }

        private bool IsTargetArrivingInRange(ICombatant target)
        {
            Hex arriving = target?.ArrivingHex();
            if (arriving == null || arriving == target.CurrentHex()) return false;

            return _me.CurrentHex.IsWithinRange(arriving, _me.Range);
        }

        private bool ShouldWaitForBlocker(ICombatant nearestEnemy, Hex targetHex)
        {
            Vector3 enemyAt = nearestEnemy.ArrivingHex().transform.position;
            float distFromMeToEnemy = Vector3.Distance(_me.CurrentHex.transform.position, enemyAt);
            float distFromTargetHexToEnemy = Vector3.Distance(targetHex.transform.position, enemyAt);
            bool nextHexMakeMeFurtherFromEnemy = distFromTargetHexToEnemy >= distFromMeToEnemy;

            if (!nextHexMakeMeFurtherFromEnemy || !WorthWaitingForBlocker(distFromMeToEnemy, nearestEnemy))
            {
                _holdSince = -1f;
                return false;
            }

            if (_holdSince < 0f) _holdSince = Time.time;
            return Time.time - _holdSince < 1f / _movement.MoveSpeed;
        }

        private bool WorthWaitingForBlocker(float distFromMeToEnemy, ICombatant nearestEnemy)
        {
            foreach (var neighbor in _me.CurrentHex.GetNeighbors())
            {
                float neighborDist = Vector3.Distance(neighbor.transform.position, nearestEnemy.ArrivingHex().transform.position);
                if (neighborDist >= distFromMeToEnemy) continue;

                ICombatant occupant = _me.WhoReservedThisHex(neighbor);
                if (occupant == null || occupant == _me as ICombatant) continue;

                bool isCommitted = occupant.StateType == HeroStateEnum.Attack || occupant.StateType == HeroStateEnum.Cast;
                if (!isCommitted) return true;
            }

            return false;
        }

    }
}
