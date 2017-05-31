// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System.Drawing;
using osu.Framework.Input;
using osu.Framework.Input.Handlers;
using osu.Framework.Platform;
using OpenTK;
using OpenTK.Input;
using MouseState = osu.Framework.Input.MouseState;
using osu.Framework.Statistics;
using osu.Framework.Threading;
using MouseEventArgs = OpenTK.Input.MouseEventArgs;

namespace osu.Framework.Desktop.Input.Handlers.Mouse
{
    internal class OpenTKMouseHandler : InputHandler
    {
        private OpenTK.Input.MouseState lastState;
        private GameHost host;

        private bool mouseInWindow;

        private ScheduledDelegate scheduled;

        public override bool Initialize(GameHost host)
        {
            this.host = host;

            host.Window.MouseMove += (s, e) => handleMouseEvent(e);
            host.Window.MouseDown += (s, e) => handleMouseEvent(e);
            host.Window.MouseUp += (s, e) => handleMouseEvent(e);
            host.Window.MouseWheel += (s, e) => handleMouseEvent(e);
            host.Window.MouseLeave += (s, e) => mouseInWindow = false;
            host.Window.MouseEnter += (s, e) => mouseInWindow = true;

            // polling is used to keep a valid mouse position when we aren't receiving events.
            host.InputThread.Scheduler.Add(scheduled = new ScheduledDelegate(delegate
            {
                // we should be getting events if the mouse is inside the window.
                if (mouseInWindow) return;

                var state = OpenTK.Input.Mouse.GetCursorState();
                var mapped = host.Window.PointToClient(new Point(state.X, state.Y));

                handleState(state, mapped);
            }, 0, 1000.0 / 60));

            return true;
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            scheduled.Cancel();
        }

        private void handleMouseEvent(MouseEventArgs e)
        {
            if (!host.Window.Visible || !mouseInWindow)
                return;

            handleState(e.Mouse);
        }

        private void handleState(OpenTK.Input.MouseState state, Point? mappedPosition = null)
        {
            if (state.Equals(lastState)) return;

            lastState = state;

            PendingStates.Enqueue(new InputState { Mouse = new TkMouseState(state, host.IsActive, mappedPosition) });
            FrameStatistics.Increment(StatisticsCounterType.MouseEvents);
        }

        /// <summary>
        /// This input handler is always active, handling the cursor position if no other input handler does.
        /// </summary>
        public override bool IsActive => true;

        /// <summary>
        /// Lowest priority. We want the normal mouse handler to only kick in if all other handlers don't do anything.
        /// </summary>
        public override int Priority => 0;

        private class TkMouseState : MouseState
        {
            public readonly bool WasActive;

            public override int WheelDelta => WasActive ? base.WheelDelta : 0;

            public TkMouseState(OpenTK.Input.MouseState tkState, bool active, Point? mappedPosition)
            {
                WasActive = active;

                // While not focused, let's silently ignore everything but position.
                if (active && tkState.IsAnyButtonDown)
                {
                    addIfPressed(tkState.LeftButton, MouseButton.Left);
                    addIfPressed(tkState.MiddleButton, MouseButton.Middle);
                    addIfPressed(tkState.RightButton, MouseButton.Right);
                    addIfPressed(tkState.XButton1, MouseButton.Button1);
                    addIfPressed(tkState.XButton2, MouseButton.Button2);
                }

                Wheel = tkState.Wheel;
                Position = new Vector2(mappedPosition?.X ?? tkState.X, mappedPosition?.Y ?? tkState.Y);
            }

            private void addIfPressed(ButtonState tkState, MouseButton button)
            {
                if (tkState == ButtonState.Pressed)
                    SetPressed(button, true);
            }
        }
    }
}

