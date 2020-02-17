// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Framework.Input.StateChanges.Events;
using osu.Framework.Input.States;
using osuTK.Input;

namespace osu.Framework.Input.StateChanges
{
    /// <summary>
    /// Denotes a change of the touch activity state (finger down, up).
    /// Any provided touch source should always be in the range <see cref="MouseButton.Touch1"/>-<see cref="MouseButton.Touch10"/>.
    /// </summary>
    public class TouchActivityInput : ButtonInput<MouseButton>
    {
        public TouchActivityInput(IEnumerable<ButtonInputEntry<MouseButton>> entries)
            : base(entries)
        {
            if (Entries.Any(e => e.Button < MouseButton.Touch1 || e.Button > MouseButton.Touch10))
                throw new ArgumentException($"Invalid touch source entry provided in: {entries}", nameof(entries));
        }

        public TouchActivityInput(MouseButton button, bool isActive)
            : base(button, isActive)
        {
            if (button < MouseButton.Touch1 || button > MouseButton.Touch10)
                throw new ArgumentException($"Invalid touch source provided: {button}", nameof(button));
        }

        public TouchActivityInput(ButtonStates<MouseButton> current, ButtonStates<MouseButton> previous)
            : base(current, previous)
        {
            if (Entries.Any(e => e.Button < MouseButton.Touch1 || e.Button > MouseButton.Touch10))
                throw new ArgumentException("Invalid touch source entry provided.");
        }

        protected override ButtonStates<MouseButton> GetButtonStates(InputState state) => state.Touch.ActiveSources;

        protected override ButtonStateChangeEvent<MouseButton> CreateEvent(InputState state, MouseButton button, ButtonStateChangeKind kind)
            => new TouchActivityChangeEvent(state, this, button, kind);

        protected override void OnButtonStateChanged(InputState state, MouseButton button, bool isPressed)
        {
            if (isPressed == false)
                state.Touch.TouchPositions[button - MouseButton.Touch1] = null;
        }
    }
}
