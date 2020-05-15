// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Linq;
using osu.Framework.Input.StateChanges.Events;
using osu.Framework.Input.States;
using osu.Framework.Utils;

namespace osu.Framework.Input.StateChanges
{
    public class JoystickAxisInput : IInput
    {
        private List<JoystickAxis> Axes;

        public JoystickAxisInput(List<JoystickAxis> axes, List<JoystickAxis> previous)
        {
            if (previous == null)
            {
                Axes = axes;
                return;
            }

            Axes = new List<JoystickAxis>();

            for (var i = 0; i < axes.Count; i++)
            {

                if (previous.Any(a => a.Axis == axes[i].Axis))
                {
                    if (Precision.AlmostEquals(axes[i].Value, previous.First(a => a.Axis == axes[i].Axis).Value))
                        continue;
                }

                Axes.Add(axes[i]);
            }
        }

        protected JoystickAxisChangeEvent CreateEvent(InputState state, JoystickAxis axis) => new JoystickAxisChangeEvent(state, this, axis);

        public void Apply(InputState state, IInputStateChangeHandler handler)
        {
            foreach (var axis in axes)
            {
                var axisChange = CreateEvent(state, axis);
                handler.HandleInputStateChange(axisChange);
            }
        }
    }
}
