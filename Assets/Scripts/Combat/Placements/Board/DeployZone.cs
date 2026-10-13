using System.Collections.Generic;
using System.Linq;

namespace MagicSchool.Combat.Placements
{
    /// <summary>
    /// The list of hexes the player can put a hero on during Preparation.
    /// Each stage can have different deploy zone.
    /// </summary>
    public class DeployZone
    {
        private readonly IReadOnlyDictionary<HexNumber, Hex> _hexs;
        private readonly BoardColours _colours;

        internal DeployZone(IReadOnlyDictionary<HexNumber, Hex> hexs, BoardColours colours)
        {
            _hexs = hexs;
            _colours = colours;
        }

        // Set each hexes to deploy zone
        public void Set(IReadOnlyList<HexNumber> zone)
        {
            var inZone = new HashSet<HexNumber>(zone);

            foreach (KeyValuePair<HexNumber, Hex> pair in _hexs)
            {
                pair.Value.SetInDeployZone(inZone.Contains(pair.Key));
            }
        }

        // Toggle on/off a specify hex into a deploy zone
        // e.g.     stage editor mode, toggle the hex by clicking hexes
        public void Toggle(Hex hex)
        {
            hex.SetInDeployZone(!hex.IsInDeployZone);

            _colours.ShowDeployZone(hex);
        }

        // Get deploy zone
        public List<HexNumber> GetDeployZone()
        {
            return _hexs
                .Where(pair => pair.Value.IsInDeployZone)
                .Select(pair => pair.Key)
                .OrderBy(number => number.column)
                .ThenBy(number => number.row)
                .ToList();
        }
    }
}
