// Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System.Collections.Generic;
using System.Drawing;
using osu.Framework.Input.Handlers;
using OpenTK;
using osu.Framework.Platform;
using osu.Framework.Threading;
using OpenTK.Input;

namespace osu.Framework.Desktop.Input.Handlers.Mouse
{
    class OpenTKMouseHandler : InputHandler, ICursorInputHandler
    {
        private BasicGameHost host;

        private MouseState previousState = new MouseState();
        private MouseState state = new MouseState();
        private Vector2 position = Vector2.One;
        private Point nativePosition;
        private int wheelDiff = 0;

        public override void Dispose()
        {
        }
        
        public override bool Initialize(BasicGameHost host)
        {
            this.host = host;

            host.InputScheduler.Add(new ScheduledDelegate(delegate
            {
                try
                {
                    state = OpenTK.Input.Mouse.GetCursorState();
                    nativePosition = host.Window.PointToClient(new Point(state.X, state.Y));
                }
                catch { }
            }, 0, 0));

            return true;
        }

        public override void UpdateInput(bool isActive)
        {
            wheelDiff = state.Wheel - previousState.Wheel;
            var point = host.Window.PointToClient(new Point(state.X, state.Y));
            position = new Vector2(point.X, point.Y);
            previousState = state;
        }

        public void SetPosition(Vector2 pos)
        {
            // no-op
        }

        public Vector2? Position => position;

        public Vector2 Size => host.DrawSize;

        public bool? Left => state.LeftButton == ButtonState.Pressed;

        public bool? Right => state.RightButton == ButtonState.Pressed;

        public bool? Middle => state.MiddleButton == ButtonState.Pressed;

        public bool? Back => state.XButton1 == ButtonState.Pressed;

        public bool? Forward => state.XButton2 == ButtonState.Pressed;

        public bool? WheelUp => wheelDiff > 0;
        public bool? WheelDown => wheelDiff < 0;

        public List<Vector2> IntermediatePositions => new List<Vector2>();

        public bool Clamping { get; set; }

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