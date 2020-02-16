// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Diagnostics;
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
            Trace.Assert(Entries.All(e => e.Button >= MouseButton.Touch1));
        }

        public TouchActivityInput(MouseButton button, bool isActive)
            : base(button, isActive)
        {
            Trace.Assert(button >= MouseButton.Touch1);
        }

        public TouchActivityInput(ButtonStates<MouseButton> current, ButtonStates<MouseButton> previous)
            : base(current, previous)
        {
            Trace.Assert(Entries.All(e => e.Button >= MouseButton.Touch1));
        }

        protected override ButtonStates<MouseButton> GetButtonStates(InputState state) => state.Touch.ActiveSources;

        protected override ButtonStateChangeEvent<MouseButton> CreateEvent(InputState state, MouseButton button, ButtonStateChangeKind kind)
            => new TouchActivityChangeEvent(state, this, button, kind);

        protected override void OnButtonStateChanged(InputState state, MouseButton button, bool isPressed)
        {
            if (isPressed == false)
                state.Touch.TouchPositions.Remove(button);
        }
    }
}
