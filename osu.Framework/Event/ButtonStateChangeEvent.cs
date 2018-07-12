// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Input;

namespace osu.Framework.Event
{
    public class ButtonStateChangeEvent<TButton> : InputStateChangeEvent
    where TButton : struct
    {
        /// <summary>
        /// The button which state changed.
        /// </summary>
        public TButton Button;

        /// <summary>
        /// The kind of button state change. either pressed or released.
        /// </summary>
        public ButtonStateChangeKind Kind;

        public ButtonStateChangeEvent(InputState state, IInput input, TButton button, ButtonStateChangeKind kind)
            : base(state, input)
        {
            Button = button;
            Kind = kind;
        }
    }
}
