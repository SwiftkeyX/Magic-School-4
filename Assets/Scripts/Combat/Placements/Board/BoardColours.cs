using System;
using System.Collections.Generic;
using UnityEngine;

namespace MagicSchool.Combat.Placements
{
    /// <summary>
    /// Answer what colour is this hex?
    /// e.g.    Preparation     => the deploy zone blue, others red
    ///         a hero is held  => the hexes it would mark orange/green
    ///         the fight       => the whole board gray
    /// </summary>
    [Serializable]
    public class BoardColours
    {
        [Tooltip("In Preparation, Deploy zone's color.")]
        [SerializeField] private Color _zoneColor = new Color(0.353f, 0.608f, 1f);

        [Tooltip("In Preparation, outside of deploy zone's color")]
        [SerializeField] private Color _outsideColor = new Color(1f, 0.353f, 0.353f);

        [Tooltip("Outside Preparation, every hex's color")]
        [SerializeField] private Color _neutralColor = new Color(0.45f, 0.52f, 0.65f);

        [Tooltip("Aligning, while a hero is held: a hex the held hero would mark with its first bonus.")]
        [SerializeField] private Color _firstBonusColor = new Color(1f, 0.62f, 0.2f);

        [Tooltip("Aligning, while a hero is held: a hex the held hero would mark with its second bonus.")]
        [SerializeField] private Color _secondBonusColor = new Color(0.4f, 0.9f, 0.5f);

        private IReadOnlyDictionary<HexNumber, Hex> _hexs;
        internal void Init(IReadOnlyDictionary<HexNumber, Hex> hexs) => _hexs = hexs;

        // Repaint the whole deploy zone
        public void ShowDeployZone(bool isShown)
        {
            foreach (Hex hex in _hexs.Values)
            {
                hex.SetColor(isShown ? ZoneColorOf(hex) : _neutralColor);
            }
        }

        // Repaint one of the hex 
        // e.g.     stage editor mode, the hex was just put in or out of the zone
        internal void ShowDeployZone(Hex hex) => hex.SetColor(ZoneColorOf(hex));

        // Repaint hexes that was marked (override deploy zone)
        public void ShowAlignMarks(ICollection<Hex> firstBonus, ICollection<Hex> secondBonus)
        {
            foreach (Hex hex in _hexs.Values)
            {
                if (firstBonus.Contains(hex)) hex.SetColor(_firstBonusColor);

                else if (secondBonus.Contains(hex)) hex.SetColor(_secondBonusColor);

                else hex.SetColor(ZoneColorOf(hex));
            }
        }

        private Color ZoneColorOf(Hex hex) => hex.IsInDeployZone ? _zoneColor : _outsideColor;
    }
}
