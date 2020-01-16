// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Drawing;
using System.Runtime.InteropServices;
using osuTK;

namespace osu.Framework.Platform.Windows.Native
{
    internal static class Input
    {
        [DllImport("user32.dll")]
        internal static extern bool RegisterTouchWindow(IntPtr hWnd, int flags);

        [DllImport(@"user32.dll", CharSet = CharSet.Unicode)]
        internal static extern int SetProp(IntPtr hWnd, string lpString, int hData);

        [DllImport(@"user32.dll", CharSet = CharSet.Unicode)]
        internal static extern int RemoveProp(IntPtr hWnd, string lpString);

        [DllImport("user32.dll")]
        internal static extern int GetSystemMetrics(int nIndex);

        [DllImport("user32.dll")]
        internal static extern bool RegisterRawInputDevices(
            [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)]
            RawInputDevice[] pRawInputDevices,
            int uiNumDevices,
            int cbSize);

        [DllImport("user32.dll")]
        internal static extern bool GetTouchInputInfo(
            IntPtr hTouchInput,
            int uiNumDevices,
            [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1), Out]
            RawTouchInput[] pRawTouchInputs,
            int cbSize);

        [DllImport("user32.dll")]
        internal static extern bool CloseTouchInputHandle(IntPtr hTouchInput);

        [DllImport("user32.dll")]
        internal static extern int GetRawInputData(IntPtr hRawInput, RawInputCommand uiCommand, out RawInput pData, ref int pcbSize, int cbSizeHeader);

        [DllImport("user32.dll")]
        internal static extern bool GetPointerInfo(int pointerID, out RawPointerInput type);

        internal static Rectangle GetVirtualScreenRect() => new Rectangle(
            GetSystemMetrics(SM_XVIRTUALSCREEN),
            GetSystemMetrics(SM_YVIRTUALSCREEN),
            GetSystemMetrics(SM_CXVIRTUALSCREEN),
            GetSystemMetrics(SM_CYVIRTUALSCREEN)
        );

        internal const int SM_XVIRTUALSCREEN = 76;
        internal const int SM_YVIRTUALSCREEN = 77;

        internal const int SM_CXVIRTUALSCREEN = 78;
        internal const int SM_CYVIRTUALSCREEN = 79;

        internal const int WM_MOUSEACTIVATE = 0x21;

        internal const int WM_NCPOINTERUPDATE = 0x0241;
        internal const int WM_NCPOINTERDOWN = 0x0242;
        internal const int WM_NCPOINTERUP = 0x0243;
        internal const int WM_POINTERUPDATE = 0x0245;
        internal const int WM_POINTERDOWN = 0x0246;
        internal const int WM_POINTERUP = 0x0247;
        internal const int WM_POINTERENTER = 0x0249;
        internal const int WM_POINTERLEAVE = 0x024A;
        internal const int WM_POINTERACTIVATE = 0x024B;
        internal const int WM_POINTERCAPTURECHANGED = 0x024C;
        internal const int WM_POINTERWHEEL = 0x024E;
        internal const int WM_POINTERHWHEEL = 0x024F;

        internal const int WM_INPUT = 0x00FF;
        internal const int WM_TOUCH = 0x0240;

        internal const int TWF_FINETOUCH = 0x00000001;
        internal const int TWF_WANTPALM = 0x00000002;

        internal const int TABLET_DISABLE_PRESSANDHOLD = 0x00000001;
        internal const int TABLET_DISABLE_PENTAPFEEDBACK = 0x00000008;
        internal const int TABLET_DISABLE_PENBARRELFEEDBACK = 0x00000010;
        internal const int TABLET_DISABLE_TOUCHUIFORCEON = 0x00000100;
        internal const int TABLET_DISABLE_TOUCHUIFORCEOFF = 0x00000200;
        internal const int TABLET_DISABLE_TOUCHSWITCH = 0x00008000;
        internal const int TABLET_DISABLE_FLICKS = 0x00010000;
        internal const int TABLET_ENABLE_FLICKSONCONTEXT = 0x00020000;
        internal const int TABLET_ENABLE_FLICKLEARNINGMODE = 0x00040000;
        internal const int TABLET_DISABLE_SMOOTHSCROLLING = 0x00080000;
        internal const int TABLET_DISABLE_FLICKFALLBACKKEYS = 0x00100000;
        internal const int TABLET_ENABLE_MULTITOUCHDATA = 0x01000000;
    }

    /// <summary>
    /// Enumeration containing pointer types.
    /// </summary>
    internal enum RawPointerType : uint
    {
        Generic = 0x00000001,
        Touch = 0x00000002,
        Pen = 0x00000003,
        Mouse = 0x00000004,
        Touchpad = 0x00000005,
    }

    internal enum RawPointerButtonType : uint
    {
        None = 0,
        FirstButtonDown,
        FirstButtonUp,
        SecondButtonDown,
        SecondButtonUp,
        ThirdButtonDown,
        ThirdButtonUp,
        FourthButtonDown,
        FourthButtonUp,
        FifthButtonDown,
        FifthButtonUp,
    }

    /// <summary>
    /// Enumeration containing pointer flags.
    /// </summary>
    [Flags]
    internal enum RawPointerFlag : uint
    {
        /// <summary>
        /// Default.
        /// </summary>
        None = 0x00000000,

        /// <summary>
        /// Indicates the arrival of a new pointer.
        /// </summary>
        New = 0x00000001,

        /// <summary>
        /// Indicates that this pointer continues to exist. When this flag is not set, it indicates the pointer has left detection range.
        /// This flag is typically not set only when a hovering pointer leaves detection range (POINTER_FLAG_UPDATE is set) or when a pointer in contact with a window surface leaves detection range (POINTER_FLAG_UP is set).
        /// </summary>
        InRange = 0x00000002,

        /// <summary>
        /// Indicates that this pointer is in contact with the digitizer surface. When this flag is not set, it indicates a hovering pointer.
        /// </summary>
        InContact = 0x00000004,

        /// <summary>
        /// Indicates a primary action, analogous to a left mouse button down.
        /// A touch pointer has this flag set when it is in contact with the digitizer surface.
        /// A pen pointer has this flag set when it is in contact with the digitizer surface with no buttons pressed.
        /// A mouse pointer has this flag set when the left mouse button is down.
        /// </summary>
        FirstButton = 0x00000010,

        /// <summary>
        /// Indicates a secondary action, analogous to a right mouse button down.
        /// A touch pointer does not use this flag.
        /// A pen pointer has this flag set when it is in contact with the digitizer surface with the pen barrel button pressed.
        /// A mouse pointer has this flag set when the right mouse button is down.
        /// </summary>
        SecondButton = 0x00000020,

        /// <summary>
        /// Analogous to a mouse wheel button down.
        /// A touch pointer does not use this flag.
        /// A pen pointer does not use this flag.
        /// A mouse pointer has this flag set when the mouse wheel button is down.
        /// </summary>
        ThirdButton = 0x00000040,

        /// <summary>
        /// Analogous to a first extended mouse (XButton1) button down.
        /// A touch pointer does not use this flag.
        /// A pen pointer does not use this flag.
        /// A mouse pointer has this flag set when the first extended mouse (XBUTTON1) button is down.
        /// </summary>
        FourthButton = 0x00000080,

        /// <summary>
        /// Analogous to a second extended mouse (XButton2) button down.
        /// A touch pointer does not use this flag.
        /// A pen pointer does not use this flag.
        /// A mouse pointer has this flag set when the second extended mouse (XBUTTON2) button is down.
        /// </summary>
        FifthButton = 0x00000100,

        /// <summary>
        /// Indicates that this pointer has been designated as the primary pointer. A primary pointer is a single pointer that can perform actions beyond those available to non-primary pointers. For example, when a primary pointer makes contact with a window’s surface, it may provide the window an opportunity to activate by sending it a WM_POINTERACTIVATE message.
        /// The primary pointer is identified from all current user interactions on the system (mouse, touch, pen, and so on). As such, the primary pointer might not be associated with your app. The first contact in a multi-touch interaction is set as the primary pointer. Once a primary pointer is identified, all contacts must be lifted before a new contact can be identified as a primary pointer. For apps that don't process pointer input, only the primary pointer's events are promoted to mouse events.
        /// </summary>
        Primary = 0x00002000,

        /// <summary>
        /// Confidence is a suggestion from the source device about whether the pointer represents an intended or accidental interaction, which is especially relevant for PT_TOUCH pointers where an accidental interaction (such as with the palm of the hand) can trigger input. The presence of this flag indicates that the source device has high confidence that this input is part of an intended interaction.
        /// </summary>
        Confidence = 0x000004000,

        /// <summary>
        /// Indicates that the pointer is departing in an abnormal manner, such as when the system receives invalid input for the pointer or when a device with active pointers departs abruptly. If the application receiving the input is in a position to do so, it should treat the interaction as not completed and reverse any effects of the concerned pointer.
        /// </summary>
        Canceled = 0x000008000,

        /// <summary>
        /// Indicates that this pointer transitioned to a down state; that is, it made contact with the digitizer surface.
        /// </summary>
        Down = 0x00010000,

        /// <summary>
        /// Indicates that this is a simple update that does not include pointer state changes.
        /// </summary>
        Update = 0x00020000,

        /// <summary>
        /// Indicates that this pointer transitioned to an up state; that is, it broke contact with the digitizer surface.
        /// </summary>
        Up = 0x00040000,

        /// <summary>
        /// Indicates input associated with a pointer wheel. For mouse pointers, this is equivalent to the action of the mouse scroll wheel (WM_MOUSEWHEEL).
        /// </summary>
        Wheel = 0x00080000,

        /// <summary>
        /// Indicates input associated with a pointer h-wheel. For mouse pointers, this is equivalent to the action of the mouse horizontal scroll wheel (WM_MOUSEHWHEEL).
        /// </summary>
        HWheel = 0x00100000,

        /// <summary>
        /// Indicates that this pointer was captured by (associated with) another element and the original element has lost capture (see WM_POINTERCAPTURECHANGED).
        /// </summary>
        CaptureChanged = 0x00200000,
    }

    /// <summary>
    /// Contains information about the state of a touch input
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    internal struct RawPointerInput
    {
        internal RawPointerType Type;
        internal int ID;
        internal uint FrameID;
        internal RawPointerFlag Flags;
        internal IntPtr SourceDevice;
        internal IntPtr TargetWindow;
        internal Point PixelLocation;
        internal Point HimetricLocation;
        internal Point PixelLocationRaw;
        internal Point HimetricLocationRaw;
        internal int Time;
        internal uint HistoryCount;
        internal int InputData;
        internal uint KeyStates;
        internal ulong PerformanceCount;
        internal RawPointerButtonType ButtonChangeType;
    }

    /// <summary>
    /// Contains information about the state of a touch input
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    internal struct RawTouchInput
    {
        /// <summary>
        /// The x-coordinate (horizontal point) of the touch input. This member is indicated in hundredths of a pixel of physical screen coordinates.
        /// </summary>
        internal int X;

        /// <summary>
        /// The y-coordinate (vertical point) of the touch input. This member is indicated in hundredths of a pixel of physical screen coordinates.
        /// </summary>
        internal int Y;

        /// <summary>
        /// A device handle for the source input device. Each device is given a unique provider at run time by the touch input provider.
        /// </summary>
        internal IntPtr Source;

        /// <summary>
        /// A touch point identifier that distinguishes a particular touch input. This value stays consistent in a touch contact sequence from the point a contact comes down until it comes back up. An ID may be reused later for subsequent contacts.
        /// </summary>
        internal int ID;

        /// <summary>
        /// A set of bit flags that specify various aspects of touch point press, release, and motion. The bits in this member can be any reasonable combination of the values in the Remarks section.
        /// </summary>
        internal RawTouchFlag Flags;

        /// <summary>
        /// A set of bit flags that specify which of the optional fields in the structure contain valid values. The availability of valid information in the optional fields is device-specific. Applications should use an optional field value only when the corresponding bit is set in Mask..
        /// </summary>
        internal RawTouchMaskFlag Mask;

        /// <summary>
        /// The time stamp for the event, in milliseconds. The consuming application should note that the system performs no validation on this field; when the TOUCHINPUTMASKF_TIMEFROMSYSTEM flag is not set, the accuracy and sequencing of values in this field are completely dependent on the touch input provider.
        /// </summary>
        internal int Time;

        /// <summary>
        /// An additional value associated with the touch event.
        /// </summary>
        internal int ExtraInfo;

        /// <summary>
        /// The width of the touch contact area in hundredths of a pixel in physical screen coordinates. This value is only valid if the Mask member has the TOUCHEVENTFMASK_CONTACTAREA flag set.
        /// </summary>
        internal uint AreaWidth;

        /// <summary>
        /// The height of the touch contact area in hundredths of a pixel in physical screen coordinates. This value is only valid if the Mask member has the TOUCHEVENTFMASK_CONTACTAREA flag set.
        /// </summary>
        internal uint AreaHeight;
    }

    /// <summary>
    /// Enumeration containing flags for raw touch input.
    /// </summary>
    [Flags]
    internal enum RawTouchFlag : uint
    {
        /// <summary>
        /// Movement has occurred. Cannot be combined with TOUCHEVENTF_DOWN.
        /// </summary>
        Move = 0x0001,

        /// <summary>
        /// The corresponding touch point was established through a new contact. Cannot be combined with TOUCHEVENTF_MOVE or TOUCHEVENTF_UP.
        /// </summary>
        Down = 0x0002,

        /// <summary>
        /// A touch point was removed.
        /// </summary>
        Up = 0x0004,

        /// <summary>
        /// A touch point is in range. This flag is used to enable touch hover support on compatible hardware. Applications that do not want support for hover can ignore this flag.
        /// </summary>
        InRange = 0x0008,

        /// <summary>
        /// Indicates that this TOUCHINPUT structure corresponds to a primary contact point. See the following FontText for more information on primary touch points.
        /// </summary>
        Primary = 0x0010,

        /// <summary>
        /// When received using GetTouchInputInfo, this input was not coalesced.
        /// </summary>
        NoCoalesce = 0x0020,

        /// <summary>
        /// The touch event came from the user's palm.
        /// </summary>
        Palm = 0x0080,
    }

    /// <summary>
    /// Enumeration containing mask flags for raw touch input.
    /// </summary>
    [Flags]
    internal enum RawTouchMaskFlag : uint
    {
        /// <summary>
        /// AreaWidth and AreaHeight are valid.
        /// </summary>
        ContactArea = 0x0004,

        /// <summary>
        /// ExtraInfo is valid.
        /// </summary>
        ExtraInfo = 0x0002,

        /// <summary>
        /// The system time was set in the TOUCHINPUT structure.
        /// </summary>
        TimeFromSystem = 0x0001,
    }

    /// <summary>
    /// Value type for raw input.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal struct RawInput
    {
        internal RawInputHeader Header;
        internal RawInputData Data;

        internal static readonly int SizeInBytes =
            BlittableValueType<RawInput>.Stride;
    }

    [StructLayout(LayoutKind.Explicit)]
    internal struct RawInputData
    {
        [FieldOffset(0)]
        internal RawMouse Mouse;

        [FieldOffset(0)]
        internal RawKeyboard Keyboard;

        [FieldOffset(0)]
        internal RawInputHid HID;
    }

    //unused structs
    /// <summary>
    /// Value type for raw input from a keyboard.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    internal struct RawKeyboard
    {
        /// <summary>Scan code for key depression.</summary>
        internal short MakeCode;

        /// <summary>Scan code information.</summary>
        internal RawKeyboardFlag Flags;

        /// <summary>Reserved.</summary>
        internal short Reserved;

        /// <summary>Virtual key code.</summary>
        internal VirtualKey VirtualKey;

        /// <summary>Corresponding window message.</summary>
        internal WindowsMessage Message;

        /// <summary>Extra information.</summary>
        internal int ExtraInformation;
    }

    /// <summary>
    /// Enumeration containing flags for raw keyboard input.
    /// </summary>
    [Flags]
    internal enum RawKeyboardFlag : ushort
    {
        /// <summary></summary>
        KeyMake = 0,

        /// <summary></summary>
        KeyBreak = 1,

        /// <summary></summary>
        KeyE0 = 2,

        /// <summary></summary>
        KeyE1 = 4,

        /// <summary></summary>
        TerminalServerSetLED = 8,

        /// <summary></summary>
        TerminalServerShadow = 0x10,

        /// <summary></summary>
        TerminalServerVKPACKET = 0x20
    }

    internal struct RawInputHid
    {
    }

    /// <summary>
    /// Contains information about the state of the mouse.
    /// </summary>
    [StructLayout(LayoutKind.Explicit)]
    internal struct RawMouse
    {
        /// <summary>
        /// The mouse state.
        /// </summary>
        [FieldOffset(0)]
        internal RawMouseFlag Flags;

        /// <summary>
        /// Flags for the event.
        /// </summary>
        [FieldOffset(4)]
        internal RawMouseButton ButtonFlags;

        /// <summary>
        /// If the mouse wheel is moved, this will contain the delta amount.
        /// </summary>
        [FieldOffset(6)]
        internal short ButtonData;

        /// <summary>
        /// Raw button data.
        /// </summary>
        [FieldOffset(8)]
        internal uint RawButtons;

        /// <summary>
        /// The motion in the X direction. This is signed relative motion or
        /// absolute motion, depending on the value of usFlags.
        /// </summary>
        [FieldOffset(12)]
        internal int LastX;

        /// <summary>
        /// The motion in the Y direction. This is signed relative motion or absolute motion,
        /// depending on the value of usFlags.
        /// </summary>
        [FieldOffset(16)]
        internal int LastY;

        /// <summary>
        /// The device-specific additional information for the event.
        /// </summary>
        [FieldOffset(20)]
        internal uint ExtraInformation;
    }

    /// <summary>
    /// Enumeration containing the flags for raw mouse data.
    /// </summary>
    [Flags]
    internal enum RawMouseFlag
        : ushort
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
    [Flags]
    internal enum RawMouseButton
        : ushort
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
    internal enum RawInputCommand
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
    internal enum RawInputType
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
    internal struct RawInputHeader
    {
        /// <summary>Type of device the input is coming from.</summary>
        internal RawInputType Type;

        /// <summary>Size of the packet of data.</summary>
        internal int Size;

        /// <summary>Handle to the device sending the data.</summary>
        internal IntPtr Device;

        /// <summary>wParam from the window message.</summary>
        internal IntPtr wParam;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct RawInputDevice
    {
        /// <summary>Top level collection Usage page for the raw input device.</summary>
        internal HIDUsagePage UsagePage;

        /// <summary>Top level collection Usage for the raw input device. </summary>
        internal HIDUsage Usage;

        /// <summary>Mode flag that specifies how to interpret the information provided by UsagePage and Usage.</summary>
        internal RawInputDeviceFlag Flags;

        /// <summary>Handle to the target device. If NULL, it follows the keyboard focus.</summary>
        internal IntPtr WindowHandle;
    }

    /// <summary>Enumeration containing flags for a raw input device.</summary>
    [Flags]
    internal enum RawInputDeviceFlag
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

    /// <summary>
    /// Enumeration for virtual keys.
    /// </summary>
    internal enum VirtualKey
        : ushort
    {
        /// <summary></summary>
        LeftButton = 0x01,

        /// <summary></summary>
        RightButton = 0x02,

        /// <summary></summary>
        Cancel = 0x03,

        /// <summary></summary>
        MiddleButton = 0x04,

        /// <summary></summary>
        ExtraButton1 = 0x05,

        /// <summary></summary>
        ExtraButton2 = 0x06,

        /// <summary></summary>
        Back = 0x08,

        /// <summary></summary>
        Tab = 0x09,

        /// <summary></summary>
        Clear = 0x0C,

        /// <summary></summary>
        Return = 0x0D,

        /// <summary></summary>
        Shift = 0x10,

        /// <summary></summary>
        Control = 0x11,

        /// <summary></summary>
        Menu = 0x12,

        /// <summary></summary>
        Pause = 0x13,

        /// <summary></summary>
        CapsLock = 0x14,

        /// <summary></summary>
        Kana = 0x15,

        /// <summary></summary>
        Hangeul = 0x15,

        /// <summary></summary>
        Hangul = 0x15,

        /// <summary></summary>
        Junja = 0x17,

        /// <summary></summary>
        Final = 0x18,

        /// <summary></summary>
        Hanja = 0x19,

        /// <summary></summary>
        Kanji = 0x19,

        /// <summary></summary>
        Escape = 0x1B,

        /// <summary></summary>
        Convert = 0x1C,

        /// <summary></summary>
        NonConvert = 0x1D,

        /// <summary></summary>
        Accept = 0x1E,

        /// <summary></summary>
        ModeChange = 0x1F,

        /// <summary></summary>
        Space = 0x20,

        /// <summary></summary>
        Prior = 0x21,

        /// <summary></summary>
        Next = 0x22,

        /// <summary></summary>
        End = 0x23,

        /// <summary></summary>
        Home = 0x24,

        /// <summary></summary>
        Left = 0x25,

        /// <summary></summary>
        Up = 0x26,

        /// <summary></summary>
        Right = 0x27,

        /// <summary></summary>
        Down = 0x28,

        /// <summary></summary>
        Select = 0x29,

        /// <summary></summary>
        Print = 0x2A,

        /// <summary></summary>
        Execute = 0x2B,

        /// <summary></summary>
        Snapshot = 0x2C,

        /// <summary></summary>
        Insert = 0x2D,

        /// <summary></summary>
        Delete = 0x2E,

        /// <summary></summary>
        Help = 0x2F,

        /// <summary></summary>
        N0 = 0x30,

        /// <summary></summary>
        N1 = 0x31,

        /// <summary></summary>
        N2 = 0x32,

        /// <summary></summary>
        N3 = 0x33,

        /// <summary></summary>
        N4 = 0x34,

        /// <summary></summary>
        N5 = 0x35,

        /// <summary></summary>
        N6 = 0x36,

        /// <summary></summary>
        N7 = 0x37,

        /// <summary></summary>
        N8 = 0x38,

        /// <summary></summary>
        N9 = 0x39,

        /// <summary></summary>
        A = 0x41,

        /// <summary></summary>
        B = 0x42,

        /// <summary></summary>
        C = 0x43,

        /// <summary></summary>
        D = 0x44,

        /// <summary></summary>
        E = 0x45,

        /// <summary></summary>
        F = 0x46,

        /// <summary></summary>
        G = 0x47,

        /// <summary></summary>
        H = 0x48,

        /// <summary></summary>
        I = 0x49,

        /// <summary></summary>
        J = 0x4A,

        /// <summary></summary>
        K = 0x4B,

        /// <summary></summary>
        L = 0x4C,

        /// <summary></summary>
        M = 0x4D,

        /// <summary></summary>
        N = 0x4E,

        /// <summary></summary>
        O = 0x4F,

        /// <summary></summary>
        P = 0x50,

        /// <summary></summary>
        Q = 0x51,

        /// <summary></summary>
        R = 0x52,

        /// <summary></summary>
        S = 0x53,

        /// <summary></summary>
        T = 0x54,

        /// <summary></summary>
        U = 0x55,

        /// <summary></summary>
        V = 0x56,

        /// <summary></summary>
        W = 0x57,

        /// <summary></summary>
        X = 0x58,

        /// <summary></summary>
        Y = 0x59,

        /// <summary></summary>
        Z = 0x5A,

        /// <summary></summary>
        LeftWindows = 0x5B,

        /// <summary></summary>
        RightWindows = 0x5C,

        /// <summary></summary>
        Application = 0x5D,

        /// <summary></summary>
        Sleep = 0x5F,

        /// <summary></summary>
        Numpad0 = 0x60,

        /// <summary></summary>
        Numpad1 = 0x61,

        /// <summary></summary>
        Numpad2 = 0x62,

        /// <summary></summary>
        Numpad3 = 0x63,

        /// <summary></summary>
        Numpad4 = 0x64,

        /// <summary></summary>
        Numpad5 = 0x65,

        /// <summary></summary>
        Numpad6 = 0x66,

        /// <summary></summary>
        Numpad7 = 0x67,

        /// <summary></summary>
        Numpad8 = 0x68,

        /// <summary></summary>
        Numpad9 = 0x69,

        /// <summary></summary>
        Multiply = 0x6A,

        /// <summary></summary>
        Add = 0x6B,

        /// <summary></summary>
        Separator = 0x6C,

        /// <summary></summary>
        Subtract = 0x6D,

        /// <summary></summary>
        Decimal = 0x6E,

        /// <summary></summary>
        Divide = 0x6F,

        /// <summary></summary>
        F1 = 0x70,

        /// <summary></summary>
        F2 = 0x71,

        /// <summary></summary>
        F3 = 0x72,

        /// <summary></summary>
        F4 = 0x73,

        /// <summary></summary>
        F5 = 0x74,

        /// <summary></summary>
        F6 = 0x75,

        /// <summary></summary>
        F7 = 0x76,

        /// <summary></summary>
        F8 = 0x77,

        /// <summary></summary>
        F9 = 0x78,

        /// <summary></summary>
        F10 = 0x79,

        /// <summary></summary>
        F11 = 0x7A,

        /// <summary></summary>
        F12 = 0x7B,

        /// <summary></summary>
        F13 = 0x7C,

        /// <summary></summary>
        F14 = 0x7D,

        /// <summary></summary>
        F15 = 0x7E,

        /// <summary></summary>
        F16 = 0x7F,

        /// <summary></summary>
        F17 = 0x80,

        /// <summary></summary>
        F18 = 0x81,

        /// <summary></summary>
        F19 = 0x82,

        /// <summary></summary>
        F20 = 0x83,

        /// <summary></summary>
        F21 = 0x84,

        /// <summary></summary>
        F22 = 0x85,

        /// <summary></summary>
        F23 = 0x86,

        /// <summary></summary>
        F24 = 0x87,

        /// <summary></summary>
        NumLock = 0x90,

        /// <summary></summary>
        ScrollLock = 0x91,

        /// <summary></summary>
        NEC_Equal = 0x92,

        /// <summary></summary>
        Fujitsu_Jisho = 0x92,

        /// <summary></summary>
        Fujitsu_Masshou = 0x93,

        /// <summary></summary>
        Fujitsu_Touroku = 0x94,

        /// <summary></summary>
        Fujitsu_Loya = 0x95,

        /// <summary></summary>
        Fujitsu_Roya = 0x96,

        /// <summary></summary>
        LeftShift = 0xA0,

        /// <summary></summary>
        RightShift = 0xA1,

        /// <summary></summary>
        LeftControl = 0xA2,

        /// <summary></summary>
        RightControl = 0xA3,

        /// <summary></summary>
        LeftMenu = 0xA4,

        /// <summary></summary>
        RightMenu = 0xA5,

        /// <summary></summary>
        BrowserBack = 0xA6,

        /// <summary></summary>
        BrowserForward = 0xA7,

        /// <summary></summary>
        BrowserRefresh = 0xA8,

        /// <summary></summary>
        BrowserStop = 0xA9,

        /// <summary></summary>
        BrowserSearch = 0xAA,

        /// <summary></summary>
        BrowserFavorites = 0xAB,

        /// <summary></summary>
        BrowserHome = 0xAC,

        /// <summary></summary>
        VolumeMute = 0xAD,

        /// <summary></summary>
        VolumeDown = 0xAE,

        /// <summary></summary>
        VolumeUp = 0xAF,

        /// <summary></summary>
        MediaNextTrack = 0xB0,

        /// <summary></summary>
        MediaPrevTrack = 0xB1,

        /// <summary></summary>
        MediaStop = 0xB2,

        /// <summary></summary>
        MediaPlayPause = 0xB3,

        /// <summary></summary>
        LaunchMail = 0xB4,

        /// <summary></summary>
        LaunchMediaSelect = 0xB5,

        /// <summary></summary>
        LaunchApplication1 = 0xB6,

        /// <summary></summary>
        LaunchApplication2 = 0xB7,

        /// <summary></summary>
        OEM1 = 0xBA,

        /// <summary></summary>
        OEMPlus = 0xBB,

        /// <summary></summary>
        OEMComma = 0xBC,

        /// <summary></summary>
        OEMMinus = 0xBD,

        /// <summary></summary>
        OEMPeriod = 0xBE,

        /// <summary></summary>
        OEM2 = 0xBF,

        /// <summary></summary>
        OEM3 = 0xC0,

        /// <summary></summary>
        OEM4 = 0xDB,

        /// <summary></summary>
        OEM5 = 0xDC,

        /// <summary></summary>
        OEM6 = 0xDD,

        /// <summary></summary>
        OEM7 = 0xDE,

        /// <summary></summary>
        OEM8 = 0xDF,

        /// <summary></summary>
        OEMAX = 0xE1,

        /// <summary></summary>
        OEM102 = 0xE2,

        /// <summary></summary>
        ICOHelp = 0xE3,

        /// <summary></summary>
        ICO00 = 0xE4,

        /// <summary></summary>
        ProcessKey = 0xE5,

        /// <summary></summary>
        ICOClear = 0xE6,

        /// <summary></summary>
        Packet = 0xE7,

        /// <summary></summary>
        OEMReset = 0xE9,

        /// <summary></summary>
        OEMJump = 0xEA,

        /// <summary></summary>
        OEMPA1 = 0xEB,

        /// <summary></summary>
        OEMPA2 = 0xEC,

        /// <summary></summary>
        OEMPA3 = 0xED,

        /// <summary></summary>
        OEMWSCtrl = 0xEE,

        /// <summary></summary>
        OEMCUSel = 0xEF,

        /// <summary></summary>
        OEMATTN = 0xF0,

        /// <summary></summary>
        OEMFinish = 0xF1,

        /// <summary></summary>
        OEMCopy = 0xF2,

        /// <summary></summary>
        OEMAuto = 0xF3,

        /// <summary></summary>
        OEMENLW = 0xF4,

        /// <summary></summary>
        OEMBackTab = 0xF5,

        /// <summary></summary>
        ATTN = 0xF6,

        /// <summary></summary>
        CRSel = 0xF7,

        /// <summary></summary>
        EXSel = 0xF8,

        /// <summary></summary>
        EREOF = 0xF9,

        /// <summary></summary>
        Play = 0xFA,

        /// <summary></summary>
        Zoom = 0xFB,

        /// <summary></summary>
        Noname = 0xFC,

        /// <summary></summary>
        PA1 = 0xFD,

        /// <summary></summary>
        OEMClear = 0xFE
    }

    internal enum HIDUsage : ushort
    {
        Pointer = 0x01,
        Mouse = 0x02,
        Joystick = 0x04,
        Gamepad = 0x05,
        Keyboard = 0x06,
        Keypad = 0x07,
        SystemControl = 0x80,
        X = 0x30,
        Y = 0x31,
        Z = 0x32,
        RelativeX = 0x33,
        RelativeY = 0x34,
        RelativeZ = 0x35,
        Slider = 0x36,
        Dial = 0x37,
        Wheel = 0x38,
        HatSwitch = 0x39,
        CountedBuffer = 0x3A,
        ByteCount = 0x3B,
        MotionWakeup = 0x3C,
        VX = 0x40,
        VY = 0x41,
        VZ = 0x42,
        VBRX = 0x43,
        VBRY = 0x44,
        VBRZ = 0x45,
        VNO = 0x46,
        SystemControlPower = 0x81,
        SystemControlSleep = 0x82,
        SystemControlWake = 0x83,
        SystemControlContextMenu = 0x84,
        SystemControlMainMenu = 0x85,
        SystemControlApplicationMenu = 0x86,
        SystemControlHelpMenu = 0x87,
        SystemControlMenuExit = 0x88,
        SystemControlMenuSelect = 0x89,
        SystemControlMenuRight = 0x8A,
        SystemControlMenuLeft = 0x8B,
        SystemControlMenuUp = 0x8C,
        SystemControlMenuDown = 0x8D,
        KeyboardNoEvent = 0x00,
        KeyboardRollover = 0x01,
        KeyboardPostFail = 0x02,
        KeyboardUndefined = 0x03,
        KeyboardaA = 0x04,
        KeyboardzZ = 0x1D,
        Keyboard1 = 0x1E,
        Keyboard0 = 0x27,
        KeyboardLeftControl = 0xE0,
        KeyboardLeftShift = 0xE1,
        KeyboardLeftALT = 0xE2,
        KeyboardLeftGUI = 0xE3,
        KeyboardRightControl = 0xE4,
        KeyboardRightShift = 0xE5,
        KeyboardRightALT = 0xE6,
        KeyboardRightGUI = 0xE7,
        KeyboardScrollLock = 0x47,
        KeyboardNumLock = 0x53,
        KeyboardCapsLock = 0x39,
        KeyboardF1 = 0x3A,
        KeyboardF12 = 0x45,
        KeyboardReturn = 0x28,
        KeyboardEscape = 0x29,
        KeyboardDelete = 0x2A,
        KeyboardPrintScreen = 0x46,
        LEDNumLock = 0x01,
        LEDCapsLock = 0x02,
        LEDScrollLock = 0x03,
        LEDCompose = 0x04,
        LEDKana = 0x05,
        LEDPower = 0x06,
        LEDShift = 0x07,
        LEDDoNotDisturb = 0x08,
        LEDMute = 0x09,
        LEDToneEnable = 0x0A,
        LEDHighCutFilter = 0x0B,
        LEDLowCutFilter = 0x0C,
        LEDEqualizerEnable = 0x0D,
        LEDSoundFieldOn = 0x0E,
        LEDSurroundFieldOn = 0x0F,
        LEDRepeat = 0x10,
        LEDStereo = 0x11,
        LEDSamplingRateDirect = 0x12,
        LEDSpinning = 0x13,
        LEDCAV = 0x14,
        LEDCLV = 0x15,
        LEDRecordingFormatDet = 0x16,
        LEDOffHook = 0x17,
        LEDRing = 0x18,
        LEDMessageWaiting = 0x19,
        LEDDataMode = 0x1A,
        LEDBatteryOperation = 0x1B,
        LEDBatteryOK = 0x1C,
        LEDBatteryLow = 0x1D,
        LEDSpeaker = 0x1E,
        LEDHeadset = 0x1F,
        LEDHold = 0x20,
        LEDMicrophone = 0x21,
        LEDCoverage = 0x22,
        LEDNightMode = 0x23,
        LEDSendCalls = 0x24,
        LEDCallPickup = 0x25,
        LEDConference = 0x26,
        LEDStandBy = 0x27,
        LEDCameraOn = 0x28,
        LEDCameraOff = 0x29,
        LEDOnLine = 0x2A,
        LEDOffLine = 0x2B,
        LEDBusy = 0x2C,
        LEDReady = 0x2D,
        LEDPaperOut = 0x2E,
        LEDPaperJam = 0x2F,
        LEDRemote = 0x30,
        LEDForward = 0x31,
        LEDReverse = 0x32,
        LEDStop = 0x33,
        LEDRewind = 0x34,
        LEDFastForward = 0x35,
        LEDPlay = 0x36,
        LEDPause = 0x37,
        LEDRecord = 0x38,
        LEDError = 0x39,
        LEDSelectedIndicator = 0x3A,
        LEDInUseIndicator = 0x3B,
        LEDMultiModeIndicator = 0x3C,
        LEDIndicatorOn = 0x3D,
        LEDIndicatorFlash = 0x3E,
        LEDIndicatorSlowBlink = 0x3F,
        LEDIndicatorFastBlink = 0x40,
        LEDIndicatorOff = 0x41,
        LEDFlashOnTime = 0x42,
        LEDSlowBlinkOnTime = 0x43,
        LEDSlowBlinkOffTime = 0x44,
        LEDFastBlinkOnTime = 0x45,
        LEDFastBlinkOffTime = 0x46,
        LEDIndicatorColor = 0x47,
        LEDRed = 0x48,
        LEDGreen = 0x49,
        LEDAmber = 0x4A,
        LEDGenericIndicator = 0x3B,
        TelephonyPhone = 0x01,
        TelephonyAnsweringMachine = 0x02,
        TelephonyMessageControls = 0x03,
        TelephonyHandset = 0x04,
        TelephonyHeadset = 0x05,
        TelephonyKeypad = 0x06,
        TelephonyProgrammableButton = 0x07,
        SimulationRudder = 0xBA,
        SimulationThrottle = 0xBB
    }

    internal enum HIDUsagePage : ushort
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
