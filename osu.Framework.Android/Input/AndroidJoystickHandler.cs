// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Linq;
using Android.Views;
using osu.Framework.Bindables;
using osu.Framework.Input;
using osu.Framework.Input.Handlers.Joystick;
using osu.Framework.Input.StateChanges;
using osu.Framework.Logging;
using osu.Framework.Platform;
using osu.Framework.Statistics;

namespace osu.Framework.Android.Input
{
    public class AndroidJoystickHandler : AndroidInputHandler
    {
        public BindableFloat DeadzoneThreshold { get; } = new BindableFloat(0.1f)
        {
            MinValue = 0,
            MaxValue = 0.95f,
            Precision = 0.005f,
        };

        public override string Description => "Joystick / Gamepad";

        public override bool IsActive => true;

        protected override IEnumerable<InputSourceType> HandledEventSources => new[]
        {
            InputSourceType.Dpad,
            InputSourceType.Gamepad,
            InputSourceType.Joystick,
            // joysticks sometimes present themselves as a keyboard in OnKey{Up,Down} events.
            InputSourceType.Keyboard
        };

        public AndroidJoystickHandler(AndroidGameView view)
            : base(view)
        {
        }

        public override bool Initialize(GameHost host)
        {
            if (!base.Initialize(host))
                return false;

            Enabled.BindValueChanged(enabled =>
            {
                if (enabled.NewValue)
                {
                    View.GenericMotion += HandleGenericMotion;
                    View.KeyDown += HandleKeyDown;
                    View.KeyUp += HandleKeyUp;
                }
                else
                {
                    View.GenericMotion -= HandleGenericMotion;
                    View.KeyDown -= HandleKeyDown;
                    View.KeyUp -= HandleKeyUp;
                }
            }, true);

            return true;
        }

        protected override bool OnKeyDown(Keycode keycode, KeyEvent e)
        {
            if (e.TryGetJoystickButton(out var button))
            {
                enqueueButtonDown(button);
                return true;
            }

            // keyboard only events are handled in AndroidKeyboardHandler
            return e.Source == InputSourceType.Keyboard;
        }

        protected override bool OnKeyUp(Keycode keycode, KeyEvent e)
        {
            if (e.TryGetJoystickButton(out var button))
            {
                enqueueButtonUp(button);
                return true;
            }

            // keyboard only events are handled in AndroidKeyboardHandler
            return e.Source == InputSourceType.Keyboard;
        }

        /// <summary>
        /// The <see cref="InputDevice"/> for which the <see cref="availableAxes"/> are valid.
        /// <c>null</c> iff the current device could not be determined, in that case, <see cref="availableAxes"/> fall back to <see cref="AndroidInputExtensions.ALL_AXES"/>.
        /// </summary>
        private string? lastDeviceDescriptor;

        /// <summary>
        /// The axes that are reported as supported by the current <see cref="MotionEvent"/>.<see cref="InputDevice"/>.
        /// <see cref="AndroidInputExtensions.ALL_AXES"/> if the current device doesn't report axes information.
        /// </summary>
        private IEnumerable<Axis> availableAxes = AndroidInputExtensions.ALL_AXES;

        /// <summary>
        /// Updates <see cref="availableAxes"/> to be appropriate for the current <paramref name="device"/>.
        /// </summary>
        private void updateAvailableAxesForDevice(InputDevice? device)
        {
            if (device?.Descriptor == null)
            {
                if (lastDeviceDescriptor == null)
                    return;

                // use the default if this device is unknown.
                lastDeviceDescriptor = null;
                availableAxes = AndroidInputExtensions.ALL_AXES;
                return;
            }

            if (device.Descriptor == lastDeviceDescriptor)
                return;

            lastDeviceDescriptor = device.Descriptor;

            var motionRanges = device.MotionRanges;

            availableAxes = motionRanges != null && motionRanges.Count > 0
                ? motionRanges.Select(m => m.Axis).Where(isValid).Distinct().ToList()
                : AndroidInputExtensions.ALL_AXES;

            bool isValid(Axis axis)
            {
                switch (axis)
                {
                    // D-pad axes are handled separately in `applyDpadInput`
                    case Axis.HatX:
                    case Axis.HatY:
                    // Brake and Gas axes mirror the left and right trigger and are therefore ignored
                    case Axis.Gas:
                    case Axis.Brake:
                        return false;
                }

                if (axis.TryGetJoystickAxisSource(out _))
                    return true;

                Logger.Log($"Unknown joystick axis: {axis}");
                return false;
            }
        }

        protected override bool OnGenericMotion(MotionEvent genericMotionEvent)
        {
            switch (genericMotionEvent.Action)
            {
                case MotionEventActions.Move:
                    updateAvailableAxesForDevice(genericMotionEvent.Device);
                    genericMotionEvent.HandleHistorically(apply);
                    return true;

                default:
                    return false;
            }
        }

        private void apply(MotionEvent motionEvent, int historyPosition)
        {
            foreach (var axis in availableAxes)
                applyAxisInput(motionEvent, historyPosition, axis);

            applyDpadInput(motionEvent, historyPosition);
        }

        private void applyAxisInput(MotionEvent motionEvent, int historyPosition, Axis axis)
        {
            if (axis.TryGetJoystickAxisSource(out var joystickAxisSource)
                && motionEvent.TryGet(axis, out float value, historyPosition))
            {
                value = JoystickHandler.RescaleByDeadzone(value, DeadzoneThreshold.Value);
                enqueueInput(new JoystickAxisInput(new JoystickAxis(joystickAxisSource, value)));
            }
        }

        private float lastDpadX;
        private float lastDpadY;

        private void applyDpadInput(MotionEvent motionEvent, int historyPosition)
        {
            float x = motionEvent.Get(Axis.HatX, historyPosition);

            if (x != lastDpadX)
            {
                if (x == 0) enqueueButtonUp(lastDpadX > 0 ? JoystickButton.GamePadDPadRight : JoystickButton.GamePadDPadLeft);
                if (x > 0) enqueueButtonDown(JoystickButton.GamePadDPadRight);
                if (x < 0) enqueueButtonDown(JoystickButton.GamePadDPadLeft);

                lastDpadX = x;
            }

            float y = motionEvent.Get(Axis.HatY, historyPosition);

            if (y != lastDpadY)
            {
                if (y == 0) enqueueButtonUp(lastDpadY > 0 ? JoystickButton.GamePadDPadDown : JoystickButton.GamePadDPadUp);
                if (y > 0) enqueueButtonDown(JoystickButton.GamePadDPadDown);
                if (y < 0) enqueueButtonDown(JoystickButton.GamePadDPadUp);

                lastDpadY = y;
            }
        }

        private void enqueueButtonDown(JoystickButton button) => enqueueInput(new JoystickButtonInput(button, true));
        private void enqueueButtonUp(JoystickButton button) => enqueueInput(new JoystickButtonInput(button, false));

        private void enqueueInput(IInput input)
        {
            PendingInputs.Enqueue(input);
            FrameStatistics.Increment(StatisticsCounterType.JoystickEvents);
        }
    }
}
