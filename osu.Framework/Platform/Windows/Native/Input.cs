// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.Drawing;
using System.Runtime.InteropServices;

namespace osu.Framework.Platform.Windows.Native
{
    internal static class Input
    {
        [DllImport("user32.dll")]
        public static extern bool RegisterRawInputDevices(
            [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)]
            RawInputDevice[] pRawInputDevices,
            int uiNumDevices,
            int cbSize);

        [DllImport("user32.dll")]
        public static extern int GetRawInputData(IntPtr hRawInput, RawInputCommand uiCommand, out RawInputData pData, ref int pcbSize, int cbSizeHeader);

        internal static Rectangle VirtualScreenRect => new Rectangle(
            GetSystemMetrics(SM_XVIRTUALSCREEN),
            GetSystemMetrics(SM_YVIRTUALSCREEN),
            GetSystemMetrics(SM_CXVIRTUALSCREEN),
            GetSystemMetrics(SM_CYVIRTUALSCREEN));

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

        public const long MI_WP_SIGNATURE = 0xFF515700;
        public const long MI_WP_SIGNATURE_MASK = 0xFFFFFF00;

        /// <summary>
        /// Flag distinguishing touch input from mouse input in <see cref="WM_INPUT"/> events.
        /// </summary>
        /// <remarks>
        /// https://docs.microsoft.com/en-us/windows/win32/tablet/system-events-and-mouse-messages
        /// <para>Additionally, the eighth bit, masked by 0x80, is used to differentiate touch input from pen input (0 = pen, 1 = touch).</para>
        /// </remarks>
        private const long touch_flag = 0x80;

        /// <summary>
        /// https://docs.microsoft.com/en-us/windows/win32/tablet/system-events-and-mouse-messages
        /// </summary>
        /// <param name="dw"><see cref="GetMessageExtraInfo"/> for the current <see cref="WM_INPUT"/> event.</param>
        /// <returns><c>true</c> if this <see cref="WM_INPUT"/> event is from a finger touch, <c>false</c> if it's from mouse or pen input.</returns>
        public static bool IsTouchEvent(long dw) => (dw & MI_WP_SIGNATURE_MASK) == MI_WP_SIGNATURE && HasTouchFlag(dw);

        /// <param name="extraInformation"><see cref="RawMouse.ExtraInformation"/> or <see cref="GetMessageExtraInfo"/></param>
        /// <returns>Whether <paramref name="extraInformation"/> has the <see cref="touch_flag"/> set.</returns>
        public static bool HasTouchFlag(long extraInformation) => (extraInformation & touch_flag) == touch_flag;

        [DllImport("user32.dll", SetLastError = false)]
        public static extern long GetMessageExtraInfo();
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

#pragma warning disable IDE1006 // Naming style

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
    }

#pragma warning restore IDE1006

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

    public enum HIDUsage : ushort
    {
        Pointer = 0x01,
        Mouse = 0x02,
        Joystick = 0x04,
        Gamepad = 0x05,
        Keyboard = 0x06,
        Keypad = 0x07,
        SystemControl = 0x80,
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
}
