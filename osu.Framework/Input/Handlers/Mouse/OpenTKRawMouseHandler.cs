// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using osu.Framework.Configuration;
using osu.Framework.Platform;
using osu.Framework.Statistics;
using osu.Framework.Threading;
using OpenTK;
using OpenTK.Platform.Windows;

namespace osu.Framework.Input.Handlers.Mouse
{
    internal class OpenTKRawMouseHandler : InputHandler, IHasCursorSensitivity
    {
        private ScheduledDelegate scheduled;

        private bool mouseInWindow;

        private readonly BindableDouble sensitivity = new BindableDouble(1) { MinValue = 0.1, MaxValue = 10 };

        public BindableDouble Sensitivity => sensitivity;

        private readonly Bindable<ConfineMouseMode> confineMode = new Bindable<ConfineMouseMode>();
        private readonly Bindable<WindowMode> windowMode = new Bindable<WindowMode>();
        private readonly BindableBool mapAbsoluteInputToWindow = new BindableBool();

        private int mostSeenStates;
        private readonly List<OpenTKMouseState> lastStates = new List<OpenTKMouseState>();

        private GameHost host;

        public override bool Initialize(GameHost host)
        {
            host.Window.MouseEnter += window_MouseEnter;
            host.Window.MouseLeave += window_MouseLeave;

            this.host = host;

            mouseInWindow = host.Window.CursorInWindow;

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

                        if ((mouseInWindow || lastStates.Any(s => s.HasAnyButtonPressed)) && host.Window.Focused)
                        {
                            var newStates = new List<OpenTK.Input.MouseState>(mostSeenStates + 1);

                            for (int i = 0; i <= mostSeenStates + 1; i++)
                            {
                                var s = OpenTK.Input.Mouse.GetState(i);
                                if (s.IsConnected || i < mostSeenStates)
                                {
                                    newStates.Add(s);
                                    mostSeenStates = i;
                                }
                            }

                            while (lastStates.Count < newStates.Count)
                                lastStates.Add(null);

                            for (int i = 0; i < newStates.Count; i++)
                            {
                                if (newStates[i].IsConnected != true)
                                {
                                    lastStates[i] = null;
                                    continue;
                                }

                                var state = newStates[i];
                                var lastState = lastStates[i];

                                if (lastState != null && state.Equals(lastState.RawState))
                                    continue;

                                var newState = new OpenTKPollMouseState(state, host.IsActive, getUpdatedPosition(state, lastState));

                                lastStates[i] = newState;

                                if (lastState != null)
                                {
                                    PendingStates.Enqueue(new InputState { Mouse = newState });
                                    FrameStatistics.Increment(StatisticsCounterType.MouseEvents);
                                }
                            }
                        }
                        else
                        {
                            var state = OpenTK.Input.Mouse.GetCursorState();
                            var screenPoint = host.Window.PointToClient(new Point(state.X, state.Y));
                            PendingStates.Enqueue(new InputState { Mouse = new UnfocusedMouseState(new OpenTK.Input.MouseState(), host.IsActive, new Vector2(screenPoint.X, screenPoint.Y)) });
                            FrameStatistics.Increment(StatisticsCounterType.MouseEvents);

                            lastStates.Clear();
                        }
                    }, 0, 0));
                }
                else
                {
                    scheduled?.Cancel();
                    lastStates.Clear();
                }
            };

            Enabled.TriggerChange();
            return true;
        }

        private Vector2 getUpdatedPosition(OpenTK.Input.MouseState state, OpenTKMouseState lastState)
        {
            Vector2 currentPosition;

            if ((state.RawFlags & RawMouseFlags.MOUSE_MOVE_ABSOLUTE) > 0)
            {
                const int raw_input_resolution = 65536;

                if (mapAbsoluteInputToWindow)
                {
                    // map directly to local window
                    currentPosition.X = ((float)((state.X - raw_input_resolution / 2f) * sensitivity.Value) + raw_input_resolution / 2f) / raw_input_resolution * host.Window.Width;
                    currentPosition.Y = ((float)((state.Y - raw_input_resolution / 2f) * sensitivity.Value) + raw_input_resolution / 2f) / raw_input_resolution
                                        * host.Window.Height;
                }
                else
                {
                    Rectangle screenRect = (state.RawFlags & RawMouseFlags.MOUSE_VIRTUAL_DESKTOP) > 0
                        ? Platform.Windows.Native.Input.GetVirtualScreenRect()
                        : new Rectangle(0, 0, DisplayDevice.Default.Width, DisplayDevice.Default.Height);

                    // map to full screen space
                    currentPosition.X = (float)state.X / raw_input_resolution * screenRect.Width + screenRect.X;
                    currentPosition.Y = (float)state.Y / raw_input_resolution * screenRect.Height + screenRect.Y;

                    // find local window coordinates
                    var clientPos = host.Window.PointToClient(new Point((int)Math.Round(currentPosition.X), (int)Math.Round(currentPosition.Y)));

                    // apply sensitivity from window's centre
                    currentPosition.X = (float)((clientPos.X - host.Window.Width / 2f) * sensitivity.Value + host.Window.Width / 2f);
                    currentPosition.Y = (float)((clientPos.Y - host.Window.Height / 2f) * sensitivity.Value + host.Window.Height / 2f);
                }
            }
            else
            {
                if (lastState == null)
                {
                    // when we return from being outside of the window, we want to set the new position of our game cursor
                    // to where the OS cursor is, just once.
                    var cursorState = OpenTK.Input.Mouse.GetCursorState();
                    var screenPoint = host.Window.PointToClient(new Point(cursorState.X, cursorState.Y));
                    currentPosition = new Vector2(screenPoint.X, screenPoint.Y);
                }
                else
                {
                    currentPosition = lastState.Position + new Vector2(state.X - lastState.RawState.X, state.Y - lastState.RawState.Y) * (float)sensitivity.Value;

                    // When confining, clamp to the window size.
                    if (confineMode.Value == ConfineMouseMode.Always || confineMode.Value == ConfineMouseMode.Fullscreen && windowMode.Value == WindowMode.Fullscreen)
                        currentPosition = Vector2.Clamp(currentPosition, Vector2.Zero, new Vector2(host.Window.Width, host.Window.Height));

                    // update the windows cursor to match our raw cursor position.
                    // this is important when sensitivity is decreased below 1.0, where we need to ensure the cursor stays within the window.
                    var screenPoint = host.Window.PointToScreen(new Point((int)currentPosition.X, (int)currentPosition.Y));
                    OpenTK.Input.Mouse.SetPosition(screenPoint.X, screenPoint.Y);
                }
            }

            return currentPosition;
        }

        private void window_MouseLeave(object sender, EventArgs e) => mouseInWindow = false;
        private void window_MouseEnter(object sender, EventArgs e) => mouseInWindow = true;

        /// <summary>
        /// This input handler is always active, handling the cursor position if no other input handler does.
        /// </summary>
        public override bool IsActive => true;

        /// <summary>
        /// Lowest priority. We want the normal mouse handler to only kick in if all other handlers don't do anything.
        /// </summary>
        public override int Priority => 0;

        private class UnfocusedMouseState : OpenTKMouseState
        {
            public UnfocusedMouseState(OpenTK.Input.MouseState tkState, bool active, Vector2? mappedPosition)
                : base(tkState, active, mappedPosition)
            {
            }
        }
    }
}
