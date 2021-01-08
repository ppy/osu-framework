// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

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
        /// </summary>
        object Action { get; set; }

        /// <summary>
        /// Get the action associated with this binding, cast to the required enum type.
        /// </summary>
        /// <typeparam name="T">The enum type.</typeparam>
        /// <returns>A cast <typeparamref name="T"/> representation of <see cref="KeyBinding.Action"/>.</returns>
        T GetAction<T>()
            where T : struct;
    }
}
