// Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System.Collections.Generic;
using System.Windows.Forms;
using OpenTK;

namespace osu.Framework.Input.Handlers.Mouse
{
    class FormMouseHandler : InputHandler, ICursorInputHandler
    {
        private Form form;
        private int pendingWheel;

        private void mouseWheel(object sender, MouseEventArgs e)
        {
            pendingWheel = e.Delta;
        }

        private void mouseUp(object sender, MouseEventArgs e)
        {
            switch (e.Button)
            {
                case MouseButtons.Left:
                    Left = false;
                    break;
                case MouseButtons.Middle:
                    Middle = false;
                    break;
                case MouseButtons.Right:
                    Right = false;
                    break;
                case MouseButtons.XButton1:
                    Back = false;
                    break;
                case MouseButtons.XButton2:
                    Forward = false;
                    break;
            }
        }

        private void mouseDown(object sender, MouseEventArgs e)
        {
            switch (e.Button)
            {
                case MouseButtons.Left:
                    Left = true;
                    break;
                case MouseButtons.Middle:
                    Middle = true;
                    break;
                case MouseButtons.Right:
                    Right = true;
                    break;
                case MouseButtons.XButton1:
                    Back = true;
                    break;
                case MouseButtons.XButton2:
                    Forward = true;
                    break;
            }
        }

        public override bool Initialize(Game game)
        {
            form = game.Window.Form;
            form.MouseDown += mouseDown;
            form.MouseUp += mouseUp;
            form.MouseWheel += mouseWheel;
            return true;
        }

        public override void Dispose()
        {
            form.MouseDown -= mouseDown;
            form.MouseUp -= mouseUp;
            form.MouseWheel -= mouseWheel;
        }

        public override void UpdateInput(bool isActive)
        {
            WheelDown = pendingWheel < 0;
            WheelUp = pendingWheel > 0;
            pendingWheel = 0;
        }

        public override bool IsActive => true;
        public override int Priority => 0;

        public void SetPosition(Vector2 pos)
        {
        }

        public Vector2? Position { get; set; }
        public Vector2 Size { get; set; }
        public bool? Left { get; private set; }
        public bool? Right { get; private set; }
        public bool? Middle { get; private set; }
        public bool? Back { get; private set; }
        public bool? Forward { get; private set; }
        public bool? WheelUp { get; private set; }
        public bool? WheelDown { get; private set; }
        public List<Vector2> IntermediatePositions => null;
        public bool Clamping { get; set; }
    }
}
