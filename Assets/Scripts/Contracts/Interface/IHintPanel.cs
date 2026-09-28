namespace MagicSchool.Contracts
{
    // IHintPanel answer: what should the player do next?
    // The line appear above of the screen. 
    public interface IHintPanel
    {
        void ShowPreparation(int stage, int stageCount);
        void ShowCombat(int stage, int stageCount);
        void ShowScoreboard(TeamEnum? winner, int stage, int stageCount, bool runCleared);
        void ShowResult(TeamEnum? winner, int stage, int stageCount, bool runCleared);
    }
}
