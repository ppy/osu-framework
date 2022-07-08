// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using osu.Framework.Input.States;

namespace osu.Framework.Input.StateChanges
{
    /// <summary>
    /// Denotes an input from an <see cref="Handlers.InputHandler"/>.
    /// </summary>
    public interface IInput
    {
        /// <summary>
        /// Applies input to an <see cref="InputState"/>.
        /// This alters the <see cref="InputState"/> and propagates the change to an <see cref="IInputStateChangeHandler"/>.
        /// </summary>
        /// <param name="state">The <see cref="InputState"/> to apply changes to.</param>
        /// <param name="handler">The <see cref="IInputStateChangeHandler"/> to handle changes to <paramref name="state"/>.</param>
        void Apply(InputState state, IInputStateChangeHandler handler);
    }
}
