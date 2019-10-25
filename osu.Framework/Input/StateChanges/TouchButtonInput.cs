// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using osu.Framework.Input.States;
using osuTK.Input;

namespace osu.Framework.Input.StateChanges
{
    public class TouchButtonInput : ButtonInput<PositionalPointer>
    {
        public TouchButtonInput(IEnumerable<ButtonInputEntry<PositionalPointer>> entries)
            : base(entries)
        {
        }

        public TouchButtonInput(PositionalPointer button, bool isPressed)
            : base(button, isPressed)
        {
        }

        public TouchButtonInput(ButtonStates<PositionalPointer> current, ButtonStates<PositionalPointer> previous)
            : base(current, previous)
        {
        }

        protected override ButtonStates<PositionalPointer> GetButtonStates(InputState state) => state.Touch.Pointers;

        public override void Apply(InputState state, IInputStateChangeHandler handler)
        {
            var touch = state.Touch;
            bool hasPrimaryBefore = touch.TryGetPrimaryPointer(out var primaryBefore);

            base.Apply(state, handler);

            // There may not be active pointers before button input application, Try checking after application.
            // As this might be a first pointer activation
            bool hasPrimaryAfter = touch.TryGetPrimaryPointer(out var primaryAfter);
            if (hasPrimaryBefore == hasPrimaryAfter && primaryBefore.Source == primaryAfter.Source)
                return;

            new MouseButtonInput(MouseButton.Left, hasPrimaryAfter).Apply(state, handler);
        }
    }
}
