namespace MagicSchool.Contracts
{
    public interface IInspectorPanel
    {
        void Inspect(IInspectable target);

        // what the panel shows now; null when nothing
        IInspectable Shown { get; }
    }
}
