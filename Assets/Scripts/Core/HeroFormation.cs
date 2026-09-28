using System.Collections.Generic;
using UnityEngine;
using MagicSchool.Contracts;

namespace MagicSchool.Core
{
    public class HeroFormation
    {
        private readonly HeroMover _heroMover;
        private readonly Dictionary<ICombatant, IPlacement> _formation = new Dictionary<ICombatant, IPlacement>();

        public HeroFormation(HeroMover heroMover)
        {
            _heroMover = heroMover;
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

        // reset every hero to their original formation
        public void Restore()
        {
            foreach (KeyValuePair<ICombatant, IPlacement> stood in _formation)
            {
                if (IsGone(stood.Key) || stood.Value == null) continue;

                _heroMover.MoveThisHeroTo(stood.Key, stood.Value);
            }

            _formation.Clear();
        }

        private static bool IsGone(ICombatant hero)
        {
            return hero as Object == null;
        }
    }
}
