namespace MagicSchool.Contracts
{
    // IRewardPanel answer: show the reward panel, that player can choose from
    public interface IRewardPanel : IPanel
    {
        void ShowReward();
        bool IsChoosing { get; }
    }
}
