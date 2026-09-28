namespace MagicSchool.Contracts
{
    public interface IInspectableItem : IInspectable
    {
        string Description { get; }
    }
}
