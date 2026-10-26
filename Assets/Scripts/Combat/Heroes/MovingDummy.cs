using UnityEngine;
using MagicSchool.Combat.Placements;

namespace MagicSchool.Combat.Heroes
{
    /// FLAGGING: This is temp
    /// <summary>
    /// A dummy that patrols up and down its column, hex by hex, the same way a hero walks:
    /// </summary>
    // Walks the way a hero does: it claims the next hex before moving and only owns it on arrival (OnUnitPlaced snaps
    // it to the centre). It turns round at the board's ends and at anything taken, and waits when boxed in both ways.
    internal class MovingDummy : MonoBehaviour
    {
        [SerializeField] private float _speed = 1f;

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

            if (!_me.IsBattleOn) return;

            if (_to == null && !TryStartStep()) return;

            _elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(_elapsed / _duration);
            _me.transform.position = Vector3.Lerp(_from.transform.position, _to.transform.position, t);

            if (t < 1f) return;

            _from.OnUnitUnplaced(_me);
            _to.OnUnitPlaced(_me);
            _to = null;
        }

        private bool TryStartStep()
        {
            Hex next = NeighborInColumn(_goingUp);

            if (next == null || _me.IsHexReservedByOther(next))
            {
                _goingUp = !_goingUp;
                next = NeighborInColumn(_goingUp);

                if (next == null || _me.IsHexReservedByOther(next)) return false;
            }

            _from = _me.CurrentHex;
            _to = next;
            _elapsed = 0f;
            _duration = 1f / _speed;

            next.OnUnitReserved(_me);
            return true;
        }

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
