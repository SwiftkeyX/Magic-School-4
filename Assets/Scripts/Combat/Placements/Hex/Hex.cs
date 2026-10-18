using System.Collections.Generic;
using UnityEngine;
using MagicSchool.Contracts;

namespace MagicSchool.Combat.Placements
{
    public class Hex : MonoBehaviour, IPlacement
    {
        private BattleBoard _board;
        private HexNumber _hexPlacement;
        private List<Hex> _neighbors;   // neighbors hex of this hex - use in pathfinding logic
        private HexPaint _paint;        // its colour, and the rings of a hex shown in more than one (See SetColors)

        // ========================== getter & setter ==========================
        public string Name => gameObject.name;
        public HexNumber Number => _hexPlacement;

        // === deploy zone ===
        // Is this hex the one player can put a hero on? 
        // (See DeployZone.cs)
        public bool IsInDeployZone { get; private set; }
        internal void SetInDeployZone(bool value) => IsInDeployZone = value;

        // ===================================== life cycle =====================================
        void Start()
        {
            InitializeNeighbors();
        }

        // ===================================== setup =====================================
        public void Init(BattleBoard board, HexNumber hexPlacement)
        {
            _board = board;
            _hexPlacement = hexPlacement;
            _neighbors = null;
        }

        // ===================================== placement interface =====================================
        // A hero commits to this hex before it gets here - reserve it so nobody else walks in,
        // but leave CurrentPlacement (and the hero's position) alone until it actually arrives.
        public void OnUnitReserved(IPlaceable hero)
        {
            if (hero is IHexPlaceable hexDweller) hexDweller.SetReservedHex(this);
        }

        // place hero on Hex
        public void OnUnitPlaced(IPlaceable hero)
        {
            // set placement for the hero normally
            this.EnterPlacementExtension(hero);

            // for hex, set reserve for the hex too
            // FLAGGING: it kinda smelled but this is all the logic for IHexPlaceable. 
            // So it would be overengineer if we go further than this.
            if (hero is IHexPlaceable hexDweller) hexDweller.SetReservedHex(this);
        }

        // unplace hero from Hex
        public void OnUnitUnplaced(IPlaceable hero)
        {
            // Only drop the reservation if it's still MINE. A hero that already claimed where it's
            // going (a jump, a walk) leaves this hex holding the next one - clearing blindly here
            // would throw that claim away and let someone else take the landing spot.
            if (hero is IHexPlaceable hexDweller && hexDweller.ReservedHex == this) hexDweller.SetReservedHex(null);
        }


        // ===================================== Neighbors =====================================
        // Neighbors are the hex adjacent to current hex.
        #region Neighbors
        // called by Hero - so hero know which hex is valid to move
        public List<Hex> GetNeighbors()
        {
            if (_neighbors == null) InitializeNeighbors();

            return _neighbors;
        }

        public bool IsAdjacentTo(Hex other) => GetNeighbors().Contains(other);

        // The 6 neighbors sit ~53 or ~63 degrees apart, so 20 either way can only catch one.
        private const float DirectionTolerance = 20f;

        // Get the hex from specify direction
        public Hex GetNeighbor(HexDirectionEnum direction)
        {
            float wanted = AngleOf(direction);

            // Find the hex using angle from itself to target
            foreach (Hex neighbor in GetNeighbors())
            {
                Vector2 offset = neighbor.transform.position - transform.position;
                float angle = Mathf.Atan2(offset.y, offset.x) * Mathf.Rad2Deg;

                if (Mathf.Abs(Mathf.DeltaAngle(angle, wanted)) <= DirectionTolerance) return neighbor;
            }

            return null;
        }

        // Get the hex from specify direction by [distance]
        public Hex GetNeighbor(HexDirectionEnum direction, int distance)
        {
            Hex hex = this;
            for (int step = 0; step < distance && hex != null; step++)
            {
                hex = hex.GetNeighbor(direction);
            }

            return hex;
        }

        // Get the angle from specify direction enum
        private static float AngleOf(HexDirectionEnum direction)
        {
            switch (direction)
            {
                case HexDirectionEnum.N: return 90f;
                case HexDirectionEnum.NE: return 26.57f;
                case HexDirectionEnum.SE: return -26.57f;
                case HexDirectionEnum.S: return -90f;
                case HexDirectionEnum.SW: return -153.43f;
                default: return 153.43f;    // NW
            }
        }

        // Generalizes IsAdjacentTo to N hex-hops via BFS, for heroes whose attack range is > 1.
        // BFS over the neighbor graph rather than raw world-space distance, since hex spacing
        // isn't uniform (same-column neighbors are ~1.0 apart, diagonal ~1.118 apart) - a
        // distance-based cutoff would drift as range grows, hop-counting can't.
        // FLAGGING: Now we have 2 standard of measuring the hex distance, we do Vector3.Distance() & count the hex using BFS. 
        // => We should use the same standard NOT scatter like this, no?
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

        // Neighbors don't create itself, we need to calculate it ourself
        private void InitializeNeighbors()
        {
            var hexs = _board.Hexs.Values;

            // find distance between current hex and every hex
            float nearestDist = float.MaxValue;
            foreach (var hex in hexs)
            {
                if (hex == this) continue;
                float dist = Vector3.Distance(transform.position, hex.transform.position);
                if (dist < nearestDist)
                    nearestDist = dist;
            }

            // create neighbors of hex as a list
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

        // ===================================== other =====================================
        internal void SetColor(Color color) => SetColors(new[] { color });

        // One colour fills the hex; more are drawn as rings inside it, the first outermost (See HexPaint)
        internal void SetColors(IReadOnlyList<Color> colours)
        {
            if (_paint == null) _paint = new HexPaint(GetComponent<SpriteRenderer>());
            _paint.SetColors(colours);
        }

        // the hex's sprite, for anything drawn to look like one (See GhostHexes)
        internal SpriteRenderer Sprite => GetComponent<SpriteRenderer>();
    }
}
