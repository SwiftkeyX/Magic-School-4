using System.Collections.Generic;

namespace MagicSchool.Contracts
{
    // IScoreboardPanel answer: put the round's numbers on screen.
    public interface IScoreboardPanel : IPanel
    {
        void ShowScores(IReadOnlyList<ScoreRow> rows);
    }
}
