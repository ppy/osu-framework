// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Collections.Generic;
using osu.Framework.Logging;
using osu.Framework.MathUtils;
using osu.Framework.Platform;
using osu.Framework.Statistics;
using osu.Framework.Threading;
using OpenTK.Input;

namespace osu.Framework.Input.Handlers.Joystick
{
    public class OpenTKJoystickHandler : InputHandler
    {
        private ScheduledDelegate scheduled;

        private int mostSeenDevices;
        private List<JoystickDevice> devices = new List<JoystickDevice>();

        public override bool Initialize(GameHost host)
        {
            Enabled.ValueChanged += enabled =>
            {
                if (enabled)
                {
                    host.InputThread.Scheduler.Add(scheduled = new ScheduledDelegate(delegate
                    {
                        refreshDevices();

                        foreach (var device in devices)
                        {
                            if (device.State.Equals(device.LastState))
                                continue;

                            if (device.LastState != null)
                            {
                                PendingStates.Enqueue(new InputState { Joystick = new OpenTKJoystickState(device) });
                                FrameStatistics.Increment(StatisticsCounterType.JoystickEvents);
                            }
                        }
                    }, 0, 0));
                }
                else
                {
                    scheduled?.Cancel();
                    devices.Clear();
                }
            };

            Enabled.TriggerChange();

            return true;
        }

        private void refreshDevices()
        {
            var newDevices = new List<JoystickDevice>();

            // Update devices and add them to newDevices if still connected
            foreach (var dev in devices)
            {
                dev.Refresh();

                if (dev.State.IsConnected)
                    newDevices.Add(dev);
                else
                    mostSeenDevices--;
            }

            // Find any newly-connected devices
            while (true)
            {
                var newDevice = new JoystickDevice(mostSeenDevices);
                if (!newDevice.Capabilities.IsConnected)
                    break;

                Logger.Log($"Connected joystick device: {newDevice.Guid}", level: LogLevel.Important);

                newDevices.Add(newDevice);
                mostSeenDevices++;
            }

            devices = newDevices;
        }

        public override bool IsActive => true;
        public override int Priority => 0;

        private class OpenTKJoystickState : JoystickState
        {
            /// <summary>
            /// Amount of axes supported by OpenTK.
            /// </summary>
            private const int max_axes = 64;

            /// <summary>
            /// Amount of buttons supported by OpenTK.
            /// </summary>
            private const int max_buttons = 64;

            /// <summary>
            /// Amount of hats supported by OpenTK.
            /// </summary>
            private const int max_hats = 4;

            private const float dead_zone = 0.4f;

            public OpenTKJoystickState(JoystickDevice device)
            {
                // Populate axes
                var axes = new List<JoystickAxis>();
                for (int i = 0; i < max_axes; i++)
                {
                    var value = device.State.GetAxis(i);
                    if (!Precision.AlmostEquals(value, 0, dead_zone))
                        axes.Add(new JoystickAxis(i, value));
                }

                Axes = axes;

                // Populate normal buttons
                var buttons = new List<JoystickButton>();
                for (int i = 0; i < max_buttons; i++)
                {
                    if (device.State.GetButton(i) == ButtonState.Pressed)
                        buttons.Add((JoystickButton)i);
                }

                // Populate hat buttons
                for (int i = 0; i < max_hats; i++)
                {
                    foreach (var hatButton in getHatButtons(device, i))
                        buttons.Add(hatButton);
                }

                // Populate axis buttons (each axis has two buttons)
                for (int i = 0; i < Axes.Count; i++)
                    buttons.Add((Axes[i].Value < 0 ? JoystickButton.AxisNegative1 : JoystickButton.AxisPositive1) + i);

                Buttons = buttons;
            }

            private IEnumerable<JoystickButton> getHatButtons(JoystickDevice device, int hat)
            {
                var state = device.State.GetHat(JoystickHat.Hat0 + hat);

                if (state.IsUp)
                    yield return JoystickButton.HatUp1 + hat;
                else if (state.IsDown)
                    yield return JoystickButton.HatDown1 + hat;

                if (state.IsLeft)
                    yield return JoystickButton.HatLeft1 + hat;
                else if (state.IsRight)
                    yield return JoystickButton.HatRight1 + hat;
            }
        }

        private class JoystickDevice
        {
            /// <summary>
            /// The last state of this <see cref="JoystickDevice"/>.
            /// This is updated with ever invocation of <see cref="Refresh"/>.
            /// </summary>
            public OpenTK.Input.JoystickState? LastState { get; private set; }

            /// <summary>
            /// The current state of this <see cref="JoystickDevice"/>.
            /// Use <see cref="Refresh"/> to update the state.
            /// </summary>
            public OpenTK.Input.JoystickState State { get; private set; }

            /// <summary>
            /// The capabilities for this joystick device.
            /// This is only queried once when <see cref="JoystickDevice"/> is constructed.
            /// </summary>
            public readonly JoystickCapabilities Capabilities;

            /// <summary>
            /// The <see cref="Guid"/> for this <see cref="JoystickDevice"/>.
            /// </summary>
            public readonly Guid Guid;

            private readonly int deviceIndex;

            public JoystickDevice(int deviceIndex)
            {
                this.deviceIndex = deviceIndex;

                Capabilities = OpenTK.Input.Joystick.GetCapabilities(deviceIndex);
                Guid = OpenTK.Input.Joystick.GetGuid(deviceIndex);

                Refresh();
            }

            /// <summary>
            /// Refreshes the state of this <see cref="JoystickDevice"/>.
            /// </summary>
            public void Refresh()
            {
                LastState = State;
                State = OpenTK.Input.Joystick.GetState(deviceIndex);
            }
        }
    }
}
