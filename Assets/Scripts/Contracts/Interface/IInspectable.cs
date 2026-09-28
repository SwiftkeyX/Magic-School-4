namespace MagicSchool.Contracts
{
    // IInspectable answers: what does a thing show in inspector when the player right-clicks show?
    // e.g. a hero, a item
    public interface IInspectable
    {
        string DisplayName { get; }
        bool IsAlive { get; }
    }
}
