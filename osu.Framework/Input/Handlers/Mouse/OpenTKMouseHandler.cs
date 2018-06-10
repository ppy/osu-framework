// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System.Drawing;
using osu.Framework.Platform;
using osu.Framework.Threading;
using OpenTK;

namespace osu.Framework.Input.Handlers.Mouse
{
    internal class OpenTKMouseHandler : OpenTKMouseHandlerBase
    {
        private OpenTK.Input.MouseState? lastState;

        private ScheduledDelegate scheduled;

        public override bool Initialize(GameHost host)
        {
            base.Initialize(host);

            Enabled.ValueChanged += enabled =>
            {
                if (enabled)
                {
                    host.Window.MouseMove += handleMouseEvent;
                    host.Window.MouseDown += handleMouseEvent;
                    host.Window.MouseUp += handleMouseEvent;
                    host.Window.MouseWheel += handleMouseEvent;

                    // polling is used to keep a valid mouse position when we aren't receiving events.
                    host.InputThread.Scheduler.Add(scheduled = new ScheduledDelegate(delegate
                    {
                        // we should be getting events if the mouse is inside the window.
                        if (MouseInWindow || !host.Window.Visible || host.Window.WindowState == WindowState.Minimized) return;

                        var state = OpenTK.Input.Mouse.GetCursorState();

                        if (state.Equals(lastState)) return;

                        lastState = state;

                        var mapped = host.Window.PointToClient(new Point(state.X, state.Y));

                        handleState(new OpenTKPollMouseState(state, host.IsActive, new Vector2(mapped.X, mapped.Y)));
                    }, 0, 1000.0 / 60));
                }
                else
                {
                    scheduled?.Cancel();

                    host.Window.MouseMove -= handleMouseEvent;
                    host.Window.MouseDown -= handleMouseEvent;
                    host.Window.MouseUp -= handleMouseEvent;
                    host.Window.MouseWheel -= handleMouseEvent;

                    lastState = null;
                }
            };
            Enabled.TriggerChange();
            return true;
        }

        private void handleMouseEvent(object sender, OpenTK.Input.MouseEventArgs e)
        {
            if (!MouseInWindow)
                return;

            if (e.Mouse.X < 0 || e.Mouse.Y < 0)
                // todo: investigate further why we are getting negative values from OpenTK events
                // on windows when crossing centre screen boundaries (width/2 or height/2).
                return;

            handleState(new OpenTKEventMouseState(e.Mouse, Host.IsActive, null));
        }

        private MouseState lastMouseState;
        private void handleState(OpenTKMouseState state)
        {
            if (lastMouseState != null)
            {
                state.LastPosition = lastMouseState.Position;
                state.LastScroll = lastMouseState.Scroll;
            }
            lastMouseState = state;
            HandleState(state);
        }
    }
}

