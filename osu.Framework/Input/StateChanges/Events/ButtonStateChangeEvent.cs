// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using osu.Framework.Input.States;

namespace osu.Framework.Input.StateChanges.Events
{
    public class ButtonStateChangeEvent<TButton> : InputStateChangeEvent
        where TButton : struct
    {
        /// <summary>
        /// The button which changed state.
        /// </summary>
        public readonly TButton Button;

        /// <summary>
        /// The kind of button state change. Either pressed or released.
        /// </summary>
        public readonly ButtonStateChangeKind Kind;

        public ButtonStateChangeEvent(InputState state, IInput input, TButton button, ButtonStateChangeKind kind)
            : base(state, input)
        {
            Button = button;
            Kind = kind;
        }
    }
}
