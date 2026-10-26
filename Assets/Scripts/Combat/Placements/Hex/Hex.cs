using System.Collections.Generic;
using UnityEngine;
using MagicSchool.Contracts;

namespace MagicSchool.Combat.Placements
{
    // A hero commits to a hex before it gets there: reserving keeps anyone else from walking in, while
    // CurrentPlacement and the position wait for the arrival. Unplacing drops the reservation only if it is still this
    // hero's: one that already claimed where it is going (a jump, a walk) leaves holding the next hex, and clearing
    // blindly would let someone take the landing spot.
    // Range is counted in hex hops by BFS, not world distance, because spacing is not uniform (same column ~1.0,
    // diagonal ~1.118) and a distance cutoff drifts as range grows. GetNeighbor finds a direction by angle: neighbours
    // sit ~53 or ~63 degrees apart, so 20 either way catches only one.
    public class Hex : MonoBehaviour, IPlacement
    {
        private BattleBoard _board;
        private HexNumber _hexPlacement;
        private List<Hex> _neighbors;
        private HexPaint _paint;

        public string Name => gameObject.name;
        public HexNumber Number => _hexPlacement;

        public bool IsInDeployZone { get; private set; }
        internal void SetInDeployZone(bool value) => IsInDeployZone = value;

        void Start()
        {
            InitializeNeighbors();
        }

        public void Init(BattleBoard board, HexNumber hexPlacement)
        {
            _board = board;
            _hexPlacement = hexPlacement;
            _neighbors = null;
        }

        public void OnUnitReserved(IPlaceable hero)
        {
            if (hero is IHexPlaceable hexDweller) hexDweller.SetReservedHex(this);
        }

        public void OnUnitPlaced(IPlaceable hero)
        {
            this.EnterPlacementExtension(hero);

            if (hero is IHexPlaceable hexDweller) hexDweller.SetReservedHex(this);
        }

        public void OnUnitUnplaced(IPlaceable hero)
        {
            if (hero is IHexPlaceable hexDweller && hexDweller.ReservedHex == this) hexDweller.SetReservedHex(null);
        }

        #region Neighbors
        public List<Hex> GetNeighbors()
        {
            if (_neighbors == null) InitializeNeighbors();

            return _neighbors;
        }

        public bool IsAdjacentTo(Hex other) => GetNeighbors().Contains(other);

        private const float DirectionTolerance = 20f;

        public Hex GetNeighbor(HexDirectionEnum direction)
        {
            float wanted = AngleOf(direction);

            foreach (Hex neighbor in GetNeighbors())
            {
                Vector2 offset = neighbor.transform.position - transform.position;
                float angle = Mathf.Atan2(offset.y, offset.x) * Mathf.Rad2Deg;

                if (Mathf.Abs(Mathf.DeltaAngle(angle, wanted)) <= DirectionTolerance) return neighbor;
            }

            return null;
        }

        public Hex GetNeighbor(HexDirectionEnum direction, int distance)
        {
            Hex hex = this;
            for (int step = 0; step < distance && hex != null; step++)
            {
                hex = hex.GetNeighbor(direction);
            }

            return hex;
        }

        private static float AngleOf(HexDirectionEnum direction)
        {
            switch (direction)
            {
                case HexDirectionEnum.N: return 90f;
                case HexDirectionEnum.NE: return 26.57f;
                case HexDirectionEnum.SE: return -26.57f;
                case HexDirectionEnum.S: return -90f;
                case HexDirectionEnum.SW: return -153.43f;
                default: return 153.43f;
            }
        }

        public bool IsWithinRange(Hex other, int range)
        {
            if (other == this) return true;

            var visited = new HashSet<Hex> { this };
            var frontier = new List<Hex> { this };

            for (int step = 0; step < range; step++)
            {
                var nextFrontier = new List<Hex>();
                foreach (var hex in frontier)
                {
                    foreach (var neighbor in hex.GetNeighbors())
                    {
                        if (!visited.Add(neighbor)) continue;
                        if (neighbor == other) return true;
                        nextFrontier.Add(neighbor);
                    }
                }
                frontier = nextFrontier;
            }

            return false;
        }

        private void InitializeNeighbors()
        {
            var hexs = _board.Hexs.Values;

            float nearestDist = float.MaxValue;
            foreach (var hex in hexs)
            {
                if (hex == this) continue;
                float dist = Vector3.Distance(transform.position, hex.transform.position);
                if (dist < nearestDist)
                    nearestDist = dist;
            }

            float threshold = nearestDist * 1.15f;
            _neighbors = new List<Hex>();
            foreach (var hex in hexs)
            {
                if (hex == this) continue;
                if (Vector3.Distance(transform.position, hex.transform.position) <= threshold)
                    _neighbors.Add(hex);
            }
        }
        #endregion

        internal void SetColor(Color color) => SetColors(new[] { color });

        internal void SetColors(IReadOnlyList<Color> colours)
        {
            if (_paint == null) _paint = new HexPaint(GetComponent<SpriteRenderer>());
            _paint.SetColors(colours);
        }

        internal SpriteRenderer Sprite => GetComponent<SpriteRenderer>();
    }
}
