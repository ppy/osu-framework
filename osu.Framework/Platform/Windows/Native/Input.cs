// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Drawing;
using System.Runtime.InteropServices;

namespace osu.Framework.Platform.Windows.Native
{
    internal static class Input
    {
        [DllImport("user32.dll")]
        public static extern bool RegisterRawInputDevices([MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)]
                                                          RawInputDevice[] pRawInputDevices, int uiNumDevices, int cbSize);

        [DllImport("user32.dll")]
        public static extern int GetRawInputData(IntPtr hRawInput, RawInputCommand uiCommand, out RawInputData pData, ref uint pcbSize, uint cbSizeHeader);

        [DllImport("user32.dll")]
        public static extern int GetRawInputData(IntPtr hRawInput, RawInputCommand uiCommand, IntPtr pData, ref uint pcbSize, uint cbSizeHeader);

        // There are 2, i believe one is for unicode?
        [DllImport("user32.dll")]
        public static extern int GetRawInputDeviceInfoW(IntPtr handle, uint uiCommand, byte[] pData, ref uint pcbSize);

        [DllImport("user32.dll")]
        public static extern int GetRawInputDeviceInfoW(IntPtr handle, uint uiCommand, IntPtr pData, ref uint pcbSize);

        [DllImport("user32.dll")]
        public static extern int GetRawInputDeviceInfoW(IntPtr handle, uint uiCommand, ref byte[] pData, ref uint pcbSize);

        [DllImport("Hid.dll")]
        public static extern uint HidP_MaxUsageListLength(HidpReportType reportType, HIDUsagePage usagePage, byte[] preparsedData);

        [DllImport("Hid.dll")]
        public static extern NSStatus HidP_GetUsages(HidpReportType reportType, HIDUsagePage usagePage, ushort linkCollection, ushort[] usages, ref uint usageLength, byte[] preparsedData, byte[] report, int reportLength);

        [DllImport("Hid.dll")]
        public static extern NSStatus HidP_GetUsageValue(HidpReportType reportType, HIDUsagePage usagePage, ushort linkCollection, HIDUsage usage, out uint usageValue, byte[] preparsedData, byte[] report, int reportLength);

        [DllImport("Hid.dll")]
        public static extern NSStatus HidP_GetScaledUsageValue(HidpReportType reportType, HIDUsagePage usagePage, ushort linkCollection, HIDUsage usage, out int usageValue, byte[] preparsedData, byte[] report, int reportLength);

        [DllImport("Hid.dll")]
        public static extern NSStatus HidP_GetCaps(byte[] preparsedData, out HidpCaps capabilities);

        [DllImport("Hid.dll")]
        public static extern NSStatus HidP_GetValueCaps(HidpReportType reportType, byte[] valueCaps, ref ushort valueCapsLength, byte[] preparsedData);

        [DllImport("Hid.dll")]
        public static extern NSStatus HidP_GetButtonCaps(HidpReportType reportType, byte[] valueCaps, ref ushort valueCapsLength, byte[] preparsedData);

        [DllImport("Kernel32.dll", CharSet = CharSet.Auto)]
        public static extern uint GetLastError();

        [DllImport("Kernel32.dll", CharSet = CharSet.Auto)]
        public static extern uint FormatMessage(FormatFlags dwFlags, IntPtr lpSource, uint dwMessageId, uint dwLanguageId, out string lpBuffer, uint nSize);

        [DllImport("Kernel32.dll", CharSet = CharSet.Auto)]
        private static extern ushort GetUserDefaultUILanguage();

        public static void ThrowLastError(string message)
        {
            uint errorCode = GetLastError();
            FormatMessage(FormatFlags.FORMAT_MESSAGE_ALLOCATE_BUFFER | FormatFlags.FORMAT_MESSAGE_FROM_SYSTEM | FormatFlags.FORMAT_MESSAGE_IGNORE_INSERTS, (IntPtr)null, errorCode,
                GetUserDefaultUILanguage(), out var data, 0);
            throw new NativeException($"{message}\nError Code: {errorCode} - {data}".Replace("\n", ""));
        }

        public static unsafe RawInputData GetRawInputData(long lParam)
        {
            uint payloadSize = 0;
            int statusCode = GetRawInputData((IntPtr)lParam, RawInputCommand.Input, (IntPtr)null, ref payloadSize, (uint)sizeof(RawInputHeader));
            if (statusCode == -1)
                ThrowLastError("Unable to get Raw Input Data");
            var bytes = new byte[payloadSize];

            fixed (byte* bytesPtr = bytes)
            {
                statusCode = GetRawInputData((IntPtr)lParam, RawInputCommand.Input, (IntPtr)bytesPtr, ref payloadSize, (uint)sizeof(RawInputHeader));
                if (statusCode == -1)
                    ThrowLastError("Unable to get Raw Input Data");
                return RawInputData.FromPointer(bytesPtr);
            }
        }

        internal static Rectangle VirtualScreenRect => new Rectangle(
            GetSystemMetrics(SM_XVIRTUALSCREEN),
            GetSystemMetrics(SM_YVIRTUALSCREEN),
            GetSystemMetrics(SM_CXVIRTUALSCREEN),
            GetSystemMetrics(SM_CYVIRTUALSCREEN));

        internal const int WM_CREATE = 0x0001;

        internal const int WM_INPUT = 0x00FF;

        [DllImport("user32.dll")]
        public static extern int GetSystemMetrics(int nIndex);

        internal static Rectangle GetVirtualScreenRect() => new Rectangle(
            GetSystemMetrics(SM_XVIRTUALSCREEN),
            GetSystemMetrics(SM_YVIRTUALSCREEN),
            GetSystemMetrics(SM_CXVIRTUALSCREEN),
            GetSystemMetrics(SM_CYVIRTUALSCREEN)
        );

        public const int SM_XVIRTUALSCREEN = 76;
        public const int SM_YVIRTUALSCREEN = 77;
        public const int SM_CXVIRTUALSCREEN = 78;
        public const int SM_CYVIRTUALSCREEN = 79;
    }

    /// <summary>
    /// Value type for raw input.
    /// </summary>
    public struct RawInputData
    {
        /// <summary>Header for the data.</summary>
        public RawInputHeader Header;

        /// <summary>Mouse raw input data.</summary>
        public RawMouse Mouse;

        public RawKeyboard Keyboard;

        public RawHID Hid;

        internal static unsafe RawInputData FromPointer(byte* ptr)
        {
            // Since RawHid cannot simply be casted (because of the RawData property),
            // it renders the RawInputData unable to be casted
            // We are able to cast the RawMouse and RawKeyboard by adding the size of the RawInputHeader to the pointer.
            // and also call the FromPointer method in Hid case.
            var result = new RawInputData
            {
                Header = *((RawInputHeader*)ptr)
            };

            switch (result.Header.Type)
            {
                case RawInputType.Mouse:
                    result.Mouse = *((RawMouse*)(ptr + sizeof(RawInputHeader)));
                    break;

                case RawInputType.Keyboard:
                    result.Keyboard = *((RawKeyboard*)(ptr + sizeof(RawInputHeader)));
                    break;

                case RawInputType.HID:
                    result.Hid = RawHID.FromPointer(ptr + sizeof(RawInputHeader));
                    break;
            }

            return result;
        }

        // This struct is a lot larger but the remaining elements have been omitted until required (Keyboard / HID / Touch).
    }

    /// <summary>
    /// Contains information about the state of the mouse.
    /// </summary>
    public struct RawMouse
    {
        /// <summary>
        /// The mouse state.
        /// </summary>
        public RawMouseFlags Flags;

        /// <summary>
        /// Flags for the event.
        /// </summary>
        public RawMouseButtons ButtonFlags;

        /// <summary>
        /// If the mouse wheel is moved, this will contain the delta amount.
        /// </summary>
        public short ButtonData;

        /// <summary>
        /// Raw button data.
        /// </summary>
        public uint RawButtons;

        /// <summary>
        /// The motion in the X direction. This is signed relative motion or
        /// absolute motion, depending on the value of usFlags.
        /// </summary>
        public int LastX;

        /// <summary>
        /// The motion in the Y direction. This is signed relative motion or absolute motion,
        /// depending on the value of usFlags.
        /// </summary>
        public int LastY;

        /// <summary>
        /// The device-specific additional information for the event.
        /// </summary>
        public uint ExtraInformation;
    }

    /// <summary>
    /// Enumeration containing the flags for raw mouse data.
    /// </summary>
    [Flags]
    public enum RawMouseFlags : ushort
    {
        /// <summary>Relative to the last position.</summary>
        MoveRelative = 0,

        /// <summary>Absolute positioning.</summary>
        MoveAbsolute = 1,

        /// <summary>Coordinate data is mapped to a virtual desktop.</summary>
        VirtualDesktop = 2,

        /// <summary>Attributes for the mouse have changed.</summary>
        AttributesChanged = 4,

        /// <summary>WM_MOUSEMOVE and WM_INPUT don't coalesce</summary>
        MoveNoCoalesce = 8,
    }

    /// <summary>
    /// Enumeration containing the button data for raw mouse input.
    /// </summary>
    public enum RawMouseButtons : ushort
    {
        /// <summary>No button.</summary>
        None = 0,

        /// <summary>Left (button 1) down.</summary>
        LeftDown = 0x0001,

        /// <summary>Left (button 1) up.</summary>
        LeftUp = 0x0002,

        /// <summary>Right (button 2) down.</summary>
        RightDown = 0x0004,

        /// <summary>Right (button 2) up.</summary>
        RightUp = 0x0008,

        /// <summary>Middle (button 3) down.</summary>
        MiddleDown = 0x0010,

        /// <summary>Middle (button 3) up.</summary>
        MiddleUp = 0x0020,

        /// <summary>Button 4 down.</summary>
        Button4Down = 0x0040,

        /// <summary>Button 4 up.</summary>
        Button4Up = 0x0080,

        /// <summary>Button 5 down.</summary>
        Button5Down = 0x0100,

        /// <summary>Button 5 up.</summary>
        Button5Up = 0x0200,

        /// <summary>Mouse wheel moved.</summary>
        MouseWheel = 0x0400
    }

    public struct RawKeyboard
    {
        public ushort MakeCode;

        public RawKeyboardFlags Flags;

        public ushort Reserved;

        // I'm sure there is a vkey table in this project.
        public ushort VKey;

        public uint Message;

        public ulong ExtraInformation;
    }

    [Flags]
    public enum RawKeyboardFlags : ushort
    {
        KeyMake = 0x0,
        KeyBreak = 0x1,
        KeyE0 = 0x2,
        KeyE1 = 0x4
    }

    public unsafe struct RawHID
    {
        public int DwSizeHid;

        public int DwCount;

        public byte[] RawData;

        internal static RawHID FromPointer(byte* ptr)
        {
            // Since RawData is not a fixed array and the size depends on DwCount and DwSizeHid,
            // we have to create the array in a function and copy the data from a pointer.
            var result = new RawHID();
            var intPtr = (int*)ptr;

            result.DwSizeHid = intPtr[0];
            result.DwCount = intPtr[1];
            result.RawData = new byte[result.DwSizeHid * result.DwCount];
            Marshal.Copy(new IntPtr(&intPtr[2]), result.RawData, 0, result.RawData.Length);

            return result;
        }
    }

    /// <summary>
    /// Enumeration contanining the command types to issue.
    /// </summary>
    public enum RawInputCommand
    {
        /// <summary>
        /// Get input data.
        /// </summary>
        Input = 0x10000003,

        /// <summary>
        /// Get header data.
        /// </summary>
        Header = 0x10000005
    }

    /// <summary>
    /// Enumeration containing the type device the raw input is coming from.
    /// </summary>
    public enum RawInputType
    {
        /// <summary>
        /// Mouse input.
        /// </summary>
        Mouse = 0,

        /// <summary>
        /// Keyboard input.
        /// </summary>
        Keyboard = 1,

        /// <summary>
        /// Another device that is not the keyboard or the mouse.
        /// </summary>
        HID = 2
    }

    /// <summary>
    /// Value type for a raw input header.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct RawInputHeader
    {
        /// <summary>Type of device the input is coming from.</summary>
        public RawInputType Type;

        /// <summary>Size of the packet of data.</summary>
        public int Size;

        /// <summary>Handle to the device sending the data.</summary>
        public IntPtr Device;

        /// <summary>wParam from the window message.</summary>
        public IntPtr wParam;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct RawInputDevice
    {
        /// <summary>Top level collection Usage page for the raw input device.</summary>
        public HIDUsagePage UsagePage;

        /// <summary>Top level collection Usage for the raw input device. </summary>
        public HIDUsage Usage;

        /// <summary>Mode flag that specifies how to interpret the information provided by UsagePage and Usage.</summary>
        public RawInputDeviceFlags Flags;

        /// <summary>Handle to the target device. If NULL, it follows the keyboard focus.</summary>
        public IntPtr WindowHandle;

        public RawInputDevice(HIDUsagePage usagePage, HIDUsage usage, RawInputDeviceFlags flags, IntPtr windowsHandle)
        {
            UsagePage = usagePage;
            Usage = usage;
            Flags = flags;
            WindowHandle = windowsHandle;
        }
    }

    /// <summary>Enumeration containing flags for a raw input device.</summary>
    [Flags]
    public enum RawInputDeviceFlags
    {
        /// <summary>No flags.</summary>
        None = 0,

        /// <summary>If set, this removes the top level collection from the inclusion list. This tells the operating system to stop reading from a device which matches the top level collection.</summary>
        Remove = 0x00000001,

        /// <summary>If set, this specifies the top level collections to exclude when reading a complete usage page. This flag only affects a TLC whose usage page is already specified with PageOnly.</summary>
        Exclude = 0x00000010,

        /// <summary>If set, this specifies all devices whose top level collection is from the specified usUsagePage. Note that Usage must be zero. To exclude a particular top level collection, use Exclude.</summary>
        PageOnly = 0x00000020,

        /// <summary>If set, this prevents any devices specified by UsagePage or Usage from generating legacy messages. This is only for the mouse and keyboard.</summary>
        NoLegacy = 0x00000030,

        /// <summary>If set, this enables the caller to receive the input even when the caller is not in the foreground. Note that WindowHandle must be specified.</summary>
        InputSink = 0x00000100,

        /// <summary>If set, the mouse button click does not activate the other window.</summary>
        CaptureMouse = 0x00000200,

        /// <summary>If set, the application-defined keyboard device hotkeys are not handled. However, the system hotkeys; for example, ALT+TAB and CTRL+ALT+DEL, are still handled. By default, all keyboard hotkeys are handled. NoHotKeys can be specified even if NoLegacy is not specified and WindowHandle is NULL.</summary>
        NoHotKeys = 0x00000200,

        /// <summary>If set, application keys are handled.  NoLegacy must be specified.  Keyboard only.</summary>
        AppKeys = 0x00000400
    }

    internal enum HidpReportType
    {
        HidP_Input,
        HidP_Output,
        HidP_Feature
    }

    public enum HIDUsagePage : ushort
    {
        Undefined = 0x00,
        Generic = 0x01,
        Simulation = 0x02,
        VR = 0x03,
        Sport = 0x04,
        Game = 0x05,
        Keyboard = 0x07,
        LED = 0x08,
        Button = 0x09,
        Ordinal = 0x0A,
        Telephony = 0x0B,
        Consumer = 0x0C,
        Digitizer = 0x0D,
        PID = 0x0F,
        Unicode = 0x10,
        AlphaNumeric = 0x14,
        Medical = 0x40,
        MonitorPage0 = 0x80,
        MonitorPage1 = 0x81,
        MonitorPage2 = 0x82,
        MonitorPage3 = 0x83,
        PowerPage0 = 0x84,
        PowerPage1 = 0x85,
        PowerPage2 = 0x86,
        PowerPage3 = 0x87,
        BarCode = 0x8C,
        Scale = 0x8D,
        MSR = 0x8E
    }

    public enum HIDUsage : ushort
    {
        // HIDUsagePage is set to General
        Pointer = 0x01,
        Mouse = 0x02,
        Joystick = 0x04,
        Gamepad = 0x05,
        Keyboard = 0x06,
        Keypad = 0x07,
        SystemControl = 0x80,

        HID_USAGE_GENERIC_X = 0x30,
        HID_USAGE_GENERIC_Y = 0x31,

        // HIDUsagePage is set to Digitizer
        PrecisionTouchpad = 0x05,
        HID_USAGE_DIGITIZER_TIP_SWITCH = 0x42,

        HID_USAGE_DIGITIZER_CONTACT_ID = 0x51,
        HID_USAGE_DIGITIZER_CONTACT_COUNT = 0x54,
    }

    [Flags]
    public enum FormatFlags : uint
    {
        FORMAT_MESSAGE_ALLOCATE_BUFFER = 0x00000100,
        FORMAT_MESSAGE_ARGUMENT_ARRAY = 0x00002000,
        FORMAT_MESSAGE_FROM_HMODULE = 0x00000800,
        FORMAT_MESSAGE_FROM_STRING = 0x00000400,
        FORMAT_MESSAGE_FROM_SYSTEM = 0x00001000,
        FORMAT_MESSAGE_IGNORE_INSERTS = 0x00000200,
    }
}
