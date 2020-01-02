// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using osu.Framework.Bindables;
using osu.Framework.Configuration;
using osu.Framework.Platform;
using osu.Framework.Threading;
using osuTK;
using osuTK.Input;

namespace osu.Framework.Input.Handlers.Mouse
{
    internal class OsuTKRawMouseHandler : OsuTKMouseHandlerBase, IHasCursorSensitivity, INeedsMousePositionFeedback
    {
        private ScheduledDelegate scheduled;

        public BindableDouble Sensitivity { get; } = new BindableDouble(1) { MinValue = 0.1, MaxValue = 10 };

        private readonly Bindable<ConfineMouseMode> confineMode = new Bindable<ConfineMouseMode>();
        private readonly Bindable<WindowMode> windowMode = new Bindable<WindowMode>();
        private readonly BindableBool mapAbsoluteInputToWindow = new BindableBool();

        private readonly List<OsuTKMouseState> lastEachDeviceStates = new List<OsuTKMouseState>();
        private readonly List<MouseState> newRawStates = new List<MouseState>();
        private OsuTKMouseState lastUnfocusedState;

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

            Enabled.BindValueChanged(e =>
            {
                if (e.NewValue)
                {
                    host.InputThread.Scheduler.Add(scheduled = new ScheduledDelegate(delegate
                    {
                        if (!host.Window.Visible || host.Window.WindowState == WindowState.Minimized)
                            return;

                        if ((MouseInWindow || lastEachDeviceStates.Any(s => s != null && s.Buttons.HasAnyButtonPressed)) && host.Window.Focused)
                        {
                            osuTK.Input.Mouse.GetStates(newRawStates);

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

                                var newState = new OsuTKPollMouseState(rawState, host.IsActive.Value, getUpdatedPosition(rawState, lastState));

                                HandleState(newState, lastState, rawState.Flags.HasFlag(MouseStateFlags.MoveAbsolute));

                                lastEachDeviceStates[i] = newState;
                                lastUnfocusedState = null;
                            }
                        }
                        else
                        {
                            var state = osuTK.Input.Mouse.GetCursorState();
                            var screenPoint = host.Window.PointToClient(new Point(state.X, state.Y));

                            var newState = new UnfocusedMouseState(new MouseState(), host.IsActive.Value, new Vector2(screenPoint.X, screenPoint.Y));

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
            }, true);

            return true;
        }

        public void FeedbackMousePositionChange(Vector2 position)
        {
            // only set the cursor position when focused
            if (lastUnfocusedState != null) return;

            var screenPoint = Host.Window.PointToScreen(new Point((int)position.X, (int)position.Y));

            osuTK.Input.Mouse.SetPosition(screenPoint.X, screenPoint.Y);
        }

        private Vector2 getUpdatedPosition(MouseState state, OsuTKMouseState lastState)
        {
            Vector2 currentPosition;

            if (state.Flags.HasFlag(MouseStateFlags.MoveAbsolute))
            {
                const int raw_input_resolution = 65536;

                if (mapAbsoluteInputToWindow.Value)
                {
                    // map directly to local window
                    currentPosition.X = ((float)((state.X - raw_input_resolution / 2f) * Sensitivity.Value) + raw_input_resolution / 2f) / raw_input_resolution * Host.Window.Width;
                    currentPosition.Y = ((float)((state.Y - raw_input_resolution / 2f) * Sensitivity.Value) + raw_input_resolution / 2f) / raw_input_resolution
                                        * Host.Window.Height;
                }
                else
                {
                    Rectangle screenRect = state.Flags.HasFlag(MouseStateFlags.VirtualDesktop)
                        ? Platform.Windows.Native.Input.GetVirtualScreenRect()
                        : new Rectangle(0, 0, DisplayDevice.Default.Width, DisplayDevice.Default.Height);

                    // map to full screen space
                    currentPosition.X = (float)state.X / raw_input_resolution * screenRect.Width + screenRect.X;
                    currentPosition.Y = (float)state.Y / raw_input_resolution * screenRect.Height + screenRect.Y;

                    // find local window coordinates
                    var clientPos = Host.Window.PointToClient(new Point((int)Math.Round(currentPosition.X), (int)Math.Round(currentPosition.Y)));

                    // apply sensitivity from window's centre
                    currentPosition.X = (float)((clientPos.X - Host.Window.Width / 2f) * Sensitivity.Value + Host.Window.Width / 2f);
                    currentPosition.Y = (float)((clientPos.Y - Host.Window.Height / 2f) * Sensitivity.Value + Host.Window.Height / 2f);
                }
            }
            else
            {
                if (lastState == null)
                {
                    // when we return from being outside of the window, we want to set the new position of our game cursor
                    // to where the OS cursor is, just once.
                    var cursorState = osuTK.Input.Mouse.GetCursorState();
                    var screenPoint = Host.Window.PointToClient(new Point(cursorState.X, cursorState.Y));
                    currentPosition = new Vector2(screenPoint.X, screenPoint.Y);
                }
                else
                {
                    currentPosition = lastState.Position + new Vector2(state.X - lastState.RawState.X, state.Y - lastState.RawState.Y) * (float)Sensitivity.Value;
                }
            }

            return currentPosition;
        }

        private class UnfocusedMouseState : OsuTKMouseState
        {
            public UnfocusedMouseState(MouseState tkState, bool active, Vector2? mappedPosition)
                : base(tkState, active, mappedPosition)
            {
            }
        }
    }
}
