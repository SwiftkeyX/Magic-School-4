using System.Collections.Generic;
using UnityEngine;

namespace MagicSchool.Combat.Placements
{
    /// <summary>
    /// Keep list of placement for each hero on the board
    /// It is here to quickly seed the board with hero
    /// </summary>
    [CreateAssetMenu(fileName = "BattleSetup", menuName = "Magic School 3/Battle Placement")]
    public class BattlePlacementSO : ScriptableObject
    {
        [SerializeField] private List<HeroPlacement> _heroesPlacement = new List<HeroPlacement>();

        public IReadOnlyList<HeroPlacement> HeroesPlacement => _heroesPlacement;
    }
}
