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
    // Aligning, while the player holds one of his heroes, hightlight the marked hex
    internal class AlignHighlight
    {
        private readonly Camera _cam;
        private readonly Dragging _dragging;
        private readonly BattleBoard _board;
        private readonly TeamEnum _team;

        private HashSet<Hex> _firstBonus = new HashSet<Hex>();
        private HashSet<Hex> _secondBonus = new HashSet<Hex>();
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

            // if holded hero was on top of the hex, get hexes that was going to be marked
            List<Hex>[] hexesByBonus = MarkedFromPointer(held);
            HashSet<Hex> firstBonus = InDeployZone(hexesByBonus[0]);
            HashSet<Hex> secondBonus = InDeployZone(hexesByBonus[1]);

            // repaint only when something moved
            if (_isShown && firstBonus.SetEquals(_firstBonus) && secondBonus.SetEquals(_secondBonus)) return;

            // mark those hexes with different colour 
            _firstBonus = firstBonus;
            _secondBonus = secondBonus;
            _isShown = true;
            _board.ShowAlignMarks(_firstBonus, _secondBonus);
        }

        // if hero was holded on top of the hex, get hexes that was going to be marked
        private List<Hex>[] MarkedFromPointer(Hero held)
        {
            Vector3 worldPos = PlayerInputSystem.GetMouseWorldPosition(_cam);
            Hex pointed = Picker.At<Hex>(worldPos);

            if (pointed == null || !pointed.IsInDeployZone) return new[] { new List<Hex>(), new List<Hex>() };

            return AligningHex.MarkedHexesByBonus(held, pointed);
        }

        // the marked hexes can only be in deploy zone.
        // (See BattleBoard.SetDeployZone)
        private static HashSet<Hex> InDeployZone(IEnumerable<Hex> hexes)
        {
            var inZone = new HashSet<Hex>();
            foreach (Hex hex in hexes)
            {
                if (hex.IsInDeployZone) inZone.Add(hex);
            }

            return inZone;
        }

        // Hide the highlight of the marked hex
        private void Hide(bool isPreparation)
        {
            if (!_isShown) return;

            _isShown = false;
            if (isPreparation) _board.ShowDeployZone(true);
        }
    }
}
