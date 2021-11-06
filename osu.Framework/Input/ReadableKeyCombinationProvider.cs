// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;
using osu.Framework.Extensions.EnumExtensions;
using osu.Framework.Input.Bindings;

namespace osu.Framework.Input
{
    public class ReadableKeyCombinationProvider
    {
        /// <summary>
        /// Invoked when the system keyboard layout has changed.
        /// </summary>
        public event Action KeymapChanged;

        internal void OnKeymapChanged()
        {
            KeymapChanged?.Invoke();
        }

        /// <summary>
        /// Returns a human-readable string for a given <see cref="KeyCombination"/>.
        /// </summary>
        /// <remarks>
        /// Consumers should subscribe to <see cref="KeymapChanged"/> and re-generate the readable <see cref="KeyCombination"/> when the keyboard layout changes.
        /// </remarks>
        /// <returns>The <see cref="KeyCombination"/> as a human-readable string.</returns>
        public string GetReadableString(KeyCombination c)
        {
            var sortedKeys = c.Keys.GetValuesInOrder().ToArray();

            return string.Join('-', sortedKeys.Select(key =>
            {
                switch (key)
                {
                    case InputKey.Control:
                        if (sortedKeys.Contains(InputKey.LControl) || sortedKeys.Contains(InputKey.RControl))
                            return null;

                        break;

                    case InputKey.Shift:
                        if (sortedKeys.Contains(InputKey.LShift) || sortedKeys.Contains(InputKey.RShift))
                            return null;

                        break;

                    case InputKey.Alt:
                        if (sortedKeys.Contains(InputKey.LAlt) || sortedKeys.Contains(InputKey.RAlt))
                            return null;

                        break;

                    case InputKey.Super:
                        if (sortedKeys.Contains(InputKey.LSuper) || sortedKeys.Contains(InputKey.RSuper))
                            return null;

                        break;
                }

                return GetReadableKey(key);
            }).Where(s => !string.IsNullOrEmpty(s)));
        }

        protected virtual string GetReadableKey(InputKey key)
        {
            if (key >= InputKey.FirstTabletAuxiliaryButton)
                return $"Tablet Aux {key - InputKey.FirstTabletAuxiliaryButton + 1}";
            if (key >= InputKey.FirstTabletPenButton)
                return $"Tablet Pen {key - InputKey.FirstTabletPenButton + 1}";

            if (key >= InputKey.MidiA0)
                return key.ToString().Substring("Midi".Length).Replace("Sharp", "#");

            if (key >= InputKey.FirstJoystickHatRightButton)
                return $"Joystick Hat {key - InputKey.FirstJoystickHatRightButton + 1} Right";
            if (key >= InputKey.FirstJoystickHatLeftButton)
                return $"Joystick Hat {key - InputKey.FirstJoystickHatLeftButton + 1} Left";
            if (key >= InputKey.FirstJoystickHatDownButton)
                return $"Joystick Hat {key - InputKey.FirstJoystickHatDownButton + 1} Down";
            if (key >= InputKey.FirstJoystickHatUpButton)
                return $"Joystick Hat {key - InputKey.FirstJoystickHatUpButton + 1} Up";
            if (key >= InputKey.FirstJoystickAxisPositiveButton)
                return $"Joystick Axis {key - InputKey.FirstJoystickAxisPositiveButton + 1} +";
            if (key >= InputKey.FirstJoystickAxisNegativeButton)
                return $"Joystick Axis {key - InputKey.FirstJoystickAxisNegativeButton + 1} -";
            if (key >= InputKey.FirstJoystickButton)
                return $"Joystick {key - InputKey.FirstJoystickButton + 1}";

            switch (key)
            {
                case InputKey.None:
                    return string.Empty;

                case InputKey.Shift:
                    return "Shift";

                case InputKey.Control:
                    return "Ctrl";

                case InputKey.Alt:
                    return "Alt";

                case InputKey.Super:
                    return "Win";

                case InputKey.LShift:
                    return "LShift";

                case InputKey.LControl:
                    return "LCtrl";

                case InputKey.LAlt:
                    return "LAlt";

                case InputKey.LSuper:
                    return "LWin";

                case InputKey.RShift:
                    return "RShift";

                case InputKey.RControl:
                    return "RCtrl";

                case InputKey.RAlt:
                    return "RAlt";

                case InputKey.RSuper:
                    return "RWin";

                case InputKey.Escape:
                    return "Esc";

                case InputKey.BackSpace:
                    return "Backsp";

                case InputKey.Insert:
                    return "Ins";

                case InputKey.Delete:
                    return "Del";

                case InputKey.PageUp:
                    return "PgUp";

                case InputKey.PageDown:
                    return "PgDn";

                case InputKey.CapsLock:
                    return "Caps";

                case InputKey.Keypad0:
                    return "Numpad0";

                case InputKey.Keypad1:
                    return "Numpad1";

                case InputKey.Keypad2:
                    return "Numpad2";

                case InputKey.Keypad3:
                    return "Numpad3";

                case InputKey.Keypad4:
                    return "Numpad4";

                case InputKey.Keypad5:
                    return "Numpad5";

                case InputKey.Keypad6:
                    return "Numpad6";

                case InputKey.Keypad7:
                    return "Numpad7";

                case InputKey.Keypad8:
                    return "Numpad8";

                case InputKey.Keypad9:
                    return "Numpad9";

                case InputKey.KeypadDivide:
                    return "NumpadDivide";

                case InputKey.KeypadMultiply:
                    return "NumpadMultiply";

                case InputKey.KeypadMinus:
                    return "NumpadMinus";

                case InputKey.KeypadPlus:
                    return "NumpadPlus";

                case InputKey.KeypadDecimal:
                    return "NumpadDecimal";

                case InputKey.KeypadEnter:
                    return "NumpadEnter";

                case InputKey.Number0:
                    return "0";

                case InputKey.Number1:
                    return "1";

                case InputKey.Number2:
                    return "2";

                case InputKey.Number3:
                    return "3";

                case InputKey.Number4:
                    return "4";

                case InputKey.Number5:
                    return "5";

                case InputKey.Number6:
                    return "6";

                case InputKey.Number7:
                    return "7";

                case InputKey.Number8:
                    return "8";

                case InputKey.Number9:
                    return "9";

                case InputKey.Tilde:
                    return "~";

                case InputKey.Minus:
                    return "Minus";

                case InputKey.Plus:
                    return "Plus";

                case InputKey.BracketLeft:
                    return "[";

                case InputKey.BracketRight:
                    return "]";

                case InputKey.Semicolon:
                    return ";";

                case InputKey.Quote:
                    return "\"";

                case InputKey.Comma:
                    return ",";

                case InputKey.Period:
                    return ".";

                case InputKey.Slash:
                    return "/";

                case InputKey.BackSlash:
                case InputKey.NonUSBackSlash:
                    return "\\";

                case InputKey.Mute:
                    return "Mute";

                case InputKey.VolumeDown:
                    return "Vol. Down";

                case InputKey.VolumeUp:
                    return "Vol. Up";

                case InputKey.Stop:
                    return "Media Stop";

                case InputKey.PlayPause:
                    return "Media Play";

                case InputKey.TrackNext:
                    return "Media Next";

                case InputKey.TrackPrevious:
                    return "Media Previous";

                case InputKey.MouseLeft:
                    return "M1";

                case InputKey.MouseMiddle:
                    return "M3";

                case InputKey.MouseRight:
                    return "M2";

                case InputKey.ExtraMouseButton1:
                    return "M4";

                case InputKey.ExtraMouseButton2:
                    return "M5";

                case InputKey.ExtraMouseButton3:
                    return "M6";

                case InputKey.ExtraMouseButton4:
                    return "M7";

                case InputKey.ExtraMouseButton5:
                    return "M8";

                case InputKey.ExtraMouseButton6:
                    return "M9";

                case InputKey.ExtraMouseButton7:
                    return "M10";

                case InputKey.ExtraMouseButton8:
                    return "M11";

                case InputKey.ExtraMouseButton9:
                    return "M12";

                case InputKey.MouseWheelDown:
                    return "Wheel Down";

                case InputKey.MouseWheelUp:
                    return "Wheel Up";

                case InputKey.MouseWheelLeft:
                    return "Wheel Left";

                case InputKey.MouseWheelRight:
                    return "Wheel Right";

                default:
                    return key.ToString();
            }
        }
    }
}
