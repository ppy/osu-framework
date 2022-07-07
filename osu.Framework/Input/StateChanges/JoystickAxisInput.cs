// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

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

                applyButtonInputsIfNeeded(state, handler, a);

                state.Joystick.AxesValues[(int)a.Source] = a.Value;
                handler.HandleInputStateChange(new JoystickAxisChangeEvent(state, this, a, oldValue));
            }
        }

        /// <summary>
        /// Applies <see cref="JoystickButtonInput"/> events depending on whether the axis has changed direction.
        /// </summary>
        private void applyButtonInputsIfNeeded(InputState state, IInputStateChangeHandler handler, JoystickAxis axis)
        {
            int index = (int)axis.Source;
            var currentButton = state.Joystick.AxisDirectionButtons[index];
            var expectedButton = getAxisButtonForInput(index, axis.Value);

            // if a directional button is pressed and does not match that for the new axis direction, release it
            if (currentButton != 0 && expectedButton != currentButton)
            {
                new JoystickButtonInput(currentButton, false).Apply(state, handler);
                state.Joystick.AxisDirectionButtons[index] = currentButton = 0;
            }

            // if we expect a directional button to be pressed, and it is not, press it
            if (expectedButton != 0 && expectedButton != currentButton)
            {
                new JoystickButtonInput(expectedButton, true).Apply(state, handler);
                state.Joystick.AxisDirectionButtons[index] = expectedButton;
            }
        }

        private static JoystickButton getAxisButtonForInput(int axisIndex, float axisValue)
        {
            if (axisValue > 0)
                return JoystickButton.FirstAxisPositive + axisIndex;

            if (axisValue < 0)
                return JoystickButton.FirstAxisNegative + axisIndex;

            return 0;
        }
    }
}
