// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Extensions.TypeExtensions;
using osu.Framework.Input.States;
using OpenTK.Input;

namespace osu.Framework.Input.Events
{
    /// <summary>
    /// Events of a keyboard key.
    /// </summary>
    public abstract class KeyboardKeyEvent : UIEvent
    {
        public readonly Key Key;

        protected KeyboardKeyEvent(InputState state, Key key)
            : base(state)
        {
            Key = key;
        }

        public override string ToString() => $"{GetType().ReadableName()}({Key})";
    }
}
