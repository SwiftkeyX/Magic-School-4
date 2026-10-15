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
        private HashSet<Hex> _wearing = new HashSet<Hex>();
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

            // holding an item: light up the hexes where a placed hero would wear it
            if (isPreparation && _dragging.HeldItem != null)
            {
                Show(new HashSet<Hex>(), new HashSet<Hex>(), InDeployZone(AligningHex.WearingHexes(_board, _team)));
                return;
            }

            if (!isPreparation)
            {
                Hide(isPreparation);
                return;
            }

            // holding nothing: a placed hero under the pointer shows the hexes it marks from where it stands
            if (held == null)
            {
                Hero hovered = HoveredPlacedHero();
                if (hovered == null) Hide(isPreparation);
                else ShowMarks(hovered, hovered.CurrentHex);
                return;
            }

            // an enemy held in the stage editor shows nothing: an enemy's marks do nothing
            if (held.Team != _team)
            {
                Hide(isPreparation);
                return;
            }

            // if holded hero was on top of the hex, get hexes that was going to be marked
            Hex pointed = PointedZoneHex();
            if (pointed == null)
            {
                Show(new HashSet<Hex>(), new HashSet<Hex>(), new HashSet<Hex>());
                return;
            }

            ShowMarks(held, pointed);
        }

        // the hexes the hero marks while standing on [standingOn], each bonus in its colour
        private void ShowMarks(Hero hero, Hex standingOn)
        {
            List<Hex>[] hexesByBonus = AligningHex.MarkedHexesByBonus(hero, standingOn);
            HashSet<Hex> firstBonus = InDeployZone(hexesByBonus[0]);
            HashSet<Hex> secondBonus = InDeployZone(hexesByBonus[1]);
            HashSet<Hex> wearing = InDeployZone(AligningHex.WearingHexes(hero, standingOn));

            Show(firstBonus, secondBonus, wearing);
        }

        // a player hero standing on a hex under the pointer - not one on a bench, and not an enemy: an enemy's marks do nothing
        private Hero HoveredPlacedHero()
        {
            Hero hovered = Picker.At<Hero>(PlayerInputSystem.GetMouseWorldPosition(_cam));

            return hovered != null && hovered.Team == _team && hovered.CurrentHex != null ? hovered : null;
        }

        // mark those hexes with different colour
        private void Show(HashSet<Hex> firstBonus, HashSet<Hex> secondBonus, HashSet<Hex> wearing)
        {
            // repaint only when something moved
            if (_isShown && firstBonus.SetEquals(_firstBonus) && secondBonus.SetEquals(_secondBonus)
                && wearing.SetEquals(_wearing)) return;

            _firstBonus = firstBonus;
            _secondBonus = secondBonus;
            _wearing = wearing;
            _isShown = true;
            _board.Colours.ShowAlignMarks(_firstBonus, _secondBonus, _wearing);
        }

        // the deploy-zone hex under the pointer: where the held hero would stand
        private Hex PointedZoneHex()
        {
            Vector3 worldPos = PlayerInputSystem.GetMouseWorldPosition(_cam);
            Hex pointed = Picker.At<Hex>(worldPos);

            return pointed != null && pointed.IsInDeployZone ? pointed : null;
        }

        // the marked hexes can only be in deploy zone.
        // (See DeployZone.cs)
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
            if (isPreparation) _board.Colours.ShowDeployZone(true);
        }
    }
}
