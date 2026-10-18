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

        [Tooltip("The board this stage is fought on.")]
        [SerializeField] private GameObject _boardLayout;

        [Tooltip("Items lying on hexes, for the player's wearing hexes. A saved team comp fills this; a stage leaves it empty.")]
        [SerializeField] private List<ItemPlacement> _itemsOnHexes = new List<ItemPlacement>();

        public IReadOnlyList<HeroPlacement> HeroesPlacement => _heroesPlacement;
        public IReadOnlyList<ItemPlacement> ItemsOnHexes => _itemsOnHexes;
        public IReadOnlyList<HexNumber> DeployZone => _deployZone;
        public GameObject BoardLayout => _boardLayout;

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
                bool sameTeam = placement.team == team;
                return sameTeam;
            });

            // re-add hero that belong to specify team
            _heroesPlacement.AddRange(placements);
        }

        // Changing the board by editing this SO.
        // e.g.     stage editor mode, a saved team comp remembers the board its hex numbers belong to.
        public void SetBoardLayout(GameObject layoutPrefab) => _boardLayout = layoutPrefab;

        // Changing the items lying on hexes by editing this SO.
        // e.g.     stage editor mode, a saved team comp keeps the items placed for its wearing hexes.
        public void ReplaceItemsOnHexes(IEnumerable<ItemPlacement> items)
        {
            _itemsOnHexes = new List<ItemPlacement>(items);
        }

        // Changing the deploy zone by editing this SO.
        // e.g.     stage editor mode, want to override the painted deploy zone to current SO.
        public void ReplaceDeployZone(IEnumerable<HexNumber> zone)
        {
            _deployZone = new List<HexNumber>(zone);
        }
    }
}
