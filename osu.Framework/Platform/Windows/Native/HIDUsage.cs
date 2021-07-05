// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace osu.Framework.Platform.Windows.Native
{
    public enum HIDUsage : ushort
    {
        // HIDUsagePage is General
        Pointer = 0x01,
        Mouse = 0x02,
        Joystick = 0x04,
        Gamepad = 0x05,
        Keyboard = 0x06,
        Keypad = 0x07,
        SystemControl = 0x80,

        HID_USAGE_GENERIC_X = 0x30,
        HID_USAGE_GENERIC_Y = 0x31,

        // HIDUsagePage is Digitizer
        PrecisionTouchpad = 0x05,
        HID_USAGE_DIGITIZER_TIP_SWITCH = 0x42,

        HID_USAGE_DIGITIZER_CONTACT_ID = 0x51,
        HID_USAGE_DIGITIZER_CONTACT_COUNT = 0x54,
    }

    public enum NSStatus : uint
    {
        HIDP_STATUS_SUCCESS = 0x00110000,
        HIDP_STATUS_INVALID_PREPARSED_DATA = 0xc0110001,
        HIDP_STATUS_USAGE_NOT_FOUND = 0xc0110004
    }
}
