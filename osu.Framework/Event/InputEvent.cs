// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using JetBrains.Annotations;
using osu.Framework.Input;

namespace osu.Framework.Event
{
    /// <summary>
    /// An event that is a result of user input.
    /// </summary>
    public abstract class InputEvent : Event
    {
        /// <summary>
        /// The current input state.
        /// This is the same instance as <see cref="InputManager.CurrentState"/> when this event is from an <see cref="InputManager"/>.
        /// </summary>
        [NotNull] public readonly InputState InputState;

        protected InputEvent([NotNull] InputState inputState)
        {
            InputState = inputState ?? throw new ArgumentNullException(nameof(inputState));
        }
    }
}
