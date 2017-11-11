// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System.Drawing;
using osu.Framework.Configuration;
using osu.Framework.Platform;
using osu.Framework.Statistics;
using osu.Framework.Threading;
using OpenTK;

namespace osu.Framework.Input.Handlers.Mouse
{
    internal class OpenTKRawMouseHandler : InputHandler, IHasCursorSensitivity
    {
        private ScheduledDelegate scheduled;

        private OpenTK.Input.MouseState? lastState;

        private Vector2 currentPosition;

        private bool mouseInWindow;

        private readonly BindableDouble sensitivity = new BindableDouble(1) { MinValue = 0.1, MaxValue = 10 };

        public BindableDouble Sensitivity => sensitivity;

        private readonly Bindable<ConfineMouseMode> confineMode = new Bindable<ConfineMouseMode>();

        private readonly Bindable<WindowMode> windowMode = new Bindable<WindowMode>();

        public override bool Initialize(GameHost host)
        {
            host.Window.MouseEnter += window_MouseEnter;
            host.Window.MouseLeave += window_MouseLeave;

            mouseInWindow = host.Window.CursorInWindow;

            // Get the bindables we need to determine whether to confine the mouse to window or not
            DesktopGameWindow desktopWindow = host.Window as DesktopGameWindow;
            if (desktopWindow != null)
            {
                confineMode.BindTo(desktopWindow.ConfineMouseMode);
                windowMode.BindTo(desktopWindow.WindowMode);
            }

            Enabled.ValueChanged += enabled =>
            {
                if (enabled)
                {
                    host.InputThread.Scheduler.Add(scheduled = new ScheduledDelegate(delegate
                    {
                        if (!host.Window.Visible || host.Window.WindowState == WindowState.Minimized)
                            return;

                        bool useRawInput = mouseInWindow && host.Window.Focused;

                        var state = useRawInput ? OpenTK.Input.Mouse.GetState() : OpenTK.Input.Mouse.GetCursorState();

                        if (state.Equals(lastState))
                            return;

                        if (useRawInput)
                        {
                            if (!lastState.HasValue)
                            {
                                // when we return from being outside of the window, we want to set the new position of our game cursor
                                // to where the OS cursor is, just once.
                                var cursorState = OpenTK.Input.Mouse.GetCursorState();
                                var screenPoint = host.Window.PointToClient(new Point(cursorState.X, cursorState.Y));
                                currentPosition = new Vector2(screenPoint.X, screenPoint.Y);
                            }
                            else
                            {
                                currentPosition += new Vector2(state.X - lastState.Value.X, state.Y - lastState.Value.Y) * (float)sensitivity.Value;

                                // When confining, clamp to the window size.
                                if (confineMode.Value == ConfineMouseMode.Always || confineMode.Value == ConfineMouseMode.Fullscreen && windowMode.Value == WindowMode.Fullscreen)
                                    currentPosition = Vector2.Clamp(currentPosition, Vector2.Zero, new Vector2(host.Window.Width, host.Window.Height));

                                // update the windows cursor to match our raw cursor position.
                                // this is important when sensitivity is decreased below 1.0, where we need to ensure the cursor stays withing the window.
                                var screenPoint = host.Window.PointToScreen(new Point((int)currentPosition.X, (int)currentPosition.Y));
                                OpenTK.Input.Mouse.SetPosition(screenPoint.X, screenPoint.Y);
                            }
                        }
                        else
                        {
                            var screenPoint = host.Window.PointToClient(new Point(state.X, state.Y));
                            currentPosition = new Vector2(screenPoint.X, screenPoint.Y);
                        }

                        IMouseState newState;

                        // While not focused, let's silently ignore everything but position.
                        if (host.IsActive)
                        {
                            lastState = state;
                            newState = new OpenTKPollMouseState(state, host.IsActive, currentPosition);
                        }
                        else
                        {
                            lastState = null;
                            newState = new UnfocusedMouseState(new OpenTK.Input.MouseState(), host.IsActive, currentPosition);
                        }

                        PendingStates.Enqueue(new InputState { Mouse = newState });

                        FrameStatistics.Increment(StatisticsCounterType.MouseEvents);
                    }, 0, 0));
                }
                else
                {
                    scheduled?.Cancel();
                    lastState = null;
                }
            };
            Enabled.TriggerChange();
            return true;
        }

        private void window_MouseLeave(object sender, System.EventArgs e) => mouseInWindow = false;

        private void window_MouseEnter(object sender, System.EventArgs e)
        {
            lastState = null;
            mouseInWindow = true;
        }

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
