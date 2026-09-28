namespace MagicSchool.Contracts
{
    // IRewardPanel answer: show the reward panel, that player can choose from
    public interface IRewardPanel : IPanel
    {
        void ShowReward();              // a stage was won: roll an offer and put it on screen
        bool IsChoosing { get; }        // is the player still choosing the reward?
    }
}
