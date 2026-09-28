using MagicSchool.Combat.Placements;
using MagicSchool.Contracts;
using MagicSchool.Core;

namespace MagicSchool.Player
{
    // How many heroes the player has fielded, against how many they may - and the chip that says so.
    internal class PlayerTeamSize
    {
        // the panel is showing nothing at all
        private const int NothingShown = -1;

        private readonly IHeroCountPanel _heroCountPanel;
        private readonly BattleBoard _board;

        // what the chip is currently saying, so it is only written to when a number actually moves
        private int _shownHeroCount = NothingShown;
        private int _shownHeroLimit = NothingShown;

        // Both are read fresh every time rather than remembered: a hero sold, or swapped off the
        // board, changes the count without anybody telling this class about it.
        public int HeroCount => _board.CountTeamOnBoard(TeamEnum.Blue);
        public int HeroLimit => GameManager.Instance.HeroLimit;

        internal PlayerTeamSize(IHeroCountPanel heroCountPanel, BattleBoard board)
        {
            _heroCountPanel = heroCountPanel;
            _board = board;
        }

        // Driven every frame, because the things that change the count do not report it: a fight
        // starting, a hero sold to the shop, a hero swapped away. The guards below mean asking
        // every frame still only touches the panel when the answer has changed.
        public void RefreshHeroCountPanel()
        {
            if (_heroCountPanel == null) return;

            // the count belongs to setting up, so a fight takes it off screen
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

            // nothing moved since the last look
            if (count == _shownHeroCount && limit == _shownHeroLimit) return;

            _heroCountPanel.ShowHeroCount(count, limit);
            _shownHeroCount = count;
            _shownHeroLimit = limit;
        }

        // is there room on the board for one more?
        public bool IsAddingOK() => HeroCount < HeroLimit;
    }
}
