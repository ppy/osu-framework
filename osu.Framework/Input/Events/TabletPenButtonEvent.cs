// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using osu.Framework.Extensions.TypeExtensions;
using osu.Framework.Input.States;

namespace osu.Framework.Input.Events
{
    /// <summary>
    /// Events of a tablet pen button.
    /// </summary>
    public abstract class TabletPenButtonEvent : TabletEvent
    {
        public readonly TabletPenButton Button;

        protected TabletPenButtonEvent(InputState state, TabletPenButton button)
            : base(state)
        {
            Button = button;
        }

        public override string ToString() => $"{GetType().ReadableName()}({Button})";
    }
}
