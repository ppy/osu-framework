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
    // todo Consider more than 10 touches, refactor code.
    /// <summary>
    /// A windows specific touchpad input handler which uses Windows Raw Input API.
    /// </summary>
    internal unsafe class WindowsTrackpadHandler : TouchpadHandler
    {
        private const int raw_input_coordinate_space = 65535;

        private WindowsGameHost host;

        private readonly Dictionary<IntPtr, HID.TouchpadInfo> devices = new();

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
                else
                    windowsGameHost.OnWndProc -= onWndProc;
            }, true);

            Logger.Log("Enabled: " + Enabled.Value);

            return base.Initialize(host);
        }

        private bool hasRegistered;

        private void onWndProc(IntPtr userData, IntPtr hWnd, uint message, ulong wParam, long lParam, ref IntPtr returnCode)
        {
            if (!Enabled.Value)
            {
                RawInputDevice touchpad = new RawInputDevice(HIDUsagePage.Digitizer, HIDUsage.PrecisionTouchpad, RawInputDeviceFlags.Remove, hWnd);

                if (!Native.Input.RegisterRawInputDevices(new RawInputDevice[1] { touchpad }, 1, sizeof(RawInputDevice)))
                {
                    Logger.Log("Dank error!");
                    returnCode = new IntPtr(-1);
                    return;
                }

                hasRegistered = false;
                if (host == null)
                    return;

                host.OnWndProc -= onWndProc;
            }

            // This is supposed to be called during WM_CREATE, but it isn't being called for some reason.
            // Possibly because it's already been called.
            if (!hasRegistered)
            {
                hasRegistered = true;
                RawInputDevice touchpad = new RawInputDevice(HIDUsagePage.Digitizer, HIDUsage.PrecisionTouchpad, RawInputDeviceFlags.InputSink, hWnd);

                if (!Native.Input.RegisterRawInputDevices(new RawInputDevice[1] { touchpad }, 1, sizeof(RawInputDevice)))
                {
                    Logger.Log("Dank error!");
                    returnCode = new IntPtr(-1);
                    return;
                }
            }

            if (message != Native.Input.WM_INPUT)
            {
                if (message == Native.Input.WM_CREATE)
                {
                    Logger.Log("Run!!!");
                }

                return;
            }

            RawInputData data = Native.Input.GetRawInputData(lParam);

            if (data.Header.Type != RawInputType.HID) return;

            if (!devices.TryGetValue(data.Header.Device, out HID.TouchpadInfo touchpadInfo))
            {
                touchpadInfo = HID.GetDeviceInfo(data.Header.Device);
                devices.Add(data.Header.Device, touchpadInfo);
                Logger.Log("Epico");
            }

            Touch[] touches = HID.GetContacts(touchpadInfo, data.Hid);

            if (touches.Length == 0) return;

            Logger.Log(
                HID.MapToScreen(touchpadInfo.Contacts[0].Area, HID.GetPrimaryTouch(touches)).Position.ToString());

            // Account for (0,0) vectors

            Touch touch = HID.MapToScreen(touchpadInfo.Contacts[0].Area, HID.GetPrimaryTouch(touches));

            // (0,0) vectors are sent for some reason if the finger is lifted,
            // it's unlikely for a person to get (0,0) anyways.
            if (touch.Position == Vector2.Zero)
                return;

            HandleTouchpadMove(new Vector2[]
            {
                HID.MapToScreen(touchpadInfo.Contacts[0].Area, HID.GetPrimaryTouch(touches)).Position
            });
        }
    }
}
