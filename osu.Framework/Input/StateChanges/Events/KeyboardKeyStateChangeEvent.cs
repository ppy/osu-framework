// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using osu.Framework.Input.States;
using osuTK.Input;

namespace osu.Framework.Input.StateChanges.Events
{
    public class KeyboardKeyStateChangeEvent : ButtonStateChangeEvent<Key>
    {
        /// <summary>
        /// Whether this event was sent in the past and is now being repeated.
        /// </summary>
        public readonly bool Repeat;

        public KeyboardKeyStateChangeEvent(InputState state, IInput input, Key key, ButtonStateChangeKind kind, bool repeat)
            : base(state, input, key, kind)
        {
            Repeat = repeat;
        }
    }
}
