﻿// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Input;
using osu.Framework.Platform.MacOS.Native;

namespace osu.Framework.Platform.MacOS
{
    internal class MacOSTextInput : GameWindowTextInput
    {
        // Defined as kCGEventFlagMaskAlphaShift in CoreGraphics
        private const ulong event_flag_mask_alpha_shift = 65536;

        // Defined as kCGEventSourceStateHIDSystemState in CoreGraphics
        private const int event_source_state_hid_system_state = 1;

        private static bool isCapsLockOn => (Cocoa.CGEventSourceFlagsState(event_source_state_hid_system_state) & event_flag_mask_alpha_shift) != 0;

        public MacOSTextInput(IWindow window)
            : base(window)
        {
        }

        protected override void HandleKeyPress(object sender, osuTK.KeyPressEventArgs e)
        {
            // Drop any keypresses if the control, alt, or windows/command key are being held.
            // This is a workaround for an issue on macOS where osuTK will fire KeyPress events even
            // if modifier keys are held.  This can be reverted when it is fixed on osuTK's side.
            var state = osuTK.Input.Keyboard.GetState();
            if (state.IsKeyDown(osuTK.Input.Key.LControl)
                || state.IsKeyDown(osuTK.Input.Key.RControl)
                || state.IsKeyDown(osuTK.Input.Key.LAlt)
                || state.IsKeyDown(osuTK.Input.Key.RAlt)
                || state.IsKeyDown(osuTK.Input.Key.LWin)
                || state.IsKeyDown(osuTK.Input.Key.RWin))
                return;

            // arbitrary choice here, but it caters for any non-printable keys on an A1243 Apple Keyboard
            if (e.KeyChar > 63000)
                return;

            // capslock is not correctly handled by osuTK, so force uppercase if capslock is on
            if (isCapsLockOn)
                e = new osuTK.KeyPressEventArgs(char.ToUpper(e.KeyChar));

            base.HandleKeyPress(sender, e);
        }
    }
}
