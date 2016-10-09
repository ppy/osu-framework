// Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using osu.Framework.Input.Handlers;
using osu.Framework.Threading;
using OpenTK;
using osu.Framework.Platform;

namespace osu.Framework.Desktop.Input.Handlers.Mouse
{
    class CursorMouseHandler : InputHandler, ICursorInputHandler
    {
        private Vector2 position = Vector2.One;

        Point nativePosition;

        BasicGameHost host;

        public override bool Initialize(BasicGameHost host)
        {
            this.host = host;
            host.InputScheduler.Add(new ScheduledDelegate(delegate
            {
                nativePosition = host.Window.Form.PointToClient(Cursor.Position);
            }, 0, 0));

            return true;
        }


        public override void Dispose()
        {
        }

        public override void UpdateInput(bool isActive)
        {
            Point nativeMousePosition = nativePosition;

            position.X = nativeMousePosition.X;
            position.Y = nativeMousePosition.Y;
        }

        public void SetPosition(Vector2 pos)
        {
            position = pos;
        }

        public Vector2? Position => position;

        public Vector2 Size => host.Size;

        public bool? Left => null;

        public bool? Right => null;

        public bool? Middle => null;

        public bool? Back => null;

        public bool? Forward => null;

        public bool? WheelUp => null;
        public bool? WheelDown => null;

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
