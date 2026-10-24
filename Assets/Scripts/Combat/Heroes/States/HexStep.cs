using UnityEngine;
using MagicSchool.Contracts;
using MagicSchool.Engine;
using MagicSchool.Combat.Placements;

namespace MagicSchool.Combat.Heroes.States
{
    // One step from the hex a hero stands on to a neighbour it has already reserved, one frame at a time.
    // Shared by HeroWalk and by HeroCast, for a skill that lets its caster walk while casting.
    internal class HexStep
    {
        private readonly Hero _me;
        private readonly MovementConfig _movement;

        private Hex _targetHex;
        private CurveMotion _motion;
        private bool _isWalking;

        public HexStep(Hero me, MovementConfig movement)
        {
            _me = me;
            _movement = movement;
        }

        // still on the way: the hero is between two hexes
        public bool IsWalking => _isWalking;

        public void StartTo(Hex hex)
        {
            _targetHex = hex;
            _motion = new CurveMotion(
                start: _me.transform.position,
                end: hex.transform.position,
                duration: 1f / _movement.MoveSpeed,
                curve: _movement.WalkCurve);
            _isWalking = true;
        }

        // walk according the movement's curve; once there, hand the hero over to the hex it stepped onto
        public void Tick(float deltaTime)
        {
            if (!_isWalking) return;

            _me.transform.position = _motion.Tick(deltaTime);
            if (!_motion.IsFinished) return;

            ArriveAt(_targetHex);
            _isWalking = false;
        }

        // forget a step that was cut off (e.g. a stun) - the next one starts from wherever the hero is
        public void Cancel() => _isWalking = false;

        // A step cut off halfway (a stun, death): back onto the hex it left, and let go of the hex it was stepping onto.
        // Kept, that booking would never be used, and an enemy next to it waits for him to arrive there forever.
        // e.g.     two Minotaurs standing still next to the hex Skeleton Archer was stepping onto when he was stunned
        public void CutOff()
        {
            if (!_isWalking) return;
            _isWalking = false;

            if (_me.CurrentHex == null) return;
            _me.transform.position = _me.CurrentHex.transform.position;
            _me.SetReservedHex(_me.CurrentHex);
        }

        // A unit arrive at new hex
        // un owner to previous hex, set new owner to the new hex.
        // NOTE: this use same pattern as HeroMover & Move Template action
        private void ArriveAt(Hex hex)
        {
            // guard
            if (_me.CurrentHex == hex) return;

            IPlacement previous = _me.CurrentPlacement;
            if (previous != null) previous.OnUnitUnplaced(_me);
            hex.OnUnitPlaced(_me);
        }
    }
}
