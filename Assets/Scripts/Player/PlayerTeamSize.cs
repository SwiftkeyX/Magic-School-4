using MagicSchool.Combat.Placements;
using MagicSchool.Contracts;
using MagicSchool.Core;

namespace MagicSchool.Player
{
    // How many heroes the player has fielded, against how many they may - and the chip that says so.
    // Polled every frame because what changes the count does not report it (a fight starting, a hero sold or swapped
    // away); it only writes to the panel when the number moves.
    internal class PlayerTeamSize
    {
        private const int NothingShown = -1;

        private readonly IHeroCountPanel _heroCountPanel;
        private readonly BattleBoard _board;

        private int _shownHeroCount = NothingShown;
        private int _shownHeroLimit = NothingShown;

        public int HeroCount => _board.CountTeamOnBoard(TeamEnum.Blue);
        public int HeroLimit => GameManager.Instance.HeroLimit;

        internal PlayerTeamSize(IHeroCountPanel heroCountPanel, BattleBoard board)
        {
            _heroCountPanel = heroCountPanel;
            _board = board;
        }

        public void RefreshHeroCountPanel()
        {
            if (_heroCountPanel == null) return;

            if (GameManager.Instance.Phase != GamePhaseEnum.Preparation)
            {
                if (_shownHeroCount == NothingShown) return;

                _heroCountPanel.SetShown(false);
                _shownHeroCount = NothingShown;
                _shownHeroLimit = NothingShown;
                return;
            }

            int count = HeroCount;
            int limit = HeroLimit;

            if (count == _shownHeroCount && limit == _shownHeroLimit) return;

            _heroCountPanel.ShowHeroCount(count, limit);
            _shownHeroCount = count;
            _shownHeroLimit = limit;
        }

        public bool IsAddingOK() => HeroCount < HeroLimit;
    }
}
