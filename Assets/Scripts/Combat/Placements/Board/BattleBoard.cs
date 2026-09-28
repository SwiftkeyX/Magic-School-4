using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using MagicSchool.Contracts;
using MagicSchool.Combat.Heroes;

namespace MagicSchool.Combat.Placements
{
    // FIXLATER: Could you tidy up the battleboard, there's too many public function, and some of it do similar thing.
    public class BattleBoard : MonoBehaviour
    {
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
        }

        // =================================== initialzie ===================================
        // BattleBoard find reference to each hex
        void InitializeHex()
        {
            var allHexes = new List<Hex>(GetComponentsInChildren<Hex>(true));

            foreach (var sideGroup in allHexes.GroupBy(h => h.transform.parent.name))
            {
                TeamEnum side = sideGroup.Key == "BlueSideHex" ? TeamEnum.Blue : TeamEnum.Red;

                var columns = sideGroup
                    .GroupBy(h => Mathf.RoundToInt(h.transform.localPosition.x * 10f))
                    .OrderBy(g => g.Key)
                    .ToList();

                int columnIndex = 0;
                foreach (var column in columns)
                {
                    var sortedRows = column.OrderByDescending(h => h.transform.localPosition.y).ToList();
                    int rowIndex = 0;
                    foreach (var hex in sortedRows)
                    {
                        HexNumber hexKey = new HexNumber(side, columnIndex, rowIndex);
                        _hexs[hexKey] = hex;
                        hex.Init(this, hexKey);
                        rowIndex++;
                    }
                    columnIndex++;
                }
            }
        }

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

        // Reset hero = reset its stat, sprite, statemachine
        // e.g. after player is losing, reset each hero from player team
        public void ResetTeam(TeamEnum team)
        {
            foreach (ICombatant combatant in _heroesOnBoard)
            {
                if (combatant == null || combatant.Team != team) continue;

                if (combatant is Hero hero) hero.ResetForNewStage();
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
