// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using osu.Framework.Bindables;
using osu.Framework.Input.StateChanges;
using osu.Framework.Platform;
using osu.Framework.Statistics;

namespace osu.Framework.Input.Handlers.Joystick
{
    public class JoystickHandler : InputHandler
    {
        public BindableFloat DeadzoneThreshold { get; } = new BindableFloat(0.1f)
        {
            MinValue = 0,
            MaxValue = 0.95f,
            Precision = 0.005f,
        };

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
        /// </summary>
        private void enqueueJoystickAxisChanged(JoystickAxisSource source, float value) =>
            enqueueJoystickEvent(new JoystickAxisInput(new JoystickAxis(source, RescaleByDeadzone(value, DeadzoneThreshold.Value))));

        internal static float RescaleByDeadzone(float axisValue, float deadzoneThreshold)
        {
            float absoluteValue = Math.Abs(axisValue);

            if (absoluteValue < deadzoneThreshold)
                return 0;

            // rescale the given axis value such that the edge of the deadzone is considered the "new zero".
            float absoluteRescaled = (absoluteValue - deadzoneThreshold) / (1f - deadzoneThreshold);
            return Math.Sign(axisValue) * absoluteRescaled;
        }
    }
}
