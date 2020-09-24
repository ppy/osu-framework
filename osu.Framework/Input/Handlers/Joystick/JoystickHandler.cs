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
                    window.JoystickButtonDown += handleJoystickButtonEvent;
                    window.JoystickButtonUp += handleJoystickButtonEvent;
                    window.JoystickAxisChanged += handleJoystickAxisChangedEvent;
                }
                else
                {
                    window.JoystickButtonDown -= handleJoystickButtonEvent;
                    window.JoystickButtonUp -= handleJoystickButtonEvent;
                    window.JoystickAxisChanged -= handleJoystickAxisChangedEvent;
                }
            }, true);

            return true;
        }

        private void handleJoystickAxisChangedEvent(JoystickAxisInput evt) => enqueueJoystickEvent(evt);

        private void handleJoystickButtonEvent(JoystickButtonInput evt) => enqueueJoystickEvent(evt);

        private void enqueueJoystickEvent(IInput evt)
        {
            PendingInputs.Enqueue(evt);
            FrameStatistics.Increment(StatisticsCounterType.JoystickEvents);
        }
    }
}
