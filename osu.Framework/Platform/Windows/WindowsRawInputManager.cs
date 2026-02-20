// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using osu.Framework.Logging;
using osu.Framework.Platform.Windows.Native;

namespace osu.Framework.Platform.Windows
{
    public class WindowsRawInputManager
    {
        /// <summary>Fired when raw mouse input is detected in <see cref="ProcessWmInput"/>.</summary>
        public event Action<RawInputData>? RawMouse
        {
            add
            {
                if (rawMouse == null)
                    register(HIDUsagePage.Generic, HIDUsage.Mouse, true);
                rawMouse += value;
            }
            remove
            {
                rawMouse -= value;
                if (rawMouse == null)
                    register(HIDUsagePage.Generic, HIDUsage.Mouse, false);
            }
        }

        public event Action<RawInputDataHidHeader, List<byte[]>>? RawTouchpad
        {
            add
            {
                if (rawTouchpad == null)
                    register(HIDUsagePage.Digitizer, HIDUsage.TouchPad, true);
                rawTouchpad += value;
            }
            remove
            {
                rawTouchpad -= value;
                if (rawTouchpad == null)
                    register(HIDUsagePage.Digitizer, HIDUsage.TouchPad, false);
            }
        }

        private event Action<RawInputData>? rawMouse;
        private event Action<RawInputDataHidHeader, List<byte[]>>? rawTouchpad;

        /// <summary>The HWND associated with the manager. Used on registering.</summary>
        public readonly IntPtr WindowHandle;

        public WindowsRawInputManager(IntPtr windowHandle)
        {
            WindowHandle = windowHandle;
        }

        /// <summary>
        /// Register a HID device to use with raw input.
        /// </summary>
        /// <param name="usagePage">Usage page of the HID device.</param>
        /// <param name="usage">Usage of the HID device (meaning of the value depend on the page).</param>
        /// <param name="enable">Register (true) or unregister (false).</param>
        private unsafe void register(HIDUsagePage usagePage, HIDUsage usage, bool enable)
        {
            var registration = new RawInputDevice
            {
                UsagePage = usagePage,
                Usage = usage,
                Flags = enable ? RawInputDeviceFlags.None : RawInputDeviceFlags.Remove,
                WindowHandle = WindowHandle
            };

            bool r = Native.Input.RegisterRawInputDevices([registration], 1, sizeof(RawInputDevice));

            if (!r)
            {
                Logger.Log($"RegisterRawInputDevices failed ({Marshal.GetLastWin32Error()}): touchpad reading not possible",
                    LoggingTarget.Input, LogLevel.Error);
            }
        }

        /// <summary>
        /// Process the WM_INPUT message. Only lParam is needed.
        /// </summary>
        /// <param name="lParam">A HRAWINPUT handle to the RAWINPUT structure that contains the raw input from the device.</param>
        public unsafe void ProcessWmInput(IntPtr lParam)
        {
            int size = 0;
            Native.Input.GetRawInputData(lParam, RawInputCommand.Input, IntPtr.Zero, ref size, sizeof(RawInputHeader));
            IntPtr buffer = Marshal.AllocHGlobal(size);

            try
            {
                if (Native.Input.GetRawInputData(lParam, RawInputCommand.Input, buffer, ref size, sizeof(RawInputHeader)) < 0)
                {
                    Logger.Log($"GetRawInputData failed ({Marshal.GetLastWin32Error()})", LoggingTarget.Input, LogLevel.Error);
                    return;
                }

                RawInputType rawInputType = Marshal.PtrToStructure<RawInputHeader>(buffer).Type;

                switch (rawInputType)
                {
                    case RawInputType.Mouse:
                    {
                        if (size < sizeof(RawInputData))
                        {
                            Logger.Log($"Raw mouse buffer too small ({size} < {sizeof(RawInputData)})", LoggingTarget.Input, LogLevel.Error);
                            return;
                        }

                        rawMouse?.Invoke(Marshal.PtrToStructure<RawInputData>(buffer));
                        break;
                    }

                    case RawInputType.HID:
                    {
                        if (size < sizeof(RawInputDataHidHeader))
                        {
                            Logger.Log($"Raw HID header buffer too small ({size} < {sizeof(RawInputDataHidHeader)})", LoggingTarget.Input, LogLevel.Error);
                            return;
                        }

                        var hidHeader = Marshal.PtrToStructure<RawInputDataHidHeader>(buffer);

                        if (size < sizeof(RawInputDataHidHeader) + hidHeader.SizeHid * hidHeader.Count)
                        {
                            Logger.Log($"Raw HID data buffer too small ({size} < {sizeof(RawInputDataHidHeader)} + {hidHeader.SizeHid} * {hidHeader.Count})", LoggingTarget.Input, LogLevel.Error);
                            return;
                        }

                        List<byte[]> buffers = new List<byte[]>();

                        for (int i = 0; i < hidHeader.Count; i++)
                        {
                            IntPtr dataPtr = IntPtr.Add(buffer, sizeof(RawInputDataHidHeader) + hidHeader.SizeHid * i);
                            byte[] report = new byte[hidHeader.SizeHid];
                            Marshal.Copy(dataPtr, report, 0, hidHeader.SizeHid);
                            buffers.Add(report);
                        }

                        // TODO maybe invoke one by one rather than as a `List`?
                        rawTouchpad?.Invoke(hidHeader, buffers);
                        break;
                    }

                    case RawInputType.Keyboard:
                    default:
                        break;
                }
            }
            finally
            {
                Marshal.FreeHGlobal(buffer);
            }
        }
    }
}
