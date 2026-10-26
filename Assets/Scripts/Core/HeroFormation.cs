using System.Collections.Generic;
using UnityEngine;
using MagicSchool.Contracts;
using MagicSchool.Engine;
using MagicSchool.Combat.Placements;

namespace MagicSchool.Core
{
    // When the spare bench is full, a hero goes to the bench, then back to the hex it started on.
    public class HeroFormation
    {
        private readonly HeroMover _heroMover;
        private readonly Bench _spareBench;
        private readonly Bench _bench;
        private readonly Dictionary<ICombatant, IPlacement> _formation = new Dictionary<ICombatant, IPlacement>();

        public HeroFormation(HeroMover heroMover, Bench spareBench, Bench bench)
        {
            _heroMover = heroMover;
            _spareBench = spareBench;
            _bench = bench;
        }

        public void Remember(IReadOnlyList<ICombatant> heroesOnBoard, TeamEnum team)
        {
            _formation.Clear();

            foreach (ICombatant hero in heroesOnBoard)
            {
                if (IsGone(hero) || hero.Team != team) continue;

                _formation[hero] = hero.CurrentPlacement;
            }
        }

        public void ReturnToStartingPlacement()
        {
            foreach (KeyValuePair<ICombatant, IPlacement> stood in _formation)
            {
                if (IsGone(stood.Key)) continue;

                _heroMover.MoveThisHeroTo(stood.Key, stood.Value);
            }

            _formation.Clear();
        }

        public void ReturnToSpareBench()
        {
            foreach (KeyValuePair<ICombatant, IPlacement> stood in _formation)
            {
                if (IsGone(stood.Key)) continue;

                IPlacement placement = FindPlaceFor(stood.Value);
                if (placement != null) _heroMover.MoveThisHeroTo(stood.Key, placement);
            }

            _formation.Clear();
        }

        private IPlacement FindPlaceFor(IPlacement startedOn)
        {
            BenchSlot spareSlot = _spareBench != null ? _spareBench.FindFreeSlot() : null;
            if (spareSlot != null) return spareSlot;

            BenchSlot benchSlot = _bench != null ? _bench.FindFreeSlot() : null;
            if (benchSlot != null) return benchSlot;

            DebugTool.LogWarning("No free slot on the spare bench or the bench - the hero goes back to the hex it started on.");
            return startedOn;
        }

        private static bool IsGone(ICombatant hero)
        {
            return hero as Object == null;
        }
    }
}
