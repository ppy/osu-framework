// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Input.StateChanges;
using osu.Framework.Input.States;

namespace osu.Framework.Event
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
