using System.Collections.Generic;
using UnityEngine;
using MagicSchool.Contracts;

namespace MagicSchool.Combat.Placements
{
    /// <summary>
    /// Keep list of placement for each hero on the board
    /// It is here to quickly seed the board with hero
    /// </summary>
    [CreateAssetMenu(fileName = "BattleSetup", menuName = "Magic School 4/Battle Placement")]
    public class BattlePlacementSO : ScriptableObject
    {
        [SerializeField] private List<HeroPlacement> _heroesPlacement = new List<HeroPlacement>();

        public IReadOnlyList<HeroPlacement> HeroesPlacement => _heroesPlacement;

        // option to create BattlePlacementSO in code. Not as asset.
        // e.g.     use when testing hero benchmark
        public static BattlePlacementSO CreateRuntime(IEnumerable<HeroPlacement> placements)
        {
            BattlePlacementSO setup = CreateInstance<BattlePlacementSO>();
            setup._heroesPlacement = new List<HeroPlacement>(placements);
            return setup;
        }

        // swap one team's placements for new ones, the other team's stay as they are.
        // e.g.     the stage editor saves the enemy team it was shown on the board
        public void ReplaceTeam(TeamEnum team, IEnumerable<HeroPlacement> placements)
        {
            _heroesPlacement.RemoveAll(placement => placement.hexPlacement.team == team);
            _heroesPlacement.AddRange(placements);
        }
    }
}
