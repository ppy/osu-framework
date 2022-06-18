// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using osu.Framework.Input.States;

namespace osu.Framework.Input.Events
{
    /// <summary>
    /// Events of a key binding action.
    /// </summary>
    /// <typeparam name="T">The action type.</typeparam>
    public abstract class KeyBindingEvent<T> : UIEvent where T : struct
    {
        public readonly T Action;

        protected KeyBindingEvent(InputState state, T action)
            : base(state)
        {
            Action = action;
        }
    }
}
