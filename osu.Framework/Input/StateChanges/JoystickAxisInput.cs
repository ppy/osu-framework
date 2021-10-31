// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Framework.Extensions.IEnumerableExtensions;
using osu.Framework.Input.StateChanges.Events;
using osu.Framework.Input.States;
using osu.Framework.Utils;

namespace osu.Framework.Input.StateChanges
{
    public class JoystickAxisInput : IInput
    {
        /// <summary>
        /// List of joystick axes.
        /// </summary>
        public readonly IEnumerable<JoystickAxis> Axes;

        public JoystickAxisInput(JoystickAxis axis)
            : this(axis.Yield())
        {
        }

        public JoystickAxisInput(IEnumerable<JoystickAxis> axes)
        {
            if (axes.Count() > JoystickState.MAX_AXES)
                throw new ArgumentException($"The length of the provided axes collection ({axes.Count()}) exceeds the maximum length ({JoystickState.MAX_AXES})", nameof(axes));

            Axes = axes;
        }

        public void Apply(InputState state, IInputStateChangeHandler handler)
        {
            foreach (var a in Axes)
            {
                float oldValue = state.Joystick.AxesValues[(int)a.Source];

                // Not enough movement, don't fire event (unless returning to zero).
                if (oldValue == a.Value || (a.Value != 0 && Precision.AlmostEquals(oldValue, a.Value)))
                    continue;

                state.Joystick.AxesValues[(int)a.Source] = a.Value;
                handler.HandleInputStateChange(new JoystickAxisChangeEvent(state, this, a, oldValue));
            }
        }
    }
}
