using System.Collections.Generic;
using UnityEngine;

namespace MagicSchool.Combat.Placements
{
    /// <summary>
    /// The dots drawn on the hexes a hero would walk through to reach the enemy it will go for, in Preparation
    /// (See Hero.RouteToExpectedTarget). Only sprites - small copies of a board hex - so they need no shader of their own.
    /// e.g.    Orc chose the Big Slime on the far side => a dot on every hex between them, the way he would go round his allies
    /// </summary>
    internal class RouteDots
    {
        private const float DotSize = 0.3f;
        private const float Lift = 0.1f;

        private readonly Transform _board;
        private Transform _holder;
        private readonly List<SpriteRenderer> _pool = new List<SpriteRenderer>();

        public RouteDots(Transform board) => _board = board;

        public void Show(IReadOnlyList<Hex> route, Color colour)
        {
            for (int i = 0; i < route.Count; i++)
            {
                SpriteRenderer dot = DotAt(i, route[i]);
                Vector3 position = route[i].transform.position;
                dot.transform.position = new Vector3(position.x, position.y, position.z - Lift);
                dot.color = colour;
                dot.gameObject.SetActive(true);
            }

            for (int i = route.Count; i < _pool.Count; i++)
            {
                if (_pool[i] != null) _pool[i].gameObject.SetActive(false);
            }
        }

        public void HideAll()
        {
            foreach (SpriteRenderer dot in _pool)
            {
                if (dot != null) dot.gameObject.SetActive(false);
            }
        }

        private SpriteRenderer DotAt(int i, Hex template)
        {
            if (_holder == null)
            {
                _holder = new GameObject("Route Dots").transform;
                _holder.SetParent(_board, worldPositionStays: false);
                _pool.Clear();
            }

            while (_pool.Count <= i)
            {
                var dot = new GameObject($"Route Dot {_pool.Count}").AddComponent<SpriteRenderer>();
                dot.transform.SetParent(_holder, worldPositionStays: false);
                _pool.Add(dot);
            }

            SpriteRenderer look = template.Sprite;
            SpriteRenderer made = _pool[i];
            made.sprite = look.sprite;
            made.sharedMaterial = look.sharedMaterial;
            made.sortingLayerID = look.sortingLayerID;
            made.sortingOrder = look.sortingOrder;
            made.transform.rotation = template.transform.rotation;
            made.transform.localScale = Vector3.one;
            made.transform.localScale = new Vector3(
                template.transform.lossyScale.x * DotSize / made.transform.lossyScale.x,
                template.transform.lossyScale.y * DotSize / made.transform.lossyScale.y, 1f);

            return made;
        }
    }
}
