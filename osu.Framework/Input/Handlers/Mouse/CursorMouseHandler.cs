//Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
//Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using osu.Framework.Graphics.OpenGL;
using OpenTK;
using osu.Framework.Threading;

namespace osu.Framework.Input.Handlers.Mouse
{
    class CursorMouseHandler : InputHandler, ICursorInputHandler
    {
        private bool wasActive = false;
        private Vector2 position = Vector2.One;

        private Game game;

        Point nativePosition;

        public override bool Initialize(Game game)
        {
            this.game = game;

            game.Host.InputScheduler.Add(new ScheduledDelegate(delegate
            {
                nativePosition = game.Window.Form.PointToClient(Cursor.Position);
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

            // This forces a windows cursor position reset which is important for non-raw input mouse to not snap back.
            wasActive = false;
        }

        public Vector2? Position
        {
            get
            {
                return position;
            }
        }

        public Vector2 Size => game.ActualSize;

        public bool? Left
        {
            get
            {
                return null;
            }
        }

        public bool? Right
        {
            get
            {
                return null;
            }
        }

        public bool? Middle
        {
            get
            {
                return null;
            }
        }

        public bool? Back
        {
            get
            {
                return null;
            }
        }

        public bool? Forward
        {
            get
            {
                return null;
            }
        }

        public bool? WheelUp { get; }
        public bool? WheelDown { get; }

        public List<Vector2> IntermediatePositions
        {
            get
            {
                return new List<Vector2>();
            }
        }

        public bool Clamping { get; set; }

        /// <summary>
        /// This input handler is always active, handling the cursor position if no other input handler does.
        /// </summary>
        public override bool IsActive
        {
            get
            {
                return true;
            }
        }

        /// <summary>
        /// Lowest priority. We want the normal mouse handler to only kick in if all other handlers don't do anything.
        /// </summary>
        public override int Priority
        {
            get
            {
                return 0;
            }
        }
    }
}
