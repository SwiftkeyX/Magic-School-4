namespace MagicSchool.Contracts
{
    // IHeroCountPanel answers: show how many heroes has the player fielded?
    public interface IHeroCountPanel : IPanel
    {
        void ShowHeroCount(int placed, int limit);
    }
}