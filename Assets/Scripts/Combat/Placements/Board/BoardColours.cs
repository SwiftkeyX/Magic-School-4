using System;
using System.Collections.Generic;
using UnityEngine;

namespace MagicSchool.Combat.Placements
{
    /// <summary>
    /// Answer what colour is this hex?
    /// e.g.    Preparation     => the deploy zone blue, others red
    ///         a hero is held  => the hexes it would mark orange/green/white, its wearing hexes purple - a hex in several is drawn in rings
    ///         an item is held => the team's wearing hexes purple
    ///         the fight       => the whole board gray
    /// </summary>
    // A hex in several marks shows every colour as rings: purple outermost, then orange, green, white, the order the
    // Align Editor draws them. A mark hex that gives nothing (red, or a gray spot off the board) keeps its own colour
    // outside with the mark's colours as faded rings inside, so it reads as where the mark is and never as a hex that
    // pays. Anything that repaints the board takes the route dots away.
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

        [Tooltip("Aligning, while a hero or an item is held: a wearing hex - an item lying there is worn. Painted over the bonus colours.")]
        [SerializeField] private Color _wearingColor = new Color(0.85f, 0.4f, 1f);

        [Tooltip("Aligning, while a hero is held: a hex of the held hero's special mark - the one its skill reads (See AlignMark).")]
        [SerializeField] private Color _specialColor = Color.white;

        [Tooltip("Aligning, while a hero is held: a hex of its marks that is past the board's edge - drawn only to show the mark's shape.")]
        [SerializeField] private Color _ghostColor = new Color(0.5f, 0.5f, 0.5f, 0.6f);

        [Tooltip("Preparation, hovering a placed hero whose nearest enemy is a tie: the tied enemies' hexes get a ring of this colour, " +
                 "and the one the player chose for it is filled with it.")]
        [SerializeField] private Color _targetColor = new Color(1f, 0.85f, 0.1f);

        [Tooltip("A mark hex that gives nothing (off the board, outside the deploy zone, or past the edge) is drawn as its own colour " +
                 "ringed in the mark's colours, faded this much toward its own colour.")]
        [Range(0f, 1f)]
        [SerializeField] private float _givesNothingFade = 0.35f;

        private IReadOnlyDictionary<HexNumber, Hex> _hexs;
        private GhostHexes _ghosts;
        private RouteDots _route;
        internal void Init(IReadOnlyDictionary<HexNumber, Hex> hexs, Transform board)
        {
            _hexs = hexs;
            _ghosts = new GhostHexes(board);
            _route = new RouteDots(board);
        }

        public void ShowDeployZone(bool isShown)
        {
            _ghosts?.HideAll();
            _route?.HideAll();

            foreach (Hex hex in _hexs.Values)
            {
                hex.SetColor(isShown ? ZoneColorOf(hex) : _neutralColor);
            }
        }

        internal void ShowDeployZone(Hex hex) => hex.SetColor(ZoneColorOf(hex));

        public void ShowAlignMarks(ICollection<Hex> firstBonus, ICollection<Hex> secondBonus, ICollection<Hex> special, ICollection<Hex> wearing)
        {
            var onBoard = new Dictionary<Hex, MarkColours>();
            foreach (Hex hex in _hexs.Values)
            {
                onBoard[hex] = new MarkColours
                {
                    wearing = wearing.Contains(hex),
                    first = firstBonus.Contains(hex),
                    second = secondBonus.Contains(hex),
                    special = special.Contains(hex),
                };
            }

            ShowAlignMarks(onBoard, new List<(Vector3, MarkColours)>());
        }

        public void ShowAlignMarks(IReadOnlyDictionary<Hex, MarkColours> onBoard, IReadOnlyList<(Vector3 position, MarkColours marks)> offBoard)
        {
            foreach (Hex hex in _hexs.Values)
            {
                onBoard.TryGetValue(hex, out MarkColours marks);
                hex.SetColors(ColoursOf(marks, ZoneColorOf(hex)));
            }

            var ghosts = new List<(Vector3, IReadOnlyList<Color>)>();
            foreach ((Vector3 position, MarkColours marks) in offBoard) ghosts.Add((position, ColoursOf(marks, _ghostColor)));

            Hex template = null;
            foreach (Hex hex in _hexs.Values) { template = hex; break; }
            _ghosts?.Show(ghosts, template);
            _route?.HideAll();
        }

        public void ShowTargets(IEnumerable<Hex> tied, Hex chosen, bool onZone = true)
        {
            foreach (Hex hex in tied)
            {
                Color own = onZone ? ZoneColorOf(hex) : _neutralColor;
                hex.SetColors(hex == chosen ? new[] { _targetColor } : new[] { own, _targetColor, own });
            }
        }

        public void ShowRoute(IReadOnlyList<Hex> route) => _route?.Show(route, _targetColor);

        private List<Color> ColoursOf(MarkColours marks, Color own)
        {
            var colours = new List<Color>();
            if (marks.wearing) colours.Add(_wearingColor);
            if (marks.first) colours.Add(_firstBonusColor);
            if (marks.second) colours.Add(_secondBonusColor);
            if (marks.special) colours.Add(_specialColor);

            if (!marks.givesNothing || colours.Count == 0)
            {
                if (colours.Count == 0) colours.Add(own);
                return colours;
            }

            for (int i = 0; i < colours.Count; i++) colours[i] = Color.Lerp(colours[i], own, _givesNothingFade);
            colours.Insert(0, own);
            return colours;
        }

        private Color ZoneColorOf(Hex hex) => hex.IsInDeployZone ? _zoneColor : _outsideColor;
    }

    /// <summary>
    /// Which mark colours one hex is shown in, and whether it is there only to show the shape.
    /// e.g.    Elf's N hex in the zone         => first + second
    /// e.g.    the same hex on a red hex       => first + second + givesNothing
    /// </summary>
    public struct MarkColours
    {
        public bool wearing;
        public bool first;
        public bool second;
        public bool special;
        public bool givesNothing;

        public bool IsMarked => wearing || first || second || special;
    }
}
