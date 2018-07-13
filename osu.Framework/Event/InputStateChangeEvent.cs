// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using JetBrains.Annotations;
using osu.Framework.Input;

namespace osu.Framework.Event
{
    /// <summary>
    /// An event which represents a change of an <see cref="InputState"/>.
    /// An <see cref="IInput"/> produces this type of event after it changes <see cref="State"/>.
    /// </summary>
    public abstract class InputStateChangeEvent : Event
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
