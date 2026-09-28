using UnityEngine;
using MagicSchool.Combat.Placements;

namespace MagicSchool.Combat.Heroes
{
    /// FLAGGING: This is temp
    /// <summary>
    /// A dummy that patrols up and down its column, hex by hex, the same way a hero walks:
    /// </summary>
    internal class MovingDummy : MonoBehaviour
    {
        [SerializeField] private float _speed = 1f;     // hexes per second, same units as a hero's move speed

        private Hero _me;

        private Hex _from;
        private Hex _to;
        private float _elapsed;
        private float _duration;
        private bool _goingUp = true;

        void Awake()
        {
            _me = GetComponent<Hero>();
        }

        void Update()
        {
            if (_me == null || !_me.IsInitialized || _me.CurrentHex == null) return;

            // stand still until the battle is on, same as a hero
            if (!_me.IsBattleOn) return;

            // between steps: line up the next one, or wait if there's nowhere to go
            if (_to == null && !TryStartStep()) return;

            _elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(_elapsed / _duration);
            _me.transform.position = Vector3.Lerp(_from.transform.position, _to.transform.position, t);

            if (t < 1f) return;

            // arrive - the hex is only really ours once we're standing on it. OnUnitPlaced does
            // the snap to the centre for us, same as it does for a walking hero.
            _from.OnUnitUnplaced(_me);
            _to.OnUnitPlaced(_me);
            _to = null;
        }

        // ======================================== private ========================================
        // Next hex straight up or down the column, turning round at the ends of the board and at
        // anything already taken.
        private bool TryStartStep()
        {
            Hex next = NeighborInColumn(_goingUp);

            if (next == null || _me.IsHexReservedByOther(next))
            {
                _goingUp = !_goingUp;
                next = NeighborInColumn(_goingUp);

                // boxed in both ways - stay put and try again next frame
                if (next == null || _me.IsHexReservedByOther(next)) return false;
            }

            _from = _me.CurrentHex;
            _to = next;
            _elapsed = 0f;
            _duration = 1f / _speed;

            // claim it before moving, exactly as a walking hero does
            next.OnUnitReserved(_me);
            return true;
        }

        // Same column means same x; one row up or down is the neighbour that shares it.
        private Hex NeighborInColumn(bool up)
        {
            Hex current = _me.CurrentHex;

            foreach (Hex neighbor in current.GetNeighbors())
            {
                if (!Mathf.Approximately(neighbor.transform.position.x, current.transform.position.x)) continue;

                bool isAbove = neighbor.transform.position.y > current.transform.position.y;
                if (isAbove == up) return neighbor;
            }

            return null;
        }
    }
}
