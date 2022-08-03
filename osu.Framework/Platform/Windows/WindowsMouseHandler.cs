// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.Drawing;
using osu.Framework.Extensions.EnumExtensions;
using osu.Framework.Input.Handlers.Mouse;
using osu.Framework.Input.StateChanges;
using osu.Framework.Platform.Windows.Native;
using osuTK;
using SDL2;

// ReSharper disable UnusedParameter.Local (Class regularly handles native events where we don't consume all parameters)

namespace osu.Framework.Platform.Windows
{
    /// <summary>
    /// A windows specific mouse input handler which overrides the SDL2 implementation of raw input.
    /// This is done to better handle quirks of some devices.
    /// </summary>
    internal unsafe class WindowsMouseHandler : MouseHandler
    {
        private const int raw_input_coordinate_space = 65535;

        private SDL.SDL_WindowsMessageHook callback;
        private WindowsWindow window;

        public override bool IsActive => Enabled.Value;

        public override bool Initialize(GameHost host)
        {
            if (!(host.Window is WindowsWindow desktopWindow))
                return false;

            window = desktopWindow;
            callback = (ptr, wnd, u, param, l) => onWndProc(ptr, wnd, u, param, l);

            Enabled.BindValueChanged(enabled =>
            {
                host.InputThread.Scheduler.Add(() => SDL.SDL_SetWindowsMessageHook(enabled.NewValue ? callback : null, IntPtr.Zero));
            }, true);

            return base.Initialize(host);
        }

        public override void FeedbackMousePositionChange(Vector2 position, bool isSelfFeedback)
        {
            window.LastMousePosition = position;
            base.FeedbackMousePositionChange(position, isSelfFeedback);
        }

        protected override void HandleMouseMoveRelative(Vector2 delta)
        {
            // handled via custom logic below.
        }

        private IntPtr onWndProc(IntPtr userData, IntPtr hWnd, uint message, ulong wParam, long lParam)
        {
            if (!Enabled.Value)
                return IntPtr.Zero;

            if (message != Native.Input.WM_INPUT)
                return IntPtr.Zero;

            if (Native.Input.IsTouchEvent(Native.Input.GetMessageExtraInfo()))
                // sometimes GetMessageExtraInfo returns 0, so additionally, mouse.ExtraInformation is checked below.
                // touch events are handled by TouchHandler
                return IntPtr.Zero;

            int payloadSize = sizeof(RawInputData);

            Native.Input.GetRawInputData((IntPtr)lParam, RawInputCommand.Input, out var data, ref payloadSize, sizeof(RawInputHeader));

            if (data.Header.Type != RawInputType.Mouse)
                return IntPtr.Zero;

            var mouse = data.Mouse;

            // `ExtraInformation` doens't have the MI_WP_SIGNATURE set, so we have to rely solely on the touch flag.
            if (Native.Input.HasTouchFlag(mouse.ExtraInformation))
                return IntPtr.Zero;

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
                    return IntPtr.Zero;
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

            return IntPtr.Zero;
        }
    }
}
