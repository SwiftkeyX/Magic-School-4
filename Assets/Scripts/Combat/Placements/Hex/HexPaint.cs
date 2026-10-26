using System.Collections.Generic;
using UnityEngine;

namespace MagicSchool.Combat.Placements
{
    /// <summary>
    /// Paints one hex sprite: one colour fills it, more are drawn as rings inside it, the first outermost.
    /// Shared by the board's hexes and the gray hexes drawn past its edge (See GhostHexes), so both look the same.
    /// e.g.    an Elf's mark hex => orange outside, green inside: an ally there gets the green bonus and pays her the orange one
    /// </summary>
    // Rings are lifted RingLift toward the camera: over their hex, still under every hero (order 1). With many colours
    // the ring step shrinks to fit, never below InnerMost.
    internal class HexPaint
    {
        private readonly SpriteRenderer _sprite;
        private readonly List<SpriteRenderer> _rings = new List<SpriteRenderer>();

        private const float RingStep = 0.3f;
        private const float InnerMost = 0.15f;
        private const float RingLift = 0.01f;

        public HexPaint(SpriteRenderer sprite) => _sprite = sprite;

        public void SetColors(IReadOnlyList<Color> colours)
        {
            if (_sprite == null || colours.Count == 0) return;

            _sprite.color = colours[0];

            float step = colours.Count <= 1 ? RingStep : Mathf.Min(RingStep, (1f - InnerMost) / (colours.Count - 1));
            for (int i = 1; i < colours.Count; i++)
            {
                SpriteRenderer ring = RingAt(i);
                ring.transform.localScale = Vector3.one * (1f - step * i);
                ring.color = colours[i];
                ring.enabled = true;
            }

            for (int i = colours.Count; i <= _rings.Count; i++) _rings[i - 1].enabled = false;
        }

        private SpriteRenderer RingAt(int i)
        {
            while (_rings.Count < i)
            {
                int depth = _rings.Count + 1;
                var ring = new GameObject($"Ring {depth}").AddComponent<SpriteRenderer>();
                ring.transform.SetParent(_sprite.transform, worldPositionStays: false);
                ring.transform.localPosition = new Vector3(0f, 0f, -RingLift * depth);
                ring.sprite = _sprite.sprite;
                ring.sortingLayerID = _sprite.sortingLayerID;
                ring.sortingOrder = _sprite.sortingOrder;
                _rings.Add(ring);
            }

            return _rings[i - 1];
        }
    }
}
