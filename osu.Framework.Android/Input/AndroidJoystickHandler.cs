// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
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
            // joysticks sometimes present themselves as a keyboard (in addition to Gamepad) when buttons are pressed. see `ShouldHandleEvent`
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

        /// <remarks>See xmldoc <see cref="AndroidKeyboardHandler.ShouldHandleEvent"/></remarks>
        protected override bool ShouldHandleEvent(InputEvent? inputEvent) => base.ShouldHandleEvent(inputEvent) && inputEvent.Source != InputSourceType.Keyboard;

        protected override void OnGenericMotion(MotionEvent genericMotionEvent)
        {
            switch (genericMotionEvent.Action)
            {
                case MotionEventActions.Move:
                    // PointerCount / GetPointerId is probably related to number of joysticks connected. framework only handles one joystick, so coalesce instead of handling separately.
                    for (int i = 0; i < genericMotionEvent.PointerCount; i++)
                    {
                        var motionRanges = genericMotionEvent.Device?.MotionRanges;

                        enqueueInput(motionRanges != null && motionRanges.Count > 0
                            ? new JoystickAxisInput(getJoystickAxesForMotionRange(genericMotionEvent, i, motionRanges))
                            : new JoystickAxisInput(getAllJoystickAxes(genericMotionEvent, i)));

                        foreach (var input in getDpadInputs(genericMotionEvent, i))
                            enqueueInput(input);
                    }

                    break;
            }
        }

        protected override void OnKeyDown(Keycode keycode, KeyEvent e)
        {
            if (keycode.TryGetJoystickButton(out var button))
                enqueueInput(new JoystickButtonInput(button, true));
        }

        protected override void OnKeyUp(Keycode keycode, KeyEvent e)
        {
            if (keycode.TryGetJoystickButton(out var button))
                enqueueInput(new JoystickButtonInput(button, false));
        }

        private IEnumerable<JoystickAxis> getJoystickAxesForMotionRange(MotionEvent motionEvent, int pointerIndex, IEnumerable<InputDevice.MotionRange> motionRanges)
        {
            foreach (var motionRange in motionRanges)
            {
                if (tryGetJoystickAxis(motionEvent, pointerIndex, motionRange.Axis, out var joystickAxis))
                    yield return joystickAxis;
            }
        }

        private IEnumerable<JoystickAxis> getAllJoystickAxes(MotionEvent motionEvent, int pointerIndex)
        {
            foreach (var axis in AndroidInputExtensions.ALL_AXES)
            {
                if (tryGetJoystickAxis(motionEvent, pointerIndex, axis, out var joystickAxis))
                    yield return joystickAxis;
            }
        }

        private bool tryGetJoystickAxis(MotionEvent motionEvent, int pointerIndex, Axis axis, out JoystickAxis joystickAxis)
        {
            if (!axis.TryGetJoystickAxisSource(out var joystickAxisSource))
            {
                if (axis != Axis.HatX && axis != Axis.HatY)
                    Logger.Log($"Unknown joystick axis: {axis}");

                joystickAxis = new JoystickAxis();
                return false;
            }

            float value = motionEvent.GetAxisValue(axis, pointerIndex);

            if (float.IsNaN(value))
            {
                joystickAxis = new JoystickAxis();
                return false;
            }

            value = JoystickHandler.RescaleByDeadzone(value, DeadzoneThreshold.Value);

            joystickAxis = new JoystickAxis(joystickAxisSource, value);
            return true;
        }

        private float lastDpadX;
        private float lastDpadY;

        private IEnumerable<JoystickButtonInput> getDpadInputs(MotionEvent motionEvent, int pointerIndex)
        {
            float x = motionEvent.GetAxisValue(Axis.HatX, pointerIndex);

            if (x != lastDpadX)
            {
                if (x == 0) yield return new JoystickButtonInput(lastDpadX > 0 ? JoystickButton.GamePadDPadRight : JoystickButton.GamePadDPadLeft, false);
                if (x > 0) yield return new JoystickButtonInput(JoystickButton.GamePadDPadRight, true);
                if (x < 0) yield return new JoystickButtonInput(JoystickButton.GamePadDPadLeft, true);

                lastDpadX = x;
            }

            float y = motionEvent.GetAxisValue(Axis.HatY, pointerIndex);

            if (y != lastDpadY)
            {
                if (y == 0) yield return new JoystickButtonInput(lastDpadY > 0 ? JoystickButton.GamePadDPadDown : JoystickButton.GamePadDPadUp, false);
                if (y > 0) yield return new JoystickButtonInput(JoystickButton.GamePadDPadDown, true);
                if (y < 0) yield return new JoystickButtonInput(JoystickButton.GamePadDPadUp, true);

                lastDpadY = y;
            }
        }

        private void enqueueInput(IInput input)
        {
            PendingInputs.Enqueue(input);
            FrameStatistics.Increment(StatisticsCounterType.JoystickEvents);
        }
    }
}
