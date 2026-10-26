using UnityEngine;
using MagicSchool.Contracts;
using MagicSchool.Engine;
using MagicSchool.Combat.Placements;

namespace MagicSchool.Combat.Heroes.States
{
    // One step from the hex a hero stands on to a neighbour it has already reserved, one frame at a time.
    // Shared by HeroWalk and by HeroCast, for a skill that lets its caster walk while casting.
    // A unit is handed its new hex only when the step ends. A step cut off halfway (a stun, death) puts the hero back
    // on the hex it left and lets go of the one it was stepping onto: kept, that booking is never used and an enemy
    // next to it waits forever (two Minotaurs once stood still beside the hex a stunned Skeleton Archer was stepping
    // onto).
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

        public void Tick(float deltaTime)
        {
            if (!_isWalking) return;

            _me.transform.position = _motion.Tick(deltaTime);
            if (!_motion.IsFinished) return;

            ArriveAt(_targetHex);
            _isWalking = false;
        }

        public void Cancel() => _isWalking = false;

        public void CutOff()
        {
            if (!_isWalking) return;
            _isWalking = false;

            if (_me.CurrentHex == null) return;
            _me.transform.position = _me.CurrentHex.transform.position;
            _me.SetReservedHex(_me.CurrentHex);
        }

        private void ArriveAt(Hex hex)
        {
            if (_me.CurrentHex == hex) return;

            IPlacement previous = _me.CurrentPlacement;
            if (previous != null) previous.OnUnitUnplaced(_me);
            hex.OnUnitPlaced(_me);
        }
    }
}
