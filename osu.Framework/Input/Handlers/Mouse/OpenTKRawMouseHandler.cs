// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using osu.Framework.Configuration;
using osu.Framework.Platform;
using osu.Framework.Threading;
using OpenTK;
using OpenTK.Input;

namespace osu.Framework.Input.Handlers.Mouse
{
    internal class OpenTKRawMouseHandler : OpenTKMouseHandlerBase, IHasCursorSensitivity, INeedsMousePositionFeedback
    {
        private ScheduledDelegate scheduled;

        private readonly BindableDouble sensitivity = new BindableDouble(1) { MinValue = 0.1, MaxValue = 10 };

        public BindableDouble Sensitivity => sensitivity;

        private readonly Bindable<ConfineMouseMode> confineMode = new Bindable<ConfineMouseMode>();
        private readonly Bindable<WindowMode> windowMode = new Bindable<WindowMode>();
        private readonly BindableBool mapAbsoluteInputToWindow = new BindableBool();

        private int mostSeenStates;
        private readonly List<OpenTKMouseState> lastEachDeviceStates = new List<OpenTKMouseState>();
        private OpenTKMouseState lastUnfocusedState;

        public override bool Initialize(GameHost host)
        {
            base.Initialize(host);

            // Get the bindables we need to determine whether to confine the mouse to window or not
            if (host.Window is DesktopGameWindow desktopWindow)
            {
                confineMode.BindTo(desktopWindow.ConfineMouseMode);
                windowMode.BindTo(desktopWindow.WindowMode);
                mapAbsoluteInputToWindow.BindTo(desktopWindow.MapAbsoluteInputToWindow);
            }

            Enabled.ValueChanged += enabled =>
            {
                if (enabled)
                {
                    host.InputThread.Scheduler.Add(scheduled = new ScheduledDelegate(delegate
                    {
                        if (!host.Window.Visible || host.Window.WindowState == WindowState.Minimized)
                            return;

                        if ((MouseInWindow || lastEachDeviceStates.Any(s => s != null && s.HasAnyButtonPressed)) && host.Window.Focused)
                        {
                            var newRawStates = new List<OpenTK.Input.MouseState>(mostSeenStates + 1);

                            for (int i = 0; i <= mostSeenStates + 1; i++)
                            {
                                var s = OpenTK.Input.Mouse.GetState(i);
                                if (s.IsConnected || i < mostSeenStates)
                                {
                                    newRawStates.Add(s);
                                    mostSeenStates = i;
                                }
                            }

                            while (lastEachDeviceStates.Count < newRawStates.Count)
                                lastEachDeviceStates.Add(null);

                            for (int i = 0; i < newRawStates.Count; i++)
                            {
                                if (newRawStates[i].IsConnected != true)
                                {
                                    lastEachDeviceStates[i] = null;
                                    continue;
                                }

                                var rawState = newRawStates[i];
                                var lastState = lastEachDeviceStates[i];

                                if (lastState != null && rawState.Equals(lastState.RawState))
                                    continue;

                                var newState = new OpenTKPollMouseState(rawState, host.IsActive, getUpdatedPosition(rawState, lastState));

                                HandleState(newState, lastState, (rawState.Flags & MouseStateFlags.MoveAbsolute) > 0);

                                lastEachDeviceStates[i] = newState;
                                lastUnfocusedState = null;
                            }
                        }
                        else
                        {
                            var state = OpenTK.Input.Mouse.GetCursorState();
                            var screenPoint = host.Window.PointToClient(new Point(state.X, state.Y));

                            var newState = new UnfocusedMouseState(new OpenTK.Input.MouseState(), host.IsActive, new Vector2(screenPoint.X, screenPoint.Y));

                            HandleState(newState, lastUnfocusedState, true);

                            lastUnfocusedState = newState;
                            lastEachDeviceStates.Clear();
                        }
                    }, 0, 0));
                }
                else
                {
                    scheduled?.Cancel();
                    lastEachDeviceStates.Clear();
                    lastUnfocusedState = null;
                }
            };

            Enabled.TriggerChange();
            return true;
        }

        public void FeedbackMousePositionChange(Vector2 position)
        {
            // only set the cursor position when focused
            if (lastUnfocusedState != null) return;

            var screenPoint = Host.Window.PointToScreen(new Point((int)position.X, (int)position.Y));

            OpenTK.Input.Mouse.SetPosition(screenPoint.X, screenPoint.Y);
        }

        private Vector2 getUpdatedPosition(OpenTK.Input.MouseState state, OpenTKMouseState lastState)
        {
            Vector2 currentPosition;

            if ((state.Flags & MouseStateFlags.MoveAbsolute) > 0)
            {
                const int raw_input_resolution = 65536;

                if (mapAbsoluteInputToWindow)
                {
                    // map directly to local window
                    currentPosition.X = ((float)((state.X - raw_input_resolution / 2f) * sensitivity.Value) + raw_input_resolution / 2f) / raw_input_resolution * Host.Window.Width;
                    currentPosition.Y = ((float)((state.Y - raw_input_resolution / 2f) * sensitivity.Value) + raw_input_resolution / 2f) / raw_input_resolution
                                        * Host.Window.Height;
                }
                else
                {
                    Rectangle screenRect = (state.Flags & MouseStateFlags.VirtualDesktop) > 0
                        ? Platform.Windows.Native.Input.GetVirtualScreenRect()
                        : new Rectangle(0, 0, DisplayDevice.Default.Width, DisplayDevice.Default.Height);

                    // map to full screen space
                    currentPosition.X = (float)state.X / raw_input_resolution * screenRect.Width + screenRect.X;
                    currentPosition.Y = (float)state.Y / raw_input_resolution * screenRect.Height + screenRect.Y;

                    // find local window coordinates
                    var clientPos = Host.Window.PointToClient(new Point((int)Math.Round(currentPosition.X), (int)Math.Round(currentPosition.Y)));

                    // apply sensitivity from window's centre
                    currentPosition.X = (float)((clientPos.X - Host.Window.Width / 2f) * sensitivity.Value + Host.Window.Width / 2f);
                    currentPosition.Y = (float)((clientPos.Y - Host.Window.Height / 2f) * sensitivity.Value + Host.Window.Height / 2f);
                }
            }
            else
            {
                if (lastState == null)
                {
                    // when we return from being outside of the window, we want to set the new position of our game cursor
                    // to where the OS cursor is, just once.
                    var cursorState = OpenTK.Input.Mouse.GetCursorState();
                    var screenPoint = Host.Window.PointToClient(new Point(cursorState.X, cursorState.Y));
                    currentPosition = new Vector2(screenPoint.X, screenPoint.Y);
                }
                else
                {
                    currentPosition = lastState.Position + new Vector2(state.X - lastState.RawState.X, state.Y - lastState.RawState.Y) * (float)sensitivity.Value;
                }
            }

            return currentPosition;
        }

        private class UnfocusedMouseState : OpenTKMouseState
        {
            public UnfocusedMouseState(OpenTK.Input.MouseState tkState, bool active, Vector2? mappedPosition)
                : base(tkState, active, mappedPosition)
            {
            }
        }
    }
}
