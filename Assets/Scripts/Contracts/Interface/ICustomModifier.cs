using System.Collections.Generic;

namespace MagicSchool.Contracts
{
    // It is a list of modifier build together into 1 custom modifier.
    // It is to answer: to make list of IModifier shared the same duration.
    // e.g. Werewolf's "WorldEnder" contain like 5 modifiers, but all of them have the same duration.
    public interface ICustomModifier
    {
        float GetDuration();
        IReadOnlyList<IModifier> GetModifiers();
        bool IsStack();
    }
}
