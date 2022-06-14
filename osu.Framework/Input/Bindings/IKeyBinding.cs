// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

namespace osu.Framework.Input.Bindings
{
    /// <summary>
    /// A binding of a <see cref="Bindings.KeyCombination"/> to an action.
    /// </summary>
    public interface IKeyBinding
    {
        /// <summary>
        /// The combination of keys which will trigger this binding.
        /// </summary>
        KeyCombination KeyCombination { get; set; }

        /// <summary>
        /// The resultant action which is triggered by this binding.
        /// Generally an enum type, but may also be an int representing an enum (converted via <see cref="KeyBindingExtensions.GetAction{T}"/>
        /// </summary>
        object Action { get; set; }
    }
}
