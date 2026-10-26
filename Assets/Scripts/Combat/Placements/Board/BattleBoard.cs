using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using MagicSchool.Contracts;

namespace MagicSchool.Combat.Placements
{
    // FIXLATER: Could you tidy up the battleboard, there's too many public function, and some of it do similar thing.
    // Hex numbers are measured from the board itself, so a copy of the board elsewhere (a benchmark lane) numbers the
    // same. A hero must be tracked to be seen by other heroes; leaving for a non-hex placement (the bench) untracks
    // it.
    // Reservations change only through Hero.SetReservedHex. The old entry is cleared only if this hero still owns it:
    // a stale `previous` would otherwise evict whoever holds it now. Whoever changes the layout takes the items off
    // first (See PreparationState); SetLayout only forgets the hexes.
    public class BattleBoard : MonoBehaviour
    {
        [Header("Hex colours")]
        [SerializeField] private BoardColours _colours = new BoardColours();

        private readonly Dictionary<HexNumber, Hex> _hexs = new Dictionary<HexNumber, Hex>();

        private List<ICombatant> _heroesOnBoard = new List<ICombatant>();
        private Dictionary<Hex, ICombatant> _reservedBy = new Dictionary<Hex, ICombatant>();

        private DeployZone _deployZone;
        private readonly HexItems _items = new HexItems();

        public IReadOnlyDictionary<HexNumber, Hex> Hexs => _hexs;
        public IReadOnlyList<ICombatant> HeroesOnBoard => _heroesOnBoard;

        public DeployZone DeployZone => _deployZone;
        public HexItems Items => _items;
        public BoardColours Colours => _colours;

        public bool IsBattleOn { get; private set; }
        public void SetBattleOn(bool isOn) => IsBattleOn = isOn;

        void Awake()
        {
            _colours.Init(_hexs, transform);
            _deployZone = new DeployZone(_hexs, _colours);

            InitializeHex();
            _colours.ShowDeployZone(false);
        }

        internal void InitializeHex()
        {
            var allHexes = new List<Hex>(GetComponentsInChildren<Hex>(true));

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

        private GameObject _layoutPrefab;

        public GameObject Layout => _layoutPrefab;

        public bool HasLayout(GameObject layoutPrefab) => layoutPrefab != null && layoutPrefab == _layoutPrefab;

        public void SetLayout(GameObject layoutPrefab)
        {
            if (layoutPrefab == null || layoutPrefab == _layoutPrefab) return;

            RemoveHexes();

            Instantiate(layoutPrefab, transform);
            _layoutPrefab = layoutPrefab;
            InitializeHex();
            _colours.ShowDeployZone(false);
        }

        private void RemoveHexes()
        {
            _hexs.Clear();
            _reservedBy.Clear();

            _items.TakeAll();

            foreach (Transform child in transform.Cast<Transform>().ToList())
            {
                bool holdsHexes = child.GetComponentInChildren<Hex>(true) != null;
                if (!holdsHexes) continue;

                child.SetParent(null);
                Destroy(child.gameObject);
            }
        }

        public void TrackThisHero(ICombatant hero)
        {
            if (!_heroesOnBoard.Contains(hero)) _heroesOnBoard.Add(hero);
        }

        public void UntrackThisHero(ICombatant hero)
        {
            _heroesOnBoard.Remove(hero);
        }

        public void UpdateReservation(ICombatant hero, Hex previous, Hex next)
        {
            if (previous != null && _reservedBy.TryGetValue(previous, out ICombatant owner) && owner == hero)
            {
                _reservedBy.Remove(previous);
            }

            if (next != null) _reservedBy[next] = hero;
        }

        public ICombatant WhoReservedThisHex(Hex hex)
        {
            if (hex == null || !_reservedBy.TryGetValue(hex, out ICombatant hero)) return null;

            return hero.IsAlive ? hero : null;
        }

        public bool IsReservedByOther(Hex hex, ICombatant asker)
        {
            ICombatant reserver = WhoReservedThisHex(hex);
            return reserver != null && reserver != asker;
        }

        public void ClearTeam(TeamEnum team)
        {
            List<ICombatant> leaving = _heroesOnBoard.Where(h => h != null && h.Team == team).ToList();

            foreach (ICombatant hero in leaving) Remove(hero);
        }

        public void Remove(ICombatant hero)
        {
            ReleaseReservationsOf(hero);
            _heroesOnBoard.Remove(hero);

            if (hero.transform != null) Destroy(hero.transform.gameObject);
        }

        private void ReleaseReservationsOf(ICombatant hero)
        {
            List<Hex> held = _reservedBy.Where(pair => pair.Value == hero).Select(pair => pair.Key).ToList();

            foreach (Hex hex in held) _reservedBy.Remove(hex);
        }

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
