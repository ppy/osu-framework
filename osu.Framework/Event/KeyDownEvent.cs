// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Extensions.TypeExtensions;
using osu.Framework.Input;
using OpenTK.Input;

namespace osu.Framework.Event
{
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
