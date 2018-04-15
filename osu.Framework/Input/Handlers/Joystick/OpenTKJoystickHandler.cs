// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Framework.Input.Bindings;
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
            /// Arbitrary amount of axes supported by the osu!framework.
            /// We can raise this to 64 (max supported by OpenTK) with appropriate implementation in <see cref="InputKey"/> if needed.
            /// </summary>
            private const int max_axes = 10;

            /// <summary>
            /// Arbitrary amount of buttons supported by the osu!framework.
            /// We can raise this to 64 (max supported by OpenTK) with appropriate implementation in <see cref="InputKey"/> if needed.
            /// </summary>
            private const int max_buttons = 20;

            /// <summary>
            /// OpenTK only supports 4 hats.
            /// </summary>
            private const int max_hats = 4;

            private const float dead_zone = 0.4f;

            public OpenTKJoystickState(JoystickDevice device)
            {
                Axes = Enumerable.Range(0, max_axes).Select(i => device.State.GetAxis(i))
                                 // To not break compatibility if more axes are added in the future, pad axes to 64 elements
                                 .Concat(Enumerable.Repeat(0f, 64 - max_axes))
                                 // Convert each hat to a pair of axes
                                 .Concat(Enumerable.Range(0, max_hats).Select(i => device.State.GetHat(JoystickHat.Hat0 + i)).SelectMany(hatToAxes))
                                 .ToList();

                var buttons = Enumerable.Range(0, max_buttons).Where(i => device.State.GetButton(i) == ButtonState.Pressed).Select(i => (JoystickButton)i).ToList();

                for (int i = 0; i < Axes.Count; i++)
                {
                    if (Precision.AlmostEquals(Axes[i], 0, dead_zone))
                        continue;
                    buttons.Add((Axes[i] < 0 ? JoystickButton.AxisNegative1 : JoystickButton.AxisPositive1) + i);
                }

                Buttons = buttons;
            }

            private float[] hatToAxes(JoystickHatState hatState)
            {
                float xAxis = 0;
                float yAxis = 0;

                if (hatState.IsLeft)
                    xAxis = -1;
                else if (hatState.IsRight)
                    xAxis = 1;
                if (hatState.IsDown)
                    yAxis = -1;
                else if (hatState.IsUp)
                    yAxis = 1;

                return new[] { xAxis, yAxis };
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
