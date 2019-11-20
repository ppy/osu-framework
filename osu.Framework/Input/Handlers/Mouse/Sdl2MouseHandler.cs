// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Extensions;
using osu.Framework.Input.StateChanges;
using osu.Framework.Platform;
using osuTK;
using Veldrid;
using Veldrid.Sdl2;
using MouseState = osu.Framework.Input.States.MouseState;

namespace osu.Framework.Input.Handlers.Mouse
{
    public class Sdl2MouseHandler : InputHandler
    {
        private readonly MouseState lastMouseState = new MouseState();
        private readonly MouseState thisMouseState = new MouseState();

        private Window window;

        public override bool Initialize(GameHost host)
        {
            window = host.Window as Window;

            if (window == null)
                return false;

            Enabled.BindValueChanged(e =>
            {
                if (e.NewValue)
                {
                    window.MouseMove += handleMouseMove;
                    window.MouseDown += handleMouseButton;
                    window.MouseUp += handleMouseButton;
                    window.MouseWheel += handleMouseWheel;
                }
                else
                {
                    window.MouseMove -= handleMouseMove;
                    window.MouseDown -= handleMouseButton;
                    window.MouseUp -= handleMouseButton;
                    window.MouseWheel -= handleMouseWheel;
                }
            }, true);

            return true;
        }

        private void handleMouseWheel(MouseWheelEventArgs args)
        {
            PendingInputs.Enqueue(new MouseScrollRelativeInput { Delta = new Vector2(0, args.WheelDelta), IsPrecise = false });
        }

        private void handleMouseButton(MouseEvent evt)
        {
            thisMouseState.SetPressed((osuTK.Input.MouseButton)evt.MouseButton, evt.Down);
            PendingInputs.Enqueue(new MouseButtonInput(thisMouseState.Buttons, lastMouseState.Buttons));
            lastMouseState.SetPressed((osuTK.Input.MouseButton)evt.MouseButton, evt.Down);
        }

        private void handleMouseMove(MouseMoveEventArgs args)
        {
            float scale = window?.Scale ?? 1;
            PendingInputs.Enqueue(new MousePositionAbsoluteInput { Position = args.MousePosition.ToOsuTK() * scale });
        }

        public override bool IsActive => true;

        public override int Priority => 0;
    }
}
