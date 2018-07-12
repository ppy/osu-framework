// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using JetBrains.Annotations;
using osu.Framework.Input;

namespace osu.Framework.Event
{
    /// <summary>
    /// Event from <see cref="IInput"/>.
    /// </summary>
    public class InputStateChangeEvent : InputEvent
    {
        /// <summary>
        /// An <see cref="IInput"/> that caused this input state change.
        /// </summary>
        [NotNull] public readonly IInput Input;

        public InputStateChangeEvent(InputState state, IInput input)
            : base(state)
        {
            Input = input;
        }
    }
}
