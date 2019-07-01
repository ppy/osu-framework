// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Input.States;

namespace osu.Framework.Input.Events
{
    public class KeyBindingReleaseEvent<T> : KeyBindingEvent<T> where T : struct
    {
        public KeyBindingReleaseEvent(InputState state, T action)
            : base(state, action)
        {
        }
    }
}
