using System.Collections.Generic;
using MagicSchool.Contracts;
using MagicSchool.Combat.Align;
using MagicSchool.Combat.Placements;

namespace MagicSchool.Combat.Heroes
{
    // Hero: where it stands - the board it is on, its hex or bench slot, its reservation, and its align marks.
    public partial class Hero
    {
        // ======================================== board ========================================
        public ICombatant WhoReservedThisHex(Hex hex) => _board.WhoReservedThisHex(hex);
        public bool IsHexReservedByOther(Hex hex) => _board.IsReservedByOther(hex, this);
        public bool IsBattleOn => _board == null || _board.IsBattleOn;

        public IReadOnlyList<ICombatant> HeroesOnBoard => _board != null ? _board.HeroesOnBoard : new List<ICombatant>();
        public void TrackOnBoard() { if (_board != null) _board.TrackThisHero(this); }
        public void UntrackFromBoard() { if (_board != null) _board.UntrackThisHero(this); }

        // ======================================== align mark ========================================
        public IReadOnlyList<AlignMark> AlignMarks => _SOData != null ? _SOData.AlignMarksOf(_alignPattern) : new List<AlignMark>();
        public int AlignPattern => _alignPattern;
        public int AlignPatternCount => _SOData != null ? _SOData.AlignPatternCount : 1;

        public bool RotateAlignMark(int step = 1)
        {
            int count = AlignPatternCount;
            if (count <= 1) return false;

            _alignPattern = ((_alignPattern + step) % count + count) % count;
            return true;
        }

        public void SetAlignMarkPattern(int pattern) => _alignPattern = pattern >= 0 && pattern < AlignPatternCount ? pattern : 0;

        // IAligned: allies on each special mark's hexes, as found by the last AlignBonuses.Refresh
        public IReadOnlyList<ICombatant> AlliesOnSpecialMark(string mark)
            => _alliesOnSpecialMark.TryGetValue(mark, out List<ICombatant> allies) ? allies : (IReadOnlyList<ICombatant>)new List<ICombatant>();
        internal void SetAlliesOnSpecialMarks(Dictionary<string, List<ICombatant>> allies) => _alliesOnSpecialMark = allies;

        // IHexMeasurable: hexes counted step by step across the board (See Hex.IsWithinRange)
        public bool IsAtLeastHexesAway(IPlaceable other, int hexes)
            => CurrentHex != null && other?.CurrentPlacement is Hex otherHex && !CurrentHex.IsWithinRange(otherHex, hexes - 1);

        // ======================================== IPlaceable ========================================
        public Hex CurrentHex => _currentPlacement as Hex;
        public Hex ReservedHex => _reservedHex;
        public IPlacement CurrentPlacement => _currentPlacement;
        public bool IsInCombat => _currentPlacement is Hex;
        public void SetReservedHex(Hex hex)
        {
            if (_board != null) _board.UpdateReservation(this, _reservedHex, hex);

            _reservedHex = hex;
        }
        public void SetCurrentPlacement(IPlacement placement) => _currentPlacement = placement;
    }
}
