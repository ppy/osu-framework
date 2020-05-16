// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using osu.Framework.Extensions.IEnumerableExtensions;
using osu.Framework.Input.StateChanges.Events;
using osu.Framework.Input.States;

namespace osu.Framework.Input.StateChanges
{
    public class JoystickAxisInput : IInput
    {
        private readonly float[] axes = new float[JoystickState.MAX_AXES];

        public JoystickAxisInput(JoystickAxis axis)
            : this(axis.Yield())
        {
        }

        public JoystickAxisInput(IEnumerable<JoystickAxis> axes)
        {
            foreach (var axis in axes)
                this.axes[axis.Axis] = axis.Value;
        }

        public JoystickAxisInput(float[] axes)
        {
            this.axes = axes;
        }

        protected JoystickAxisChangeEvent CreateEvent(InputState state, JoystickAxis axis) => new JoystickAxisChangeEvent(state, this, axis);

        public void Apply(InputState state, IInputStateChangeHandler handler)
        {
            for (var i = 0; i < axes.Length; i++)
            {
                var axisChange = CreateEvent(state, new JoystickAxis(i, axes[i]));
                handler.HandleInputStateChange(axisChange);
            }
        }
    }
}
