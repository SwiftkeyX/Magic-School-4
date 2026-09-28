using UnityEngine;

namespace MagicSchool.Contracts
{
    // IEquipment answers: what can a hero wear & what modifier does it give to a hero?
    public interface IEquipment
    {
        string DisplayName { get; }
        Transform transform { get; }
        ICustomModifier Modifier { get; }
    }
}
