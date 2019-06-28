// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Extensions.TypeExtensions;
using osu.Framework.Input.States;
using osuTK.Input;

namespace osu.Framework.Input.Events
{
    /// <summary>
    /// An event representing a press of a keyboard key.
    /// </summary>
    public class KeyDownEvent : KeyboardEvent
    {
        public readonly bool Repeat;

        public KeyDownEvent(InputState state, Key key, bool repeat = false)
            : base(state, key)
        {
            Repeat = repeat;
        }

        public override string ToString() => $"{GetType().ReadableName()}({Key}, {Repeat})";
    }
}
