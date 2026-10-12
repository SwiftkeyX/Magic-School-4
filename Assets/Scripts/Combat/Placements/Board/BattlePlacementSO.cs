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

        [Tooltip("Hexes the player may put a hero on this stage. Every stage must list them: empty = nowhere.")]
        [SerializeField] private List<HexNumber> _deployZone = new List<HexNumber>();

        public IReadOnlyList<HeroPlacement> HeroesPlacement => _heroesPlacement;
        public IReadOnlyList<HexNumber> DeployZone => _deployZone;

        // option to create BattlePlacementSO in code. Not as asset.
        // e.g.     use when testing hero benchmark
        public static BattlePlacementSO CreateRuntime(IEnumerable<HeroPlacement> placements)
        {
            BattlePlacementSO setup = CreateInstance<BattlePlacementSO>();
            setup._heroesPlacement = new List<HeroPlacement>(placements);
            return setup;
        }

        // Changing team formation by editing this SO. 
        // e.g.     stage editor mode, want to override new enemy formation to current SO. 
        public void ReplaceTeam(TeamEnum team, IEnumerable<HeroPlacement> placements)
        {
            // removing all hero that belong the specify team first
            _heroesPlacement.RemoveAll(placement =>
            {
                bool sameTeam = placement.hexPlacement.team == team;
                return sameTeam;
            });

            // re-add hero that belong to specify team
            _heroesPlacement.AddRange(placements);
        }
    }
}
