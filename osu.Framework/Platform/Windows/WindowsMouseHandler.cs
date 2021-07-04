// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Drawing;
using osu.Framework.Extensions.EnumExtensions;
using osu.Framework.Input.Handlers.Mouse;
using osu.Framework.Input.StateChanges;
using osu.Framework.Platform.Windows.Native;
using osuTK;

namespace osu.Framework.Platform.Windows
{
    /// <summary>
    /// A windows specific mouse input handler which overrides the SDL2 implementation of raw input.
    /// This is done to better handle quirks of some devices.
    /// </summary>
    internal class WindowsMouseHandler : MouseHandler
    {
        private const int raw_input_coordinate_space = 65535;

        // private SDL.SDL_WindowsMessageHook callback;
        private SDL2DesktopWindow window;

        public override bool IsActive => Enabled.Value;

        public override bool Initialize(GameHost host)
        {
            if (!(host.Window is SDL2DesktopWindow desktopWindow))
                return false;

            window = desktopWindow;

            Enabled.BindValueChanged(enabled =>
            {
                if (!(host is WindowsGameHost windowsGameHost)) return;

                if (enabled.NewValue)
                    windowsGameHost.OnWndProc += onWndProc;
                else
                    windowsGameHost.OnWndProc -= onWndProc;
            }, true);

            return base.Initialize(host);
        }

        protected override void HandleMouseMoveRelative(Vector2 delta)
        {
            // handled via custom logic below.
        }

        private void onWndProc(IntPtr userData, IntPtr hWnd, uint message, ulong wParam, long lParam, ref IntPtr returnCode)
        {
            if (!Enabled.Value) return;

            if (message != Native.Input.WM_INPUT) return;

            RawInputData data = Native.Input.GetRawInputData(lParam);

            if (data.Header.Type != RawInputType.Mouse)
                return;

            var mouse = data.Mouse;

            //TODO: this isn't correct.
            if (mouse.ExtraInformation > 0)
            {
                // i'm not sure if there is a valid case where we need to handle packets with this present
                // but the osu!tablet fires noise events with non-zero values, which we want to ignore.
                // return IntPtr.Zero;
            }

            var position = new Vector2(mouse.LastX, mouse.LastY);
            float sensitivity = (float)Sensitivity.Value;

            if (mouse.Flags.HasFlagFast(RawMouseFlags.MoveAbsolute))
            {
                var screenRect = mouse.Flags.HasFlagFast(RawMouseFlags.VirtualDesktop) ? Native.Input.VirtualScreenRect : new Rectangle(window.Position, window.ClientSize);

                Vector2 screenSize = new Vector2(screenRect.Width, screenRect.Height);

                if (mouse.LastX == 0 && mouse.LastY == 0)
                {
                    // not sure if this is the case for all tablets, but on osu!tablet these can appear and are noise.
                    return;
                }

                // i am not sure what this 64 flag is, but it's set on the osu!tablet at very least.
                // using it here as a method of determining where the coordinate space is incorrect.
                if (((int)mouse.Flags & 64) == 0)
                {
                    position /= raw_input_coordinate_space;
                    position *= screenSize;
                }

                if (Sensitivity.Value != 1)
                {
                    // apply absolute sensitivity adjustment from the centre of the screen area.
                    Vector2 halfScreenSize = (screenSize / 2);

                    position -= halfScreenSize;
                    position *= (float)Sensitivity.Value;
                    position += halfScreenSize;
                }

                // map from screen to client coordinate space.
                // not using Window's PointToClient implementation to keep floating point precision here.
                position -= new Vector2(window.Position.X, window.Position.Y);
                position *= window.Scale;

                PendingInputs.Enqueue(new MousePositionAbsoluteInput { Position = position });
            }
            else
            {
                PendingInputs.Enqueue(new MousePositionRelativeInput { Delta = new Vector2(mouse.LastX, mouse.LastY) * sensitivity });
            }
        }
    }
}
