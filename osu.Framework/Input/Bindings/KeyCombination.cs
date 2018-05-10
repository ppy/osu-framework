// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Collections.Generic;
using System.Linq;
using OpenTK.Input;

namespace osu.Framework.Input.Bindings
{
    /// <summary>
    /// Represent a combination of more than one <see cref="InputKey"/>s.
    /// </summary>
    public class KeyCombination : IEquatable<KeyCombination>
    {
        /// <summary>
        /// The keys.
        /// </summary>
        public readonly IEnumerable<InputKey> Keys;

        /// <summary>
        /// Construct a new instance.
        /// </summary>
        /// <param name="keys">The keys.</param>
        public KeyCombination(IEnumerable<InputKey> keys)
        {
            Keys = keys.OrderBy(k => (int)k).ToArray();
        }

        /// <summary>
        /// Construct a new instance.
        /// </summary>
        /// <param name="keys">A comma-separated (KeyCode) string representation of the keys.</param>
        public KeyCombination(string keys)
            : this(keys.Split(',').Select(s => (InputKey)int.Parse(s)))
        {
        }

        /// <summary>
        /// Check whether the provided pressed keys are valid for this <see cref="KeyCombination"/>.
        /// </summary>
        /// <param name="pressedKeys">The potential pressed keys for this <see cref="KeyCombination"/>.</param>
        /// <param name="exact">Whether <paramref name="pressedKeys"/> should exactly match the keys required for this <see cref="KeyCombination"/>.</param>
        /// <returns>Whether the pressedKeys keys are valid.</returns>
        public bool IsPressed(KeyCombination pressedKeys, bool exact)
        {
            if (exact)
                return pressedKeys.Keys.Count() == Keys.Count() && pressedKeys.Keys.All(Keys.Contains);
            return !Keys.Except(pressedKeys.Keys).Any();
        }

        public bool Equals(KeyCombination other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Keys.SequenceEqual(other.Keys);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((KeyCombination)obj);
        }

        public override int GetHashCode() => Keys != null ? Keys.Select(b => b.GetHashCode()).Aggregate((h1, h2) => h1 * 17 + h2) : 0;

        public static implicit operator KeyCombination(InputKey singleKey) => new KeyCombination(new[] { singleKey });

        public static implicit operator KeyCombination(string stringRepresentation) => new KeyCombination(stringRepresentation);

        public static implicit operator KeyCombination(InputKey[] keys) => new KeyCombination(keys);

        public override string ToString() => Keys?.Select(b => ((int)b).ToString()).Aggregate((s1, s2) => $"{s1},{s2}") ?? string.Empty;

        public string ReadableString() => Keys?.Select(getReadableKey).Aggregate((s1, s2) => $"{s1}+{s2}") ?? string.Empty;

        private string getReadableKey(InputKey key)
        {
            if (key >= InputKey.FirstJoystickHatRightButton)
                return $"Joystick Hat {key - InputKey.FirstJoystickHatRightButton} Right";
            if (key >= InputKey.FirstJoystickHatLeftButton)
                return $"Joystick Hat {key - InputKey.FirstJoystickHatLeftButton} Left";
            if (key >= InputKey.FirstJoystickHatDownButton)
                return $"Joystick Hat {key - InputKey.FirstJoystickHatDownButton} Down";
            if (key >= InputKey.FirstJoystickHatUpButton)
                return $"Joystick Hat {key - InputKey.FirstJoystickHatUpButton} Up";
            if (key >= InputKey.FirstJoystickAxisPositiveButton)
                return $"Joystick Axis {key - InputKey.FirstJoystickAxisPositiveButton} +";
            if (key >= InputKey.FirstJoystickAxisNegativeButton)
                return $"Joystick Axis {key - InputKey.FirstJoystickAxisNegativeButton} -";
            if (key >= InputKey.FirstJoystickButton)
                return $"Joystick {key - InputKey.FirstJoystickButton}";

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
                case InputKey.Escape:
                    return "Esc";
                case InputKey.BackSpace:
                    return "Backsp";
                case InputKey.Insert:
                    return "Ins";
                case InputKey.Delete:
                    return "Del";
                case InputKey.PageUp:
                    return "Pgup";
                case InputKey.PageDown:
                    return "Pgdn";
                case InputKey.CapsLock:
                    return "Caps";
                case InputKey.Number0:
                case InputKey.Keypad0:
                    return "0";
                case InputKey.Number1:
                case InputKey.Keypad1:
                    return "1";
                case InputKey.Number2:
                case InputKey.Keypad2:
                    return "2";
                case InputKey.Number3:
                case InputKey.Keypad3:
                    return "3";
                case InputKey.Number4:
                case InputKey.Keypad4:
                    return "4";
                case InputKey.Number5:
                case InputKey.Keypad5:
                    return "5";
                case InputKey.Number6:
                case InputKey.Keypad6:
                    return "6";
                case InputKey.Number7:
                case InputKey.Keypad7:
                    return "7";
                case InputKey.Number8:
                case InputKey.Keypad8:
                    return "8";
                case InputKey.Number9:
                case InputKey.Keypad9:
                    return "9";
                case InputKey.Tilde:
                    return "~";
                case InputKey.Minus:
                    return "-";
                case InputKey.Plus:
                    return "+";
                case InputKey.BracketLeft:
                    return "(";
                case InputKey.BracketRight:
                    return ")";
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
                case InputKey.MouseLeft:
                    return "M1";
                case InputKey.MouseMiddle:
                    return "M3";
                case InputKey.MouseRight:
                    return "M2";
                case InputKey.MouseButton1:
                    return "M4";
                case InputKey.MouseButton2:
                    return "M5";
                case InputKey.MouseButton3:
                    return "M6";
                case InputKey.MouseButton4:
                    return "M7";
                case InputKey.MouseButton5:
                    return "M8";
                case InputKey.MouseButton6:
                    return "M9";
                case InputKey.MouseButton7:
                    return "M10";
                case InputKey.MouseButton8:
                    return "M11";
                case InputKey.MouseButton9:
                    return "M12";
                case InputKey.MouseWheelDown:
                    return "Wheel Down";
                case InputKey.MouseWheelUp:
                    return "Wheel Up";
                default:
                    return key.ToString();
            }
        }

        public static InputKey FromKey(Key key)
        {
            switch (key)
            {
                case Key.RShift:
                    return InputKey.Shift;
                case Key.RAlt:
                    return InputKey.Alt;
                case Key.RControl:
                    return InputKey.Control;
                case Key.RWin:
                    return InputKey.Super;
            }

            return (InputKey)key;
        }

        public static InputKey FromMouseButton(MouseButton button) => (InputKey)((int)InputKey.FirstMouseButton + button);

        public static InputKey FromJoystickButton(JoystickButton button)
        {
            if (button >= JoystickButton.FirstHatRight)
                return InputKey.FirstJoystickHatRightButton + (button - JoystickButton.FirstHatRight);
            if (button >= JoystickButton.FirstHatLeft)
                return InputKey.FirstJoystickHatLeftButton + (button - JoystickButton.FirstHatLeft);
            if (button >= JoystickButton.FirstHatDown)
                return InputKey.FirstJoystickHatDownButton + (button - JoystickButton.FirstHatDown);
            if (button >= JoystickButton.FirstHatUp)
                return InputKey.FirstJoystickHatUpButton + (button - JoystickButton.FirstHatUp);
            if (button >= JoystickButton.FirstAxisPositive)
                return InputKey.FirstJoystickAxisPositiveButton + (button - JoystickButton.FirstAxisPositive);
            if (button >= JoystickButton.FirstAxisNegative)
                return InputKey.FirstJoystickAxisNegativeButton + (button - JoystickButton.FirstAxisNegative);
            return InputKey.FirstJoystickButton + (int)button;
        }

        public static KeyCombination FromInputState(InputState state)
        {
            List<InputKey> keys = new List<InputKey>();

            if (state.Mouse != null)
            {
                foreach (var button in state.Mouse.Buttons)
                    keys.Add(FromMouseButton(button));

                if (state.Mouse.WheelDelta > 0) keys.Add(InputKey.MouseWheelUp);
                if (state.Mouse.WheelDelta < 0) keys.Add(InputKey.MouseWheelDown);
            }

            if (state.Keyboard != null)
            {
                foreach (var key in state.Keyboard.Keys)
                {
                    InputKey iKey = FromKey(key);

                    switch (key)
                    {
                        case Key.LShift:
                        case Key.RShift:
                        case Key.LAlt:
                        case Key.RAlt:
                        case Key.LControl:
                        case Key.RControl:
                        case Key.LWin:
                        case Key.RWin:
                            if (!keys.Contains(iKey))
                                keys.Add(iKey);
                            break;
                        default:
                            keys.Add(iKey);
                            break;
                    }
                }
            }

            if (state.Joystick != null)
                keys.AddRange(state.Joystick.Buttons.Select(FromJoystickButton));

            return new KeyCombination(keys);
        }
    }
}
