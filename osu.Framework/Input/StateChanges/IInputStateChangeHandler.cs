// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using osu.Framework.Input.StateChanges.Events;
using osu.Framework.Input.States;

namespace osu.Framework.Input.StateChanges
{
    /// <summary>
    /// An object which can handle <see cref="InputState"/> changes.
    /// </summary>
    public interface IInputStateChangeHandler
    {
        /// <summary>
        /// Handles an input state change event.
        /// </summary>
        void HandleInputStateChange(InputStateChangeEvent inputStateChange);
    }
}
