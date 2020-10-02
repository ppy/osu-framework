// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Input.StateChanges;
using osu.Framework.Platform;
using osu.Framework.Statistics;

namespace osu.Framework.Input.Handlers.Joystick
{
    public class JoystickHandler : InputHandler
    {
        public override bool IsActive => true;

        public override int Priority => 0;

        public override bool Initialize(GameHost host)
        {
            if (!(host.Window is DesktopWindow window))
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

        private void enqueueJoystickAxisChanged(JoystickAxis axis) => enqueueJoystickEvent(new JoystickAxisInput(axis));
    }
}
