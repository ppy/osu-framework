// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Input.StateChanges;
using osu.Framework.Platform;
using osu.Framework.Statistics;

namespace osu.Framework.Input.Handlers.Joystick
{
    public class JoystickHandler : InputHandler
    {
        private const float deadzone_threshold = 0.075f;

        private readonly JoystickButton[] axisDirectionButtons = new JoystickButton[(int)JoystickAxisSource.AxisCount];

        public override string Description => "Joystick / Gamepad";

        public override bool IsActive => true;

        public override bool Initialize(GameHost host)
        {
            if (!base.Initialize(host))
                return false;

            if (!(host.Window is SDL2DesktopWindow window))
                return false;

            Enabled.BindValueChanged(e =>
            {
                if (e.NewValue)
                {
                    window.JoystickButtonDown += enqueueJoystickButtonDown;
                    window.JoystickButtonUp += enqueueJoystickButtonUp;
                    window.JoystickAxisChanged += enqueueJoystickAxisChanged;
                }
                else
                {
                    window.JoystickButtonDown -= enqueueJoystickButtonDown;
                    window.JoystickButtonUp -= enqueueJoystickButtonUp;
                    window.JoystickAxisChanged -= enqueueJoystickAxisChanged;
                }
            }, true);

            return true;
        }

        private void enqueueJoystickEvent(IInput evt)
        {
            PendingInputs.Enqueue(evt);
            FrameStatistics.Increment(StatisticsCounterType.JoystickEvents);
        }

        private void enqueueJoystickButtonDown(JoystickButton button) => enqueueJoystickEvent(new JoystickButtonInput(button, true));

        private void enqueueJoystickButtonUp(JoystickButton button) => enqueueJoystickEvent(new JoystickButtonInput(button, false));

        /// <summary>
        /// Enqueues a <see cref="JoystickAxisInput"/> taking into account the axis deadzone.
        /// Also enqueues <see cref="JoystickButtonInput"/> events depending on whether the axis has changed direction.
        /// </summary>
        private void enqueueJoystickAxisChanged(JoystickAxis axis)
        {
            float value = rescaleByDeadzone(axis.Value);

            int index = (int)axis.Source;
            var currentButton = axisDirectionButtons[index];
            var expectedButton = getAxisButtonForInput(index, value);

            // if a directional button is pressed and does not match that for the new axis direction, release it
            if (currentButton != 0 && expectedButton != currentButton)
            {
                enqueueJoystickButtonUp(currentButton);
                axisDirectionButtons[index] = currentButton = 0;
            }

            // if we expect a directional button to be pressed, and it is not, press it
            if (expectedButton != 0 && expectedButton != currentButton)
            {
                enqueueJoystickButtonDown(expectedButton);
                axisDirectionButtons[index] = expectedButton;
            }

            enqueueJoystickEvent(new JoystickAxisInput(new JoystickAxis(axis.Source, value)));
        }

        private static float rescaleByDeadzone(float axisValue)
        {
            float absoluteValue = Math.Abs(axisValue);

            if (absoluteValue < deadzone_threshold)
                return 0;

            // rescale the given axis value such that the edge of the deadzone is considered the "new zero".
            float absoluteRescaled = (absoluteValue - deadzone_threshold) / (1f - deadzone_threshold);
            return Math.Sign(axisValue) * absoluteRescaled;
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
