namespace MagicSchool.Contracts
{
    public interface IBannerPanel : IPanel
    {
        void ShowResult(TeamEnum? winner, int stage, int stageCount, bool runCleared);
    }
}
