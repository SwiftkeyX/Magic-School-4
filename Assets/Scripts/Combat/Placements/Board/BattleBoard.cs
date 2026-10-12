using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using MagicSchool.Contracts;

namespace MagicSchool.Combat.Placements
{
    // FIXLATER: Could you tidy up the battleboard, there's too many public function, and some of it do similar thing.
    public class BattleBoard : MonoBehaviour
    {
        [Header("Hex colours")]
        [Tooltip("In Preparation: a hex in the player's deploy zone.")]
        [SerializeField] private Color _zoneColor = new Color(0.353f, 0.608f, 1f);
        [Tooltip("In Preparation: every hex outside the deploy zone.")]
        [SerializeField] private Color _outsideColor = new Color(1f, 0.353f, 0.353f);
        [Tooltip("Outside Preparation: every hex. The zone only matters while placing.")]
        [SerializeField] private Color _neutralColor = new Color(0.45f, 0.52f, 0.65f);

        // track every hex
        private Dictionary<HexNumber, Hex> _hexs = new Dictionary<HexNumber, Hex>();

        // track every hero on the battle board
        private List<ICombatant> _heroesOnBoard = new List<ICombatant>();
        private Dictionary<Hex, ICombatant> _reservedBy = new Dictionary<Hex, ICombatant>();

        // ======================== Setter & Getter ========================
        public IReadOnlyDictionary<HexNumber, Hex> Hexs => _hexs;
        public IReadOnlyList<ICombatant> HeroesOnBoard => _heroesOnBoard;

        public bool IsBattleOn { get; private set; }
        public void SetBattleOn(bool isOn) => IsBattleOn = isOn;

        void Awake()
        {
            InitializeHex();
            ShowDeployZone(false);
        }

        // =================================== initialzie ===================================
        // BattleBoard find reference to each hex
        void InitializeHex()
        {
            var allHexes = new List<Hex>(GetComponentsInChildren<Hex>(true));

            // one grid for the whole board: columns numbered left to right, rows top to bottom.
            // measured from the board itself, so a copy of the board somewhere else numbers the same
            var columns = allHexes
                .GroupBy(h => Mathf.RoundToInt(PositionOnBoard(h).x * 10f))
                .OrderBy(g => g.Key)
                .ToList();

            int columnIndex = 0;
            foreach (var column in columns)
            {
                var sortedRows = column.OrderByDescending(h => PositionOnBoard(h).y).ToList();
                int rowIndex = 0;
                foreach (var hex in sortedRows)
                {
                    HexNumber hexKey = new HexNumber(columnIndex, rowIndex);

                    _hexs[hexKey] = hex;
                    hex.Init(this, hexKey);
                    rowIndex++;
                }
                columnIndex++;
            }
        }

        private Vector3 PositionOnBoard(Hex hex) => transform.InverseTransformPoint(hex.transform.position);

        // =================================== hero tracking ===================================
        // when initialize, every hero need to be tracked on the board.
        // If they didn't get tracked, those heroes will be invisible to other hero.
        public void TrackThisHero(ICombatant hero)
        {
            if (!_heroesOnBoard.Contains(hero)) _heroesOnBoard.Add(hero);
        }

        // Counterpart to TrackThisHero - a hero leaving its hex for a non-hex placement
        // (e.g. back to the bench) is no longer on the battlefield.
        public void UntrackThisHero(ICombatant hero)
        {
            _heroesOnBoard.Remove(hero);
        }

        // =================================== Hex reservation ===================================
        // Called by Hero.SetReservedHex, which is the only place a hex reservation changes.
        public void UpdateReservation(ICombatant hero, Hex previous, Hex next)
        {
            // Only clear the old entry if this hero still owns it. Two heroes can't hold the same
            // hex, but a stale `previous` would otherwise evict whoever legitimately holds it now.
            if (previous != null && _reservedBy.TryGetValue(previous, out ICombatant owner) && owner == hero)
            {
                _reservedBy.Remove(previous);
            }

            if (next != null) _reservedBy[next] = hero;
        }

        // Who currently holds this hex, or null if it's free.
        public ICombatant WhoReservedThisHex(Hex hex)
        {
            if (hex == null || !_reservedBy.TryGetValue(hex, out ICombatant hero)) return null;

            return hero.IsAlive ? hero : null;
        }

        // "Is this hex taken by someone other than me?"
        public bool IsReservedByOther(Hex hex, ICombatant asker)
        {
            ICombatant reserver = WhoReservedThisHex(hex);
            return reserver != null && reserver != asker;
        }

        // =================================== Deploy zone ===================================
        /// <summary>
        /// The list of hexes the player can put a hero on during Preparation. 
        /// Each stage have different deploy zone.
        /// </summary>

        // Set the deploy zone to the board
        public void SetDeployZone(IReadOnlyList<HexNumber> zone)
        {
            var inZone = new HashSet<HexNumber>(zone);

            foreach (KeyValuePair<HexNumber, Hex> pair in _hexs)
            {
                pair.Value.SetInDeployZone(inZone.Contains(pair.Key));
            }
        }

        // Show the deploy zone by setting the hex's color to blue
        public void ShowDeployZone(bool isShown)
        {
            foreach (Hex hex in _hexs.Values)
            {
                hex.SetColor(isShown ? ZoneColorOf(hex) : _neutralColor);
            }
        }

        private Color ZoneColorOf(Hex hex) => hex.IsInDeployZone ? _zoneColor : _outsideColor;

        // Toggle on/off a specify hex into a deploy zone 
        // e.g.     stage editor mode, toggle the hex by clicking hexes
        public void ToggleInDeployZone(Hex hex)
        {
            hex.SetInDeployZone(!hex.IsInDeployZone);

            hex.SetColor(ZoneColorOf(hex));
        }

        // Get deploy zone (the list of deploy hex)
        public List<HexNumber> ReadDeployZone()
        {
            return _hexs
                .Where(pair => pair.Value.IsInDeployZone)
                .Select(pair => pair.Key)
                .OrderBy(number => number.column)
                .ThenBy(number => number.row)
                .ToList();
        }

        // =================================== Between stages ===================================
        // Wipe one team off the board
        // e.g. after player is winning, clear enemy team 
        public void ClearTeam(TeamEnum team)
        {
            List<ICombatant> leaving = _heroesOnBoard.Where(h => h != null && h.Team == team).ToList();

            foreach (ICombatant hero in leaving)
            {
                ReleaseReservationsOf(hero);    // FLAGGING: this is O(n), it don't have to.
                _heroesOnBoard.Remove(hero);

                if (hero.transform != null) Destroy(hero.transform.gameObject);
            }
        }

        private void ReleaseReservationsOf(ICombatant hero)
        {
            List<Hex> held = _reservedBy.Where(pair => pair.Value == hero).Select(pair => pair.Key).ToList();

            foreach (Hex hex in held) _reservedBy.Remove(hex);
        }

        // How many of consuming team are standing on the board right now.
        public int CountTeamOnBoard(TeamEnum team)
        {
            int count = 0;
            foreach (ICombatant hero in _heroesOnBoard)
            {
                if (hero.Team == team) count++;
            }

            return count;
        }
    }
}
