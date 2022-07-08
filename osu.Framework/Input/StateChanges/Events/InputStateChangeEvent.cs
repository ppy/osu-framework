// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using JetBrains.Annotations;
using osu.Framework.Input.States;

namespace osu.Framework.Input.StateChanges.Events
{
    /// <summary>
    /// An event which represents a change of an <see cref="InputState"/>.
    /// An <see cref="IInput"/> produces this type of event after it changes <see cref="State"/>.
    /// </summary>
    public abstract class InputStateChangeEvent
    {
        /// <summary>
        /// The <see cref="InputState"/> changed by this event.
        /// </summary>
        [NotNull]
        public readonly InputState State;

        /// <summary>
        /// The <see cref="IInput"/> that caused this input state change.
        /// </summary>
        [CanBeNull]
        public readonly IInput Input;

        protected InputStateChangeEvent(InputState state, IInput input)
        {
            State = state ?? throw new ArgumentNullException(nameof(state));
            Input = input;
        }
    }
}
