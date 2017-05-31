// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System.Drawing;
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
    internal class OpenTKMouseHandler : InputHandler
    {
        private ScheduledDelegate scheduled;

        private OpenTK.Input.MouseState lastState;

        private Size rawOffset;
        private OpenTK.Input.MouseState lastScrenState;
        private bool requireRawTare;
        private bool mouseInWindow;

        public override bool Initialize(GameHost host)
        {
            host.Window.MouseEnter += window_MouseEnter;
            host.Window.MouseLeave += window_MouseLeave;
            host.Window.MouseMove += window_MouseMove;

            host.InputThread.Scheduler.Add(scheduled = new ScheduledDelegate(delegate
            {
                if (!host.Window.Visible)
                    return;

                var state = mouseInWindow ? OpenTK.Input.Mouse.GetState() : lastScrenState;

                if (state.Equals(lastState))
                    return;

                lastState = state;

                Vector2 pos;

                if (mouseInWindow)
                {
                    Point point = new Point(state.X, state.Y);

                    if (requireRawTare)
                    {
                        rawOffset = new Size(lastScrenState.X - point.X, lastScrenState.Y - point.Y);
                        requireRawTare = false;
                    }

                    point += rawOffset;

                    pos = new Vector2(point.X, point.Y);

                    // update the windows cursor to match our raw cursor position
                    var screenPoint = host.Window.PointToScreen(point);
                    OpenTK.Input.Mouse.SetPosition(screenPoint.X, screenPoint.Y);

                }
                else
                {
                    pos = new Vector2(lastScrenState.X, lastScrenState.Y);
                }

                // While not focused, let's silently ignore everything but position.
                if (!host.Window.Focused)
                    state = new OpenTK.Input.MouseState();

                PendingStates.Enqueue(new InputState { Mouse = new TkMouseState(state, pos, host.IsActive) });

                FrameStatistics.Increment(StatisticsCounterType.MouseEvents);
            }, 0, 0));

            return true;
        }

        private void window_MouseLeave(object sender, System.EventArgs e) => mouseInWindow = false;

        private void window_MouseMove(object sender, MouseMoveEventArgs e) => lastScrenState = e.Mouse;

        private void window_MouseEnter(object sender, System.EventArgs e)
        {
            requireRawTare = true;
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
