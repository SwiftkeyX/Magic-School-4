using MagicSchool.Combat.Placements;

namespace MagicSchool.Core
{
    // What Player asks of the Stage Editor. The editor itself is in MagicSchool.DevTools, which release builds leave out
    // and which registers it at start (GameManager.RegisterStageEditor): Core and Player never reference DevTools, so a
    // build without it simply has no stage editing.
    public interface IStageEditing
    {
        bool CanEditNow { get; }
        bool TryToggleDeployHex(Hex hex);
    }
}
