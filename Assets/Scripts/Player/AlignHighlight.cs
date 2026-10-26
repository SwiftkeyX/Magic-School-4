using System.Collections.Generic;
using System.Linq;
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
    // A mark hex pays only inside the deploy zone, and only when its line did not leave the board on the way; one that
    // gives nothing is still drawn, faded, so the player sees where the mark is. The Shift view hides the marks: they
    // sit on the hexes round the hero and would read as part of the route. In a fight the same view is a debug tool
    // over any hero, repainted live (e.g. why did Reaper turn back? the hexes next to her target were taken by her
    // allies).
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
        private (Hero hero, Hex standingOn, int pattern) _shownShape;
        private (Hero hero, Hex standingOn, ICombatant chosen, int tied) _shownTargetView;
        private List<Hex> _shownFightView;

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

            if (isPreparation && _dragging.HeldItem != null)
            {
                Show(new HashSet<Hex>(), new HashSet<Hex>(), new HashSet<Hex>(), InDeployZone(AligningHex.WearingHexes(_board, _team)));
                return;
            }

            if (!isPreparation)
            {
                Hero inFight = GameManager.Instance.Phase == GamePhaseEnum.Combat && PlayerInputSystem.TargetViewHeld ? HoveredHeroInFight() : null;
                if (inFight != null) ShowFightTargetView(inFight);
                else Hide(isPreparation);
                return;
            }

            if (held == null)
            {
                Hero hovered = HoveredPlacedHero();
                if (hovered == null) Hide(isPreparation);
                else if (PlayerInputSystem.TargetViewHeld) ShowTargetView(hovered);
                else ShowMarks(hovered, hovered.CurrentHex);
                return;
            }

            if (held.Team != _team)
            {
                Hide(isPreparation);
                return;
            }

            Hex pointed = PointedZoneHex();
            if (pointed == null)
            {
                Show(new HashSet<Hex>(), new HashSet<Hex>(), new HashSet<Hex>(), new HashSet<Hex>());
                return;
            }

            ShowMarks(held, pointed);
        }

        private void ShowTargetView(Hero hero)
        {
            IReadOnlyList<ICombatant> tied = hero.TiedNearestEnemies();
            ICombatant expected = hero.ExpectedTarget;

            (Hero, Hex, ICombatant, int) view = (hero, hero.CurrentHex, expected, tied.Count);
            if (_isShown && _shownTargetView == view) return;

            _board.Colours.ShowDeployZone(true);
            _shownFightView = null;

            if (tied.Count >= 1)
            {
                var tiedHexes = new List<Hex>();
                foreach (ICombatant enemy in tied)
                {
                    if (enemy.CurrentPlacement is Hex hex) tiedHexes.Add(hex);
                }

                _board.Colours.ShowTargets(tiedHexes, expected?.CurrentPlacement as Hex);
                _board.Colours.ShowRoute(hero.RouteToExpectedTarget());
            }

            _isShown = true;
            _shownShape = default;
            _shownTargetView = view;
        }

        private void ShowFightTargetView(Hero hero)
        {
            Hex target = hero.TargetInFight?.CurrentPlacement as Hex;

            var tied = new List<Hex>();
            foreach (ICombatant enemy in hero.TiedNearestEnemies())
            {
                if (enemy.CurrentPlacement is Hex hex) tied.Add(hex);
            }
            if (target != null && !tied.Contains(target)) tied.Add(target);

            IReadOnlyList<Hex> route = hero.RouteTo(hero.TargetInFight);

            var view = new List<Hex>(tied) { target, null };
            view.AddRange(route);
            if (_isShown && _shownFightView != null && _shownFightView.SequenceEqual(view)) return;

            _board.Colours.ShowDeployZone(false);
            _board.Colours.ShowTargets(tied, target, onZone: false);
            _board.Colours.ShowRoute(route);

            _isShown = true;
            _shownShape = default;
            _shownTargetView = default;
            _shownFightView = view;
        }

        private void ShowMarks(Hero hero, Hex standingOn)
        {
            (Hero, Hex, int) shape = (hero, standingOn, hero.AlignPattern);
            if (_isShown && _shownShape == shape) return;

            var onBoard = new Dictionary<Hex, MarkColours>();
            var paying = new HashSet<Hex>();
            var offBoard = new Dictionary<Vector2Int, (Vector3 position, MarkColours marks)>();

            foreach (AlignShape.ShownHex shown in AlignShape.ShownHexes(hero, standingOn, _board.Hexs.Values))
            {
                if (shown.IsOffBoard)
                {
                    var spot = new Vector2Int(Mathf.RoundToInt(shown.position.x * 4f), Mathf.RoundToInt(shown.position.y * 4f));
                    offBoard.TryGetValue(spot, out var ghost);
                    offBoard[spot] = (shown.position, With(ghost.marks, shown, givesNothing: true));
                    continue;
                }

                onBoard.TryGetValue(shown.hex, out MarkColours marks);
                onBoard[shown.hex] = With(marks, shown, givesNothing: false);

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
            _shownFightView = null;
        }

        private static MarkColours With(MarkColours marks, AlignShape.ShownHex shown, bool givesNothing)
        {
            if (shown.colour == AligningHex.Orange) marks.first = true;
            if (shown.colour == AligningHex.Green) marks.second = true;
            if (shown.colour == AligningHex.Special) marks.special = true;
            if (shown.isWearing) marks.wearing = true;
            marks.givesNothing = givesNothing;
            return marks;
        }

        private Hero HoveredPlacedHero()
        {
            Hero hovered = Picker.At<Hero>(PlayerInputSystem.GetMouseWorldPosition(_cam));

            return hovered != null && hovered.Team == _team && hovered.CurrentHex != null ? hovered : null;
        }

        private Hero HoveredHeroInFight()
        {
            Hero hovered = Picker.At<Hero>(PlayerInputSystem.GetMouseWorldPosition(_cam));

            return hovered != null && hovered.IsAlive && hovered.CurrentHex != null ? hovered : null;
        }

        private void Show(HashSet<Hex> firstBonus, HashSet<Hex> secondBonus, HashSet<Hex> special, HashSet<Hex> wearing)
        {
            if (_isShown && _shownShape.hero == null && _shownTargetView.hero == null && firstBonus.SetEquals(_firstBonus) && secondBonus.SetEquals(_secondBonus)
                && special.SetEquals(_special) && wearing.SetEquals(_wearing)) return;

            _firstBonus = firstBonus;
            _secondBonus = secondBonus;
            _special = special;
            _wearing = wearing;
            _shownFightView = null;
            _isShown = true;
            _shownShape = default;
            _shownTargetView = default;
            _board.Colours.ShowAlignMarks(_firstBonus, _secondBonus, _special, _wearing);
        }

        private Hex PointedZoneHex()
        {
            Vector3 worldPos = PlayerInputSystem.GetMouseWorldPosition(_cam);
            Hex pointed = Picker.At<Hex>(worldPos);

            return pointed != null && pointed.IsInDeployZone ? pointed : null;
        }

        private static HashSet<Hex> InDeployZone(IEnumerable<Hex> hexes)
        {
            var inZone = new HashSet<Hex>();
            foreach (Hex hex in hexes)
            {
                if (hex.IsInDeployZone) inZone.Add(hex);
            }

            return inZone;
        }

        private void Hide(bool isPreparation)
        {
            if (!_isShown) return;

            _isShown = false;
            _shownShape = default;
            _shownFightView = null;

            _board.Colours.ShowDeployZone(isPreparation);
        }
    }
}
