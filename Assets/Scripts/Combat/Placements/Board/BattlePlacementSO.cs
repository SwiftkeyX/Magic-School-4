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

        public static BattlePlacementSO CreateRuntime(IEnumerable<HeroPlacement> placements)
        {
            BattlePlacementSO setup = CreateInstance<BattlePlacementSO>();
            setup._heroesPlacement = new List<HeroPlacement>(placements);
            return setup;
        }

        public void ReplaceTeam(TeamEnum team, IEnumerable<HeroPlacement> placements)
        {
            _heroesPlacement.RemoveAll(placement =>
            {
                bool sameTeam = placement.team == team;
                return sameTeam;
            });

            _heroesPlacement.AddRange(placements);
        }

        public void SetBoardLayout(GameObject layoutPrefab) => _boardLayout = layoutPrefab;

        public void ReplaceItemsOnHexes(IEnumerable<ItemPlacement> items)
        {
            _itemsOnHexes = new List<ItemPlacement>(items);
        }

        public void ReplaceDeployZone(IEnumerable<HexNumber> zone)
        {
            _deployZone = new List<HexNumber>(zone);
        }
    }
}
