using System;
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
    internal unsafe class WindowsRawInputMouseHandler : InputHandler
    {
        private const int raw_input_coordinate_space = 65536;
        private SDL.SDL_WindowsMessageHook callback;
        private SDL2DesktopWindow window;
        private Vector2? lastRelativePosition;
        private IntPtr lastRelativeDevice;

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

            var position = new Vector2(mouse.LastX, mouse.LastY);

            //TODO: this isn't correct.
            if (mouse.ExtraInformation > 0)
            {
                // i'm not sure if there is a valid case where we need to handle packets with this present
                // but the osu!tablet fires noise events with non-zero values, which we want to ignore.
                return IntPtr.Zero;
            }

            // i am not sure what this 64 flag is, but it's set on the osu!tablet at very least.
            // using it here as a method of determining where the coordinate space is incorrect.
            if (((int)mouse.Flags & 64) > 0)
            {
                // tablets that provide raw input in screen space instead of 0..65536
                PendingInputs.Enqueue(new MousePositionAbsoluteInput { Position = position });
            }
            else
            {
                position /= raw_input_coordinate_space;
                position = new Vector2(position.X * window.ClientSize.Width, position.Y * window.ClientSize.Height);

                if (mouse.Flags.HasFlagFast(RawMouseFlags.MoveAbsolute))
                {
                    PendingInputs.Enqueue(new MousePositionAbsoluteInput { Position = position });
                }
                else
                {
                    if (lastRelativeDevice != data.Header.Device)
                    {
                        // if the relative data is coming from a new device, forget the last coordinate.
                        lastRelativePosition = null;
                        lastRelativeDevice = data.Header.Device;
                    }

                    lastRelativePosition ??= position;
                    PendingInputs.Enqueue(new MousePositionRelativeInput { Delta = new Vector2(mouse.LastX - lastRelativePosition.Value.X, mouse.LastY - lastRelativePosition.Value.Y) });
                }
            }

            return IntPtr.Zero;
        }
    }
}
