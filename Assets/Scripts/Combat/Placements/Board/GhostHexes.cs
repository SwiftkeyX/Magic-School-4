using System.Collections.Generic;
using UnityEngine;

namespace MagicSchool.Combat.Placements
{
    /// <summary>
    /// The gray hexes drawn past the board's edge while a hero's marks are shown, so a mark that falls off the board
    /// still shows where it would be (See AlignShape). They are only sprites: not a Hex, not on the board,
    /// nothing walks on them, places on them or picks them - and nothing is drawn while no marks are shown.
    /// e.g.    Shield Knight on the top row, his mark N 2 => a gray hex above the board, ringed in the mark's colour
    /// </summary>
    internal class GhostHexes
    {
        private readonly Transform _board;
        private Transform _holder;
        private readonly List<(SpriteRenderer sprite, HexPaint paint)> _pool = new List<(SpriteRenderer, HexPaint)>();

        public GhostHexes(Transform board) => _board = board;

        public void Show(IReadOnlyList<(Vector3 position, IReadOnlyList<Color> colours)> spots, Hex template)
        {
            if (template == null) { HideAll(); return; }

            for (int i = 0; i < spots.Count; i++)
            {
                (SpriteRenderer sprite, HexPaint paint) = GhostAt(i, template);
                Vector3 position = spots[i].position;
                sprite.transform.position = new Vector3(position.x, position.y, template.transform.position.z);
                sprite.gameObject.SetActive(true);
                paint.SetColors(spots[i].colours);
            }

            for (int i = spots.Count; i < _pool.Count; i++) _pool[i].sprite.gameObject.SetActive(false);
        }

        public void HideAll()
        {
            foreach ((SpriteRenderer sprite, HexPaint _) in _pool)
            {
                if (sprite != null) sprite.gameObject.SetActive(false);
            }
        }

        private (SpriteRenderer, HexPaint) GhostAt(int i, Hex template)
        {
            if (_holder == null)
            {
                _holder = new GameObject("Ghost Hexes").transform;
                _holder.SetParent(_board, worldPositionStays: false);
                _pool.Clear();
            }

            while (_pool.Count <= i)
            {
                var sprite = new GameObject($"Ghost Hex {_pool.Count}").AddComponent<SpriteRenderer>();
                sprite.transform.SetParent(_holder, worldPositionStays: false);
                _pool.Add((sprite, new HexPaint(sprite)));
            }

            SpriteRenderer ghost = _pool[i].sprite;
            SpriteRenderer look = template.Sprite;
            ghost.sprite = look.sprite;
            ghost.sortingLayerID = look.sortingLayerID;
            ghost.sortingOrder = look.sortingOrder;
            ghost.transform.rotation = template.transform.rotation;
            ghost.transform.localScale = Vector3.one;
            ghost.transform.localScale = new Vector3(
                template.transform.lossyScale.x / ghost.transform.lossyScale.x,
                template.transform.lossyScale.y / ghost.transform.lossyScale.y, 1f);

            return _pool[i];
        }
    }
}
