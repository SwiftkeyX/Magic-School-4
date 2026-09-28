using System.Collections.Generic;
using MagicSchool.Contracts;

namespace MagicSchool.Modifiers
{
    public class CustomModifier : ICustomModifier
    {
        private readonly IReadOnlyList<IModifier> _modifiers;
        private readonly float _duration;
        public float GetDuration() => _duration;
        public IReadOnlyList<IModifier> GetModifiers() => _modifiers;

        public CustomModifier(float duration, IReadOnlyList<IModifier> modifiers)
        {
            _duration = duration;
            _modifiers = modifiers ?? new List<IModifier>();
        }

    }
}
