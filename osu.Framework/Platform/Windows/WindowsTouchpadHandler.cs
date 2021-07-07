// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using osu.Framework.Input;
using osu.Framework.Input.Handlers.Touchpad;
using osu.Framework.Platform.Windows.Native;
using osu.Framework.Logging;
using osuTK;

namespace osu.Framework.Platform.Windows
{
    /// <summary>
    /// A Windows specific touchpad implementation which uses Windows Raw Input API.
    /// </summary>
    internal unsafe class WindowsTouchpadHandler : TouchpadHandler
    {
        private WindowsGameHost host;

        private readonly Dictionary<IntPtr, HidpUtils.TouchpadInfo> devices = new Dictionary<IntPtr, HidpUtils.TouchpadInfo>();

        public override bool IsActive => Enabled.Value;

        public override bool Initialize(GameHost host)
        {
            if (!(host is WindowsGameHost windowsGameHost))
                return false;

            this.host = windowsGameHost;

            Enabled.BindValueChanged(enabled =>
            {
                if (enabled.NewValue)
                    windowsGameHost.OnWndProc += onWndProc;
            }, true);

            return base.Initialize(host);
        }

        private bool isTouchpadRegistered;

        private void onWndProc(IntPtr userData, IntPtr hWnd, uint message, ulong wParam, long lParam, ref IntPtr returnCode)
        {
            if (!Enabled.Value)
            {
                isTouchpadRegistered = false;
                if (host != null) host.OnWndProc -= onWndProc;
                RawInputDevice touchpad = new RawInputDevice(HIDUsagePage.Digitizer, HIDUsage.PrecisionTouchpad, RawInputDeviceFlags.Remove, IntPtr.Zero);

                if (!Native.Input.RegisterRawInputDevices(new[] { touchpad }, 1, sizeof(RawInputDevice)))
                {
                    Native.Input.ThrowLastError("Unable to remove Touchpad as a Raw Input Device!");
                    returnCode = new IntPtr(-1);
                    return;
                }
            }

            // This is supposed to be called during the WM_CREATE message, though it isn't being set to WM_CREATE
            // Probably because it's already been called before the initialisation of this class.

            // This method needs to be called once in the onWndProc method, creating the isTouchpadRegistered.
            if (!isTouchpadRegistered)
            {
                RawInputDevice touchpad = new RawInputDevice(HIDUsagePage.Digitizer, HIDUsage.PrecisionTouchpad, RawInputDeviceFlags.InputSink, hWnd);

                if (!Native.Input.RegisterRawInputDevices(new[] { touchpad }, 1, sizeof(RawInputDevice)))
                {
                    Logger.Log("Unable to register Touchpad as a Raw Input Device!");
                    returnCode = new IntPtr(-1);
                    return;
                }

                isTouchpadRegistered = true;
            }

            if (message != Native.Input.WM_INPUT) return;

            RawInputData data = Native.Input.GetRawInputData(lParam);

            if (data.Header.Type != RawInputType.HID) return;

            if (!devices.TryGetValue(data.Header.Device, out HidpUtils.TouchpadInfo touchpadInfo))
            {
                touchpadInfo = HidpUtils.GetDeviceInfo(data.Header.Device);
                devices.Add(data.Header.Device, touchpadInfo);
            }

            Touch[] touches = HidpUtils.GetTouches(touchpadInfo, data.Hid);

            if (touches.Length == 0) return;

            Touch touch = HidpUtils.MapToScreen(touchpadInfo.Contacts[0].Area, HidpUtils.GetPrimaryTouch(touches));

            // (0,0) vectors are sent for some reason if the finger is lifted,
            if (touch.Position == Vector2.Zero)
                return;

            HandleSingleTouchMove(touch.Position);
        }
    }
}
