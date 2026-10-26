using System;
using System.Collections.Generic;
using MagicSchool.Contracts;

namespace MagicSchool.Modifiers
{
    // A group of modifiers sharing one timer. Applied again to a hero who has it: isStack adds another with its own
    // timer, otherwise it replaces the old one and the timer restarts.
    // keepsHealth: when it expires max HP goes back but current HP stays (capped), so Husk keeps the HP he stole. The
    // duration is a Func so it can change after construction.
    public class CustomModifier : ICustomModifier
    {
        private readonly IReadOnlyList<IModifier> _modifiers;
        private readonly Func<float> _duration;
        public float GetDuration() => _duration();
        public IReadOnlyList<IModifier> GetModifiers() => _modifiers;

        private readonly bool _isStack;
        public bool IsStack() => _isStack;

        public bool KeepsHealth { get; }

        public CustomModifier(float duration, IReadOnlyList<IModifier> modifiers, bool isStack, bool keepsHealth = false)
            : this(() => duration, modifiers, isStack, keepsHealth) { }

        public CustomModifier(Func<float> duration, IReadOnlyList<IModifier> modifiers, bool isStack, bool keepsHealth = false)
        {
            KeepsHealth = keepsHealth;
            _isStack = isStack;
            _duration = duration;
            _modifiers = modifiers ?? new List<IModifier>();
        }

    }
}
