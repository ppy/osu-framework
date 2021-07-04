// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using osu.Framework.Logging;

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

        // One thing that may be detrimental is that gc would just move shit around if this is not in a fixed statement.
        [DllImport("Hid.dll")]
        public static extern NSStatus HidP_GetUsageValue(HidpReportType reportType, HIDUsagePage usagePage, ushort linkCollection, HIDUsage usage, out uint usageValue, byte[] preparsedData, byte[] report, int reportLength);

        [DllImport("Hid.dll")]
        public static extern NSStatus HidP_GetScaledUsageValue(HidpReportType reportType, HIDUsagePage usagePage, ushort linkCollection, ushort usage, out int usageValue, byte[] preparsedData, byte[] report, int reportLength);

        [DllImport("Hid.dll")]
        public static extern NSStatus HidP_GetCaps(byte[] preparsedData, out HidpCaps capabilities);

        [DllImport("Hid.dll")]
        public static extern NSStatus HidP_GetValueCaps(HidpReportType reportType, byte[] valueCaps, ref ushort valueCapsLength, byte[] preparsedData);

        [DllImport("Hid.dll")]
        public static extern NSStatus HidP_GetButtonCaps(HidpReportType reportType, byte[] valueCaps, ref ushort valueCapsLength, byte[] preparsedData);

        public static bool GetHidUsageButton(HidpReportType reportType, HIDUsagePage usagePage, ushort linkCollection, HIDUsage usage, byte[] preparsedData, byte[] report, int reportLength)
        {
            uint numUsages = HidP_MaxUsageListLength(reportType, usagePage, preparsedData);

            ushort[] usages = new ushort[numUsages];

            HidP_GetUsages(reportType, usagePage, linkCollection, usages, ref numUsages, preparsedData, report, reportLength);

            return usages.Any(u => u == (uint)usage);
        }

        public static unsafe RawInputData GetRawInputData(long lParam)
        {
            uint payloadSize = 0;
            int statusCode = GetRawInputData((IntPtr)lParam, RawInputCommand.Input, (IntPtr)null, ref payloadSize, (uint)sizeof(RawInputHeader));
            if (statusCode == -1)
                Logger.Log("Something is pretty wrong");
            var bytes = new byte[payloadSize];

            fixed (byte* bytesPtr = bytes)
            {
                statusCode = GetRawInputData((IntPtr)lParam, RawInputCommand.Input, (IntPtr)bytesPtr, ref payloadSize, (uint)sizeof(RawInputHeader));
                if (statusCode == -1)
                    Logger.Log("Something is pretty wrong");
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
        public int dwSizeHid;

        public int dwCount;

        // While this should be an array, it can't be as it depends on the 2 above
        // variables, this is also why you shouldn't call sizeof on the RawInputData struct
        // as it would produce unreliable results.
        public byte[] rawData;

        internal static RawHID FromPointer(byte* ptr)
        {
            var result = new RawHID();
            var intPtr = (int*)ptr;

            result.dwSizeHid = intPtr[0];
            result.dwCount = intPtr[1];
            result.rawData = new byte[result.dwSizeHid * result.dwCount];
            Marshal.Copy(new IntPtr(&intPtr[2]), result.rawData, 0, result.rawData.Length);

            return result;
        }

        public override string ToString() =>
            $"{{Count: {dwCount}, Size: {dwSizeHid}, Content: {BitConverter.ToString(rawData).Replace("-", " ")}}}";
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

    internal enum HidpReportType
    {
        HidP_Input,
        HidP_Output,
        HidP_Feature
    }

    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct HidpCaps
    {
        public HIDUsage Usage;
        public HIDUsagePage UsagePage;
        public ushort InputReportByteLength;
        public ushort OutputReportByteLength;
        public ushort FeatureReportByteLength;
        public fixed ushort Reserved[17];
        public ushort NumberLinkCollectionNodes;
        public ushort NumberInputButtonCaps;
        public ushort NumberInputValueCaps;
        public ushort NumberInputDataIndices;
        public ushort NumberOutputButtonCaps;
        public ushort NumberOutputValueCaps;
        public ushort NumberOutputDataIndices;
        public ushort NumberFeatureButtonCaps;
        public ushort NumberFeatureValueCaps;
        public ushort NumberFeatureDataIndices;

        public override string ToString() =>
            $"{{Usage: {Usage}, UsagePage: {UsagePage}, InputReportByteLength: {InputReportByteLength}, \n" +
            $"InputReportByteLength: {InputReportByteLength}, InputReportByteLength: {InputReportByteLength}, \n" +
            $"OutputReportByteLength: {OutputReportByteLength}, FeatureReportByteLength: {FeatureReportByteLength}, \n" +
            $"InputReportByteLength: {InputReportByteLength}, NumberLinkCollectionNodes: {NumberLinkCollectionNodes}, \n" +
            $"NumberInputButtonCaps: {NumberInputButtonCaps}, NumberInputValueCaps: {NumberInputValueCaps}, \n" +
            $"NumberInputDataIndices: {NumberInputDataIndices}, NumberOutputButtonCaps: {NumberOutputButtonCaps}, \n" +
            $"NumberOutputValueCaps: {NumberOutputValueCaps}, NumberOutputDataIndices: {NumberOutputDataIndices}, \n" +
            $"NumberFeatureButtonCaps: {NumberFeatureButtonCaps}, NumberFeatureValueCaps: {NumberFeatureValueCaps}, \n" +
            $"NumberFeatureDataIndices: {NumberFeatureDataIndices}";
    }
}
