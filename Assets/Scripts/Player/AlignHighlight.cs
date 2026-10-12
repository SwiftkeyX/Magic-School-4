using System.Collections.Generic;
using UnityEngine;
using MagicSchool.Contracts;
using MagicSchool.Core;
using MagicSchool.Combat.Align;
using MagicSchool.Combat.Heroes;
using MagicSchool.Combat.Placements;
using MagicSchool.Input;

namespace MagicSchool.Player
{
    // Aligning, while the player holds one of his heroes, light up the marked hex
    internal class AlignHighlight
    {
        private readonly Camera _cam;
        private readonly Dragging _dragging;
        private readonly BattleBoard _board;
        private readonly TeamEnum _team;

        private HashSet<Hex> _marked = new HashSet<Hex>();
        private HashSet<Hex> _markedByHeld = new HashSet<Hex>();
        private bool _isShown;

        public AlignHighlight(Camera cam, Dragging dragging, BattleBoard board, TeamEnum team)
        {
            _cam = cam;
            _dragging = dragging;
            _board = board;
            _team = team;
        }

        public void Tick()
        {
            bool isPreparation = GameManager.Instance.Phase == GamePhaseEnum.Preparation;
            Hero held = _dragging.HeldHero;

            // an enemy held in the stage editor shows nothing: an enemy's marks do nothing
            if (!isPreparation || held == null || held.Team != _team)
            {
                Hide(isPreparation);
                return;
            }

            HashSet<Hex> marked = InZone(AligningHex.MarkedHexes(_board, _team, except: held));
            HashSet<Hex> markedByHeld = InZone(MarkedFromPointer(held));

            // repaint only when something moved
            if (_isShown && marked.SetEquals(_marked) && markedByHeld.SetEquals(_markedByHeld)) return;

            _marked = marked;
            _markedByHeld = markedByHeld;
            _isShown = true;
            _board.ShowAlignMarks(_marked, _markedByHeld);
        }

        // what the held hero would mark, dropped on the hex under the pointer
        private IEnumerable<Hex> MarkedFromPointer(Hero held)
        {
            Vector3 worldPos = PlayerInputSystem.GetMouseWorldPosition(_cam);
            Hex pointed = Picker.At<Hex>(worldPos);

            // the held hero can only be dropped inside the zone
            if (pointed == null || !pointed.IsInDeployZone) return new List<Hex>();

            return AligningHex.MarkedHexes(held, pointed);
        }

        private static HashSet<Hex> InZone(IEnumerable<Hex> hexes)
        {
            var inZone = new HashSet<Hex>();
            foreach (Hex hex in hexes)
            {
                if (hex.IsInDeployZone) inZone.Add(hex);
            }

            return inZone;
        }

        // back to the plain deploy zone. Outside Preparation the board has already repainted itself.
        private void Hide(bool isPreparation)
        {
            if (!_isShown) return;

            _isShown = false;
            if (isPreparation) _board.ShowDeployZone(true);
        }
    }
}
