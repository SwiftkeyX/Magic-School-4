using System;
using System.Collections.Generic;
using MagicSchool.Contracts;

namespace MagicSchool.Modifiers
{
    public class CustomModifier : ICustomModifier
    {
        private readonly IReadOnlyList<IModifier> _modifiers;
        private readonly Func<float> _duration;
        public float GetDuration() => _duration();
        public IReadOnlyList<IModifier> GetModifiers() => _modifiers;

        // What happens when this same modifier is applied to a hero who already has it:
        //      stack   = modifier is stack, each has its own timer.
        //      refresh = no stack, the modifier is replaced, so the timer restarts.
        private readonly bool _isStack;
        public bool IsStack() => _isStack;

        public CustomModifier(float duration, IReadOnlyList<IModifier> modifiers, bool isStack)
            : this(() => duration, modifiers, isStack) { }

        // duration is Func<Float> because it allow the duration to be adjust after initialize in constructor.
        public CustomModifier(Func<float> duration, IReadOnlyList<IModifier> modifiers, bool isStack)
        {
            _isStack = isStack;
            _duration = duration;
            _modifiers = modifiers ?? new List<IModifier>();
        }

    }
}
