// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System.Drawing;
using osu.Framework.Caching;
using osu.Framework.Input;
using osu.Framework.Input.Handlers;
using osu.Framework.Platform;
using osu.Framework.Threading;
using OpenTK;
using OpenTK.Input;
using MouseState = osu.Framework.Input.MouseState;
using osu.Framework.Statistics;

namespace osu.Framework.Desktop.Input.Handlers.Mouse
{
    internal class OpenTKRawMouseHandler : InputHandler
    {
        private ScheduledDelegate scheduled;

        private OpenTK.Input.MouseState lastState;

        private Vector2 currentPosition;

        private bool mouseInWindow;
        private GameHost host;

        private Cached rawOffset = new Cached();

        public float Sensitivity = 1;

        public override bool Initialize(GameHost host)
        {
            this.host = host;

            host.Window.MouseEnter += window_MouseEnter;
            host.Window.MouseLeave += window_MouseLeave;

            updateBindings();
            return true;
        }

        public override bool Enabled
        {
            get
            {
                return base.Enabled;
            }

            set
            {
                if (Enabled == value) return;

                base.Enabled = value;
                updateBindings();
            }
        }

        private void updateBindings()
        {
            if (Enabled)
            {
                host.InputThread.Scheduler.Add(scheduled = new ScheduledDelegate(delegate
                {
                    if (!host.Window.Visible)
                        return;

                    var state = mouseInWindow ? OpenTK.Input.Mouse.GetState() : OpenTK.Input.Mouse.GetCursorState();

                    if (state.Equals(lastState))
                        return;

                    if (mouseInWindow)
                    {
                        if (!rawOffset.IsValid)
                        {
                            rawOffset.Refresh(() =>
                            {
                                var cursorState = OpenTK.Input.Mouse.GetCursorState();
                                var screenPoint = host.Window.PointToClient(new Point(cursorState.X, cursorState.Y));
                                currentPosition = new Vector2(screenPoint.X, screenPoint.Y);
                            });
                        }
                        else
                        {
                            currentPosition += new Vector2(state.X - lastState.X, state.Y - lastState.Y) * Sensitivity;

                            // update the windows cursor to match our raw cursor position
                            var screenPoint = host.Window.PointToScreen(new Point((int)currentPosition.X, (int)currentPosition.Y));
                            OpenTK.Input.Mouse.SetPosition(screenPoint.X, screenPoint.Y);
                        }
                    }
                    else
                    {
                        var screenPoint = host.Window.PointToClient(new Point(state.X, state.Y));
                        currentPosition = new Vector2(screenPoint.X, screenPoint.Y);
                    }

                    lastState = state;


                    // While not focused, let's silently ignore everything but position.
                    if (!host.Window.Focused)
                        state = new OpenTK.Input.MouseState();

                    PendingStates.Enqueue(new InputState { Mouse = new TkMouseState(state, currentPosition, host.IsActive) });

                    FrameStatistics.Increment(StatisticsCounterType.MouseEvents);
                }, 0, 0));
            }
            else
            {
                scheduled?.Cancel();
            }
        }

        private void window_MouseLeave(object sender, System.EventArgs e) => mouseInWindow = false;

        private void window_MouseEnter(object sender, System.EventArgs e)
        {
            rawOffset.Invalidate();
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

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            scheduled.Cancel();
        }

        private class TkMouseState : MouseState
        {
            public readonly bool WasActive;

            public override int WheelDelta => WasActive ? base.WheelDelta : 0;

            public TkMouseState(OpenTK.Input.MouseState tkState, Vector2 position, bool active)
            {
                WasActive = active;

                if (active && tkState.IsAnyButtonDown)
                {
                    addIfPressed(tkState.LeftButton, MouseButton.Left);
                    addIfPressed(tkState.MiddleButton, MouseButton.Middle);
                    addIfPressed(tkState.RightButton, MouseButton.Right);
                    addIfPressed(tkState.XButton1, MouseButton.Button1);
                    addIfPressed(tkState.XButton2, MouseButton.Button2);
                }

                Wheel = tkState.Wheel;
                Position = position;
            }

            private void addIfPressed(ButtonState tkState, MouseButton button)
            {
                if (tkState == ButtonState.Pressed)
                    SetPressed(button, true);
            }
        }
    }
}
