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
        private HashSet<Hex> _special = new HashSet<Hex>();
        private HashSet<Hex> _wearing = new HashSet<Hex>();
        private bool _isShown;
        private (Hero hero, Hex standingOn, int pattern) _shownShape;     // the marks drawn now, when ShowMarks drew them
        private (Hero hero, Hex standingOn, ICombatant chosen, int tied) _shownTargetView;   // when ShowTargetView drew it

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
                Show(new HashSet<Hex>(), new HashSet<Hex>(), new HashSet<Hex>(), InDeployZone(AligningHex.WearingHexes(_board, _team)));
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
                else if (PlayerInputSystem.TargetViewHeld) ShowTargetView(hovered);
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
                Show(new HashSet<Hex>(), new HashSet<Hex>(), new HashSet<Hex>(), new HashSet<Hex>());
                return;
            }

            ShowMarks(held, pointed);
        }

        // Shift held over a placed hero: instead of its marks, the enemies tied for its nearest - a yellow band, the one the
        // player chose for it filled - and a dot on every hex it would walk through to reach that one. Shown apart from the
        // marks, which sit on the same hexes round the hero and would read as part of the route.
        // Right-clicking it again while inspected switches the target (See PlayerController.TrySwitchTarget).
        private void ShowTargetView(Hero hero)
        {
            IReadOnlyList<ICombatant> tied = hero.TiedNearestEnemies();
            ICombatant chosen = hero.ChosenTarget;

            // repaint only when something changed
            (Hero, Hex, ICombatant, int) view = (hero, hero.CurrentHex, chosen, tied.Count);
            if (_isShown && _shownTargetView == view) return;

            _board.Colours.ShowDeployZone(true);       // no marks

            if (tied.Count >= 2)
            {
                var tiedHexes = new List<Hex>();
                foreach (ICombatant enemy in tied)
                {
                    if (enemy.CurrentPlacement is Hex hex) tiedHexes.Add(hex);
                }

                _board.Colours.ShowTargets(tiedHexes, chosen?.CurrentPlacement as Hex);
                _board.Colours.ShowRoute(hero.RouteToChosenTarget());
            }

            _isShown = true;
            _shownShape = default;
            _shownTargetView = view;
        }

        // the hexes the hero marks while standing on [standingOn], each bonus in its colour.
        // The whole shape is drawn: a mark hex that gives nothing - on a red hex, past the board's edge, or off the board as a
        // gray hex - is still shown, faded, so the player sees where the mark is (See AlignShape, BoardColours.ShowAlignMarks).
        private void ShowMarks(Hero hero, Hex standingOn)
        {
            // repaint only when something moved
            (Hero, Hex, int) shape = (hero, standingOn, hero.AlignPattern);
            if (_isShown && _shownShape == shape) return;

            var onBoard = new Dictionary<Hex, MarkColours>();
            var paying = new HashSet<Hex>();
            var offBoard = new Dictionary<Vector2Int, (Vector3 position, MarkColours marks)>();

            foreach (AlignShape.ShownHex shown in AlignShape.ShownHexes(hero, standingOn, _board.Hexs.Values))
            {
                if (shown.IsOffBoard)
                {
                    // one gray hex per spot, however many marks land there
                    var spot = new Vector2Int(Mathf.RoundToInt(shown.position.x * 4f), Mathf.RoundToInt(shown.position.y * 4f));
                    offBoard.TryGetValue(spot, out var ghost);
                    offBoard[spot] = (shown.position, With(ghost.marks, shown, givesNothing: true));
                    continue;
                }

                onBoard.TryGetValue(shown.hex, out MarkColours marks);
                onBoard[shown.hex] = With(marks, shown, givesNothing: false);

                // a mark hex only pays inside the deploy zone, and only when its line did not leave the board on the way
                if (shown.hex.IsInDeployZone && !shown.isPastEdge) paying.Add(shown.hex);
            }

            foreach (Hex hex in new List<Hex>(onBoard.Keys))
            {
                MarkColours marks = onBoard[hex];
                marks.givesNothing = !paying.Contains(hex);
                onBoard[hex] = marks;
            }

            _board.Colours.ShowAlignMarks(onBoard, new List<(Vector3, MarkColours)>(offBoard.Values));
            _isShown = true;
            _shownShape = shape;
            _shownTargetView = default;
        }

        // [marks] with the colour of [shown] added
        private static MarkColours With(MarkColours marks, AlignShape.ShownHex shown, bool givesNothing)
        {
            if (shown.colour == AligningHex.Orange) marks.first = true;
            if (shown.colour == AligningHex.Green) marks.second = true;
            if (shown.colour == AligningHex.Special) marks.special = true;
            if (shown.isWearing) marks.wearing = true;
            marks.givesNothing = givesNothing;
            return marks;
        }

        // a player hero standing on a hex under the pointer - not one on a bench, and not an enemy: an enemy's marks do nothing
        private Hero HoveredPlacedHero()
        {
            Hero hovered = Picker.At<Hero>(PlayerInputSystem.GetMouseWorldPosition(_cam));

            return hovered != null && hovered.Team == _team && hovered.CurrentHex != null ? hovered : null;
        }

        // mark those hexes with different colour
        private void Show(HashSet<Hex> firstBonus, HashSet<Hex> secondBonus, HashSet<Hex> special, HashSet<Hex> wearing)
        {
            // repaint only when something moved
            if (_isShown && _shownShape.hero == null && _shownTargetView.hero == null && firstBonus.SetEquals(_firstBonus) && secondBonus.SetEquals(_secondBonus)
                && special.SetEquals(_special) && wearing.SetEquals(_wearing)) return;

            _firstBonus = firstBonus;
            _secondBonus = secondBonus;
            _special = special;
            _wearing = wearing;
            _isShown = true;
            _shownShape = default;
            _shownTargetView = default;
            _board.Colours.ShowAlignMarks(_firstBonus, _secondBonus, _special, _wearing);
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
            _shownShape = default;
            if (isPreparation) _board.Colours.ShowDeployZone(true);
        }
    }
}
