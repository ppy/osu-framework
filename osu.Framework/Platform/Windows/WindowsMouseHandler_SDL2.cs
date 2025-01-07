// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Drawing;
using osu.Framework.Extensions.EnumExtensions;
using osu.Framework.Input.StateChanges;
using osu.Framework.Platform.Windows.Native;
using osu.Framework.Statistics;
using osuTK;
using static SDL2.SDL;

namespace osu.Framework.Platform.Windows
{
    internal partial class WindowsMouseHandler
    {
        private static readonly GlobalStatistic<ulong> statistic_relative_events = GlobalStatistics.Get<ulong>(StatisticGroupFor<WindowsMouseHandler>(), "Relative events");
        private static readonly GlobalStatistic<ulong> statistic_absolute_events = GlobalStatistics.Get<ulong>(StatisticGroupFor<WindowsMouseHandler>(), "Absolute events");
        private static readonly GlobalStatistic<ulong> statistic_dropped_touch_inputs = GlobalStatistics.Get<ulong>(StatisticGroupFor<WindowsMouseHandler>(), "Dropped native touch inputs");

        private static readonly GlobalStatistic<ulong> statistic_inputs_with_extra_information =
            GlobalStatistics.Get<ulong>(StatisticGroupFor<WindowsMouseHandler>(), "Native inputs with ExtraInformation");

        private const int raw_input_coordinate_space = 65535;

        private SDL_WindowsMessageHook sdl2Callback = null!;

        private void initialiseSDL2(GameHost host)
        {
            // ReSharper disable once ConvertClosureToMethodGroup
            sdl2Callback = (ptr, wnd, u, param, l) => onWndProcSDL2(ptr, wnd, u, param, l);

            Enabled.BindValueChanged(enabled =>
            {
                host.InputThread.Scheduler.Add(() => SDL_SetWindowsMessageHook(enabled.NewValue ? sdl2Callback : null, IntPtr.Zero));
            }, true);
        }

        protected override void HandleMouseMoveRelative(Vector2 delta)
        {
            if (window is SDL2WindowsWindow)
            {
                // on SDL2, relative movement is reported via the WndProc handler below.
                return;
            }

            base.HandleMouseMoveRelative(delta);
        }

        private unsafe IntPtr onWndProcSDL2(IntPtr userData, IntPtr hWnd, uint message, ulong wParam, long lParam)
        {
            if (!Enabled.Value)
                return IntPtr.Zero;

            if (message != Native.Input.WM_INPUT)
                return IntPtr.Zero;

            if (Native.Input.IsTouchEvent(Native.Input.GetMessageExtraInfo()))
            {
                // sometimes GetMessageExtraInfo returns 0, so additionally, mouse.ExtraInformation is checked below.
                // touch events are handled by TouchHandler
                statistic_dropped_touch_inputs.Value++;
                return IntPtr.Zero;
            }

            int payloadSize = sizeof(RawInputData);

#pragma warning disable CA2020 // Prevent behavioral change for IntPtr conversion
            Native.Input.GetRawInputData((IntPtr)lParam, RawInputCommand.Input, out var data, ref payloadSize, sizeof(RawInputHeader));
#pragma warning restore CA2020

            if (data.Header.Type != RawInputType.Mouse)
                return IntPtr.Zero;

            var mouse = data.Mouse;

            // `ExtraInformation` doesn't have the MI_WP_SIGNATURE set, so we have to rely solely on the touch flag.
            if (Native.Input.HasTouchFlag(mouse.ExtraInformation))
            {
                statistic_dropped_touch_inputs.Value++;
                return IntPtr.Zero;
            }

            //TODO: this isn't correct.
            if (mouse.ExtraInformation > 0)
            {
                statistic_inputs_with_extra_information.Value++;

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
                statistic_absolute_events.Value++;
            }
            else
            {
                PendingInputs.Enqueue(new MousePositionRelativeInput { Delta = new Vector2(mouse.LastX, mouse.LastY) * sensitivity });
                statistic_relative_events.Value++;
            }

            return IntPtr.Zero;
        }
    }
}
