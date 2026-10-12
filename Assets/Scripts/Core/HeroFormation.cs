using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using MagicSchool.Contracts;
using MagicSchool.Combat.Placements;

namespace MagicSchool.Core
{
    public class HeroFormation
    {
        private readonly HeroMover _heroMover;
        private readonly Bench _bench;
        private readonly BattleBoard _board;
        private readonly Dictionary<ICombatant, IPlacement> _formation = new Dictionary<ICombatant, IPlacement>();

        public HeroFormation(HeroMover heroMover, Bench bench, BattleBoard board)
        {
            _heroMover = heroMover;
            _bench = bench;
            _board = board;
        }

        // remember the current formation of the consuem team.
        public void Remember(IReadOnlyList<ICombatant> heroesOnBoard, TeamEnum team)
        {
            _formation.Clear();

            foreach (ICombatant hero in heroesOnBoard)
            {
                if (IsGone(hero) || hero.Team != team) continue;

                _formation[hero] = hero.CurrentPlacement;
            }
        }

        // After a fight every hero goes back to the bench: the next stage has its own deploy zone,
        // so the player places the team again instead of finding it where the last zone put it.
        public void ReturnToBench()
        {
            var noRoomOnBench = new List<KeyValuePair<ICombatant, IPlacement>>();

            foreach (KeyValuePair<ICombatant, IPlacement> stood in _formation)
            {
                if (IsGone(stood.Key)) continue;

                BenchSlot slot = _bench.FindFreeSlot();

                if (slot != null) _heroMover.MoveThisHeroTo(stood.Key, slot);
                else noRoomOnBench.Add(stood);
            }

            KeepOnBoard(noRoomOnBench);

            _formation.Clear();
        }

        // The bench is full, so these heroes have to stay on the board - but inside the new zone.
        // A hero whose starting hex is still in the zone goes back to it. They go first, so the
        // others cannot be handed a hex one of them is about to return to.
        private void KeepOnBoard(List<KeyValuePair<ICombatant, IPlacement>> heroes)
        {
            foreach (KeyValuePair<ICombatant, IPlacement> stood in heroes.Where(stood => IsInZone(stood.Value)))
            {
                _heroMover.MoveThisHeroTo(stood.Key, stood.Value);
            }

            foreach (KeyValuePair<ICombatant, IPlacement> stood in heroes.Where(stood => !IsInZone(stood.Value)))
            {
                // no free hex in the zone means the zone is smaller than the team - the starting hex is all that is left
                IPlacement placement = FindFreeHexInZone(stood.Key) ?? stood.Value;

                if (placement != null) _heroMover.MoveThisHeroTo(stood.Key, placement);
            }
        }

        private Hex FindFreeHexInZone(ICombatant hero)
        {
            return _board.Hexs
                .Where(pair => pair.Key.team == hero.Team && pair.Value.IsDeployable && !_board.IsReservedByOther(pair.Value, hero))
                .OrderBy(pair => pair.Key.column)
                .ThenBy(pair => pair.Key.row)
                .Select(pair => pair.Value)
                .FirstOrDefault();
        }

        private static bool IsInZone(IPlacement placement) => placement is Hex hex && hex.IsDeployable;

        private static bool IsGone(ICombatant hero)
        {
            return hero as Object == null;
        }
    }
}
