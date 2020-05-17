// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

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
        private readonly IEnumerable<float> axes;

        public JoystickAxisInput(JoystickAxis axis)
            : this(axis.Yield())
        {
        }

        public JoystickAxisInput(IEnumerable<JoystickAxis> axes)
        {
            var array = new float[JoystickState.MAX_AXES];

            foreach (var axis in axes)
            {
                array[axis.Axis - InputAxis.FirstJoystickAxis] = axis.Value;
            }

            this.axes = array;
        }

        public void Apply(InputState state, IInputStateChangeHandler handler)
        {
            foreach (var a in axes.Select((v, i) => new JoystickAxis(i + InputAxis.FirstJoystickAxis, v)))
            {
                // Not enough movement, don't fire event
                if (Precision.AlmostEquals(state.Joystick.Axes[a.Axis - InputAxis.FirstJoystickAxis], a.Value))
                    continue;

                state.Joystick.Axes[a.Axis - InputAxis.FirstJoystickAxis] = a.Value;
                handler.HandleInputStateChange(new JoystickAxisChangeEvent(state, this, a));
            }
        }
    }
}
