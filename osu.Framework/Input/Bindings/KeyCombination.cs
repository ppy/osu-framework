// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using osu.Framework.Input.States;
using osuTK;
using osuTK.Input;

namespace osu.Framework.Input.Bindings
{
    /// <summary>
    /// Represent a combination of more than one <see cref="InputKey"/>s.
    /// </summary>
    public readonly struct KeyCombination : IEquatable<KeyCombination>
    {
        /// <summary>
        /// The keys.
        /// </summary>
        public readonly ImmutableArray<InputKey> Keys;

        private static readonly ImmutableArray<InputKey> none = ImmutableArray.Create(InputKey.None);

        /// <summary>
        /// Construct a new instance.
        /// </summary>
        /// <param name="keys">The keys.</param>
        /// <remarks>This constructor is not optimized. Hot paths are assumed to use <see cref="FromInputState(InputState, Vector2?)"/>.</remarks>
        public KeyCombination(IEnumerable<InputKey> keys)
        {
            Keys = keys?.Any() == true ? keys.Distinct().OrderBy(k => (int)k).ToImmutableArray() : none;
        }

        /// <summary>
        /// Construct a new instance.
        /// </summary>
        /// <param name="keys">The keys.</param>
        /// <remarks>This constructor is not optimized. Hot paths are assumed to use <see cref="FromInputState(InputState, Vector2?)"/>.</remarks>
        public KeyCombination(params InputKey[] keys)
            : this(keys.AsEnumerable())
        {
        }

        /// <summary>
        /// Construct a new instance from string representation provided by <see cref="ToString"/>.
        /// </summary>
        /// <param name="keys">A comma-separated (KeyCode in integer) string representation of the keys.</param>
        /// <remarks>This constructor is not optimized. Hot paths are assumed to use <see cref="FromInputState(InputState, Vector2?)"/>.</remarks>
        public KeyCombination(string keys)
            : this(keys.Split(',').Select(s => (InputKey)int.Parse(s)))
        {
        }

        /// <summary>
        /// Constructor optimized for known builder. The caller is responsible to sort it.
        /// </summary>
        /// <param name="keys">The already sorted <see cref="ImmutableArray{InputKey}"/>.</param>
        private KeyCombination(ImmutableArray<InputKey> keys)
        {
            Keys = keys;
        }

        /// <summary>
        /// Check whether the provided pressed keys are valid for this <see cref="KeyCombination"/>.
        /// </summary>
        /// <param name="pressedKeys">The potential pressed keys for this <see cref="KeyCombination"/>.</param>
        /// <param name="matchingMode">The method for handling exact key matches.</param>
        /// <returns>Whether the pressedKeys keys are valid.</returns>
        public bool IsPressed(KeyCombination pressedKeys, KeyCombinationMatchingMode matchingMode)
        {
            Debug.Assert(!pressedKeys.Keys.Contains(InputKey.None)); // Having None in pressed keys will break IsPressed

            if (Keys == pressedKeys.Keys) // Fast test for reference equality of underlying array
                return true;

            switch (matchingMode)
            {
                case KeyCombinationMatchingMode.Any:
                    return containsAll(pressedKeys.Keys, Keys, false);

                case KeyCombinationMatchingMode.Exact:
                    // Keys are always ordered
                    return pressedKeys.Keys.SequenceEqual(Keys);

                case KeyCombinationMatchingMode.Modifiers:
                    return containsAll(pressedKeys.Keys, Keys, true);

                default:
                    return false;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool containsAll(ImmutableArray<InputKey> pressedKey, ImmutableArray<InputKey> candidateKey, bool exactModifiers)
        {
            // can be local function once attribute on local functions are implemented
            // optimized to avoid allocation
            // Usually Keys.Count <= 3. Does not worth special logic for Contains().
            foreach (var key in candidateKey)
            {
                if (!pressedKey.Contains(key))
                    return false;
            }

            if (exactModifiers)
            {
                foreach (var key in pressedKey)
                {
                    if (IsModifierKey(key) &&
                        !candidateKey.Contains(key))
                        return false;
                }
            }

            return true;
        }

        public bool Equals(KeyCombination other) => Keys.SequenceEqual(other.Keys);

        public override bool Equals(object obj) => obj is KeyCombination kc && Equals(kc);

        public override int GetHashCode()
        {
            int hash = 0;
            foreach (var key in Keys)
                hash = hash * 17 + (int)key;
            return hash;
        }

        public static implicit operator KeyCombination(InputKey singleKey) => new KeyCombination(ImmutableArray.Create(singleKey));

        public static implicit operator KeyCombination(string stringRepresentation) => new KeyCombination(stringRepresentation);

        public static implicit operator KeyCombination(InputKey[] keys) => new KeyCombination(keys);

        /// <summary>
        /// Get a string representation can be used with <see cref="KeyCombination(string)"/>.
        /// </summary>
        /// <returns>The string representation.</returns>
        public override string ToString() => string.Join(",", Keys.Select(k => (int)k));

        public string ReadableString() => string.Join(" ", Keys.Select(getReadableKey));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsModifierKey(InputKey key) => key == InputKey.Control || key == InputKey.Shift || key == InputKey.Alt || key == InputKey.Super;

        private string getReadableKey(InputKey key)
        {
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

            return InputKey.FirstJoystickButton + (button - JoystickButton.FirstButton);
        }

        public static InputKey FromScrollDelta(Vector2 scrollDelta)
        {
            if (scrollDelta.Y > 0) return InputKey.MouseWheelUp;
            if (scrollDelta.Y < 0) return InputKey.MouseWheelDown;

            return InputKey.None;
        }

        /// <summary>
        /// Construct a new instance from input state.
        /// </summary>
        /// <param name="state">The input state object.</param>
        /// <param name="scrollDelta">Delta of scroller's position.</param>
        /// <returns>The new constructed <see cref="KeyCombination"/> instance.</returns>
        /// <remarks>This factory method is optimized and should be used for hot paths.</remarks>
        public static KeyCombination FromInputState(InputState state, Vector2? scrollDelta = null)
        {
            var keys = ImmutableArray.CreateBuilder<InputKey>();

            if (state.Mouse != null)
            {
                foreach (var button in state.Mouse.Buttons)
                    keys.Add(FromMouseButton(button));
            }

            if (scrollDelta is Vector2 v && v.Y != 0)
                keys.Add(FromScrollDelta(v));

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
            {
                foreach (var joystickButton in state.Joystick.Buttons)
                    keys.Add(FromJoystickButton(joystickButton));
            }

            Debug.Assert(!keys.Contains(InputKey.None)); // Having None in pressed keys will break IsPressed
            keys.Sort();
            return new KeyCombination(keys.ToImmutable());
        }
    }

    public enum KeyCombinationMatchingMode
    {
        /// <summary>
        /// Matches a <see cref="KeyCombination"/> regardless of any additional key presses.
        /// </summary>
        Any,

        /// <summary>
        /// Matches a <see cref="KeyCombination"/> if there are no additional key presses.
        /// </summary>
        Exact,

        /// <summary>
        /// Matches a <see cref="KeyCombination"/> regardless of any additional key presses, however key modifiers must match exactly.
        /// </summary>
        Modifiers,
    }
}
