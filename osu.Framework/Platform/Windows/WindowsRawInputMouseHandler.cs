using System;
using System.Drawing;
using osu.Framework.Bindables;
using osu.Framework.Extensions.EnumExtensions;
using osu.Framework.Input.Handlers;
using osu.Framework.Input.StateChanges;
using osu.Framework.Platform.Windows.Native;
using osuTK;
using SDL2;

namespace osu.Framework.Platform.Windows
{
    /// <summary>
    /// A windows specific mouse input handler which overrides the SDL2 implementation of raw input.
    /// This is done to better handle quirks of some devices.
    /// </summary>
    internal unsafe class WindowsRawInputMouseHandler : InputHandler, IHasCursorSensitivity
    {
        public BindableDouble Sensitivity { get; } = new BindableDouble(1) { MinValue = 0.1, MaxValue = 10 };

        private const int raw_input_coordinate_space = 65536;

        private SDL.SDL_WindowsMessageHook callback;
        private SDL2DesktopWindow window;

        public override bool IsActive => Enabled.Value;
        public override int Priority => 0;

        public override bool Initialize(GameHost host)
        {
            if (!(host.Window is SDL2DesktopWindow desktopWindow))
                return false;

            window = desktopWindow;
            callback = (ptr, wnd, u, param, l) => onWndProc(ptr, wnd, u, param, l);

            try
            {
                // this is already done by SDL2 in general, but let's register just for safety.
                var registration = new RawInputDevice
                {
                    UsagePage = HIDUsagePage.Generic,
                    Usage = HIDUsage.Mouse,
                    Flags = RawInputDeviceFlags.None,
                    WindowHandle = desktopWindow.WindowHandle
                };

                if (!Native.Input.RegisterRawInputDevices(new[] { registration }, 1, sizeof(RawInputDevice)))
                    return false;
            }
            catch
            {
                return false;
            }

            Enabled.BindValueChanged(enabled =>
            {
                host.InputThread.Scheduler.Add(() => SDL.SDL_SetWindowsMessageHook(enabled.NewValue ? callback : null, IntPtr.Zero));
            }, true);

            return true;
        }

        private IntPtr onWndProc(IntPtr userData, IntPtr hWnd, uint message, ulong wParam, long lParam)
        {
            if (!Enabled.Value)
                return IntPtr.Zero;

            if (message != Native.Input.WM_INPUT)
                return IntPtr.Zero;

            int payloadSize = sizeof(RawInputData);

            Native.Input.GetRawInputData((IntPtr)lParam, RawInputCommand.Input, out var data, ref payloadSize, sizeof(RawInputHeader));

            if (data.Header.Type != RawInputType.Mouse)
                return IntPtr.Zero;

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
