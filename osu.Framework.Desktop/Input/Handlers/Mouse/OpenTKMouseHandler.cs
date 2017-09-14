﻿// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System.Drawing;
using osu.Framework.Input;
using osu.Framework.Input.Handlers;
using osu.Framework.Platform;
using osu.Framework.Statistics;
using osu.Framework.Threading;
using OpenTK;
using MouseEventArgs = OpenTK.Input.MouseEventArgs;

namespace osu.Framework.Desktop.Input.Handlers.Mouse
{
    internal class OpenTKMouseHandler : InputHandler
    {
        private OpenTK.Input.MouseState? lastState;
        private GameHost host;

        private bool mouseInWindow;

        private ScheduledDelegate scheduled;

        public override bool Initialize(GameHost host)
        {
            this.host = host;

            host.Window.MouseLeave += (s, e) => mouseInWindow = false;
            host.Window.MouseEnter += (s, e) => mouseInWindow = true;

            mouseInWindow = host.Window.CursorInWindow;

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
                        if (mouseInWindow || !host.Window.Visible) return;

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

        private void handleMouseEvent(object sender, MouseEventArgs e)
        {
            if (!mouseInWindow)
                return;

            if (e.Mouse.X < 0 || e.Mouse.Y < 0)
                // todo: investigate further why we are getting negative values from OpenTK events
                // on windows when crossing centre screen boundaries (width/2 or height/2).
                return;

            handleState(new OpenTKEventMouseState(e.Mouse, host.IsActive, null));
        }

        private void handleState(MouseState state)
        {
            PendingStates.Enqueue(new InputState { Mouse = state });
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
    }
}

