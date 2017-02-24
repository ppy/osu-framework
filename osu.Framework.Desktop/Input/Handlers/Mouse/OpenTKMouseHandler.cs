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

namespace osu.Framework.Desktop.Input.Handlers.Mouse
{
    class OpenTKMouseHandler : InputHandler
    {
        private ScheduledDelegate scheduled;

        public override bool Initialize(GameHost host)
        {
            host.InputThread.Scheduler.Add(scheduled = new ScheduledDelegate(delegate
            {
                OpenTK.Input.MouseState state = OpenTK.Input.Mouse.GetCursorState();
                Point point = host.Window.PointToClient(new Point(state.X, state.Y));

                //todo: reimplement if necessary
                //Vector2 pos = Vector2.Multiply(point, Vector2.Divide(host.DrawSize, this.Size));

                Vector2 pos = new Vector2(point.X, point.Y);

                var tkState = new TkMouseState(state, pos, host.IsActive);

                PendingStates.Enqueue(new InputState { Mouse = tkState });
            }, 0, 0));

            return true;
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

        class TkMouseState : MouseState
        {
            public bool WasActive;

            public override int WheelDelta => WasActive ? base.WheelDelta : 0;

            public TkMouseState(OpenTK.Input.MouseState tkState, Vector2 position, bool active)
            {
                WasActive = active;

                if (active)
                {
                    foreach (var b in ButtonStates)
                    {
                        switch (b.Button)
                        {
                            case MouseButton.Left:
                                b.State |= tkState.LeftButton == OpenTK.Input.ButtonState.Pressed;
                                break;
                            case MouseButton.Middle:
                                b.State |= tkState.MiddleButton == OpenTK.Input.ButtonState.Pressed;
                                break;
                            case MouseButton.Right:
                                b.State |= tkState.RightButton == OpenTK.Input.ButtonState.Pressed;
                                break;
                            case MouseButton.Button1:
                                b.State |= tkState.XButton1 == OpenTK.Input.ButtonState.Pressed;
                                break;
                            case MouseButton.Button2:
                                b.State |= tkState.XButton2 == OpenTK.Input.ButtonState.Pressed;
                                break;
                        }
                    }
                }

                Wheel = tkState.Wheel;
                Position = position;
            }
        }
    }
}