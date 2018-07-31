// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE


using osu.Framework.Input.States;

namespace osu.Framework.Input.Events
{
    /// <summary>
    /// An event representing a mouse hover.
    /// Triggered when mouse cursor is moved onto a drawable.
    /// </summary>
    public class Hovered : HoverEvent
    {
        public Hovered(InputState state)
            : base(state)
        {
        }
    }
}
