using System;
using UnityEngine;

namespace MagicSchool.Core
{
    /// <summary>
    /// Hides a field in the Inspector until a sibling field next to it is filled in.
    ///
    /// Not just tidiness - a field that shows while nothing reads it invites authoring a value that
    /// silently does nothing, e.g. an amplifier set to 0.5 on an effect with no condition, or a
    /// cadence interval on an effect that is not cadence.
    ///
    /// The sibling is named by string, so it must sit in the same class as the field being hidden.
    /// Use nameof() at the usage so a rename cannot quietly break the link.
    ///
    /// Recognised sibling types: bool (shown while true), and object or [SerializeReference]
    /// fields (shown while not null). Anything else always shows, on the grounds that a field
    /// vanishing for a reason nobody can see is worse than one showing needlessly.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class ShowIfAttribute : PropertyAttribute
    {
        public readonly string SiblingField;

        public ShowIfAttribute(string siblingField)
        {
            SiblingField = siblingField;
        }
    }
}
