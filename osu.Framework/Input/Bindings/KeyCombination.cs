// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using osu.Framework.Extensions.EnumExtensions;
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
            : this(keys.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(s => (InputKey)int.Parse(s)))
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

            return ContainsAll(Keys, pressedKeys.Keys, matchingMode);
        }

        /// <summary>
        /// Check whether the provided set of pressed keys matches the candidate binding.
        /// </summary>
        /// <param name="candidateKey">The candidate key binding to match against.</param>
        /// <param name="pressedKey">The keys which have been pressed by a user.</param>
        /// <param name="matchingMode">The matching mode to be used when checking.</param>
        /// <returns>Whether this is a match.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool ContainsAll(ImmutableArray<InputKey> candidateKey, ImmutableArray<InputKey> pressedKey, KeyCombinationMatchingMode matchingMode)
        {
            // first, check that all the candidate keys are contained in the provided pressed keys.
            // regardless of the matching mode, every key needs to at least be present (matching modes only change
            // the behaviour of excess keys).
            foreach (var key in candidateKey)
            {
                if (!ContainsKey(pressedKey, key))
                    return false;
            }

            switch (matchingMode)
            {
                case KeyCombinationMatchingMode.Exact:
                    foreach (var key in pressedKey)
                    {
                        // in exact matching mode, every pressed key needs to be in the candidate.
                        if (!ContainsKeyPermissive(candidateKey, key))
                            return false;
                    }

                    break;

                case KeyCombinationMatchingMode.Modifiers:
                    foreach (var key in pressedKey)
                    {
                        // in modifiers match mode, the same check applies as exact but only for modifier keys.
                        if (IsModifierKey(key) && !ContainsKeyPermissive(candidateKey, key))
                            return false;
                    }

                    break;

                case KeyCombinationMatchingMode.Any:
                    // any match mode needs no further checks.
                    break;
            }

            return true;
        }

        /// <summary>
        /// Check whether the provided key is part of the candidate binding.
        /// This will match bidirectionally for modifier keys (LShift and Shift being present in both of the two parameters in either order will return true).
        /// </summary>
        /// <param name="candidate">The candidate key binding to match against.</param>
        /// <param name="key">The key which has been pressed by a user.</param>
        /// <returns>Whether this is a match.</returns>
        internal static bool ContainsKeyPermissive(ImmutableArray<InputKey> candidate, InputKey key)
        {
            switch (key)
            {
                case InputKey.LControl:
                case InputKey.RControl:
                    if (candidate.Contains(InputKey.Control))
                        return true;

                    break;

                case InputKey.LShift:
                case InputKey.RShift:
                    if (candidate.Contains(InputKey.Shift))
                        return true;

                    break;

                case InputKey.RAlt:
                case InputKey.LAlt:
                    if (candidate.Contains(InputKey.Alt))
                        return true;

                    break;

                case InputKey.LSuper:
                case InputKey.RSuper:
                    if (candidate.Contains(InputKey.Super))
                        return true;

                    break;
            }

            return ContainsKey(candidate, key);
        }

        /// <summary>
        /// Check whether a single key from a candidate binding is relevant to the currently pressed keys.
        /// If the <paramref name="key"/> contains a left/right specific modifier, the <paramref name="candidate"/> must also for this to match.
        /// </summary>
        /// <param name="candidate">The candidate key binding to match against.</param>
        /// <param name="key">The key which has been pressed by a user.</param>
        /// <returns>Whether this is a match.</returns>
        internal static bool ContainsKey(ImmutableArray<InputKey> candidate, InputKey key)
        {
            switch (key)
            {
                case InputKey.Control:
                    if (candidate.Contains(InputKey.LControl) || candidate.Contains(InputKey.RControl))
                        return true;

                    break;

                case InputKey.Shift:
                    if (candidate.Contains(InputKey.LShift) || candidate.Contains(InputKey.RShift))
                        return true;

                    break;

                case InputKey.Alt:
                    if (candidate.Contains(InputKey.LAlt) || candidate.Contains(InputKey.RAlt))
                        return true;

                    break;

                case InputKey.Super:
                    if (candidate.Contains(InputKey.LSuper) || candidate.Contains(InputKey.RSuper))
                        return true;

                    break;
            }

            return candidate.Contains(key);
        }

        public bool Equals(KeyCombination other) => Keys.SequenceEqual(other.Keys);

        public override bool Equals(object obj) => obj is KeyCombination kc && Equals(kc);

        public override int GetHashCode()
        {
            var hash = new HashCode();
            foreach (var key in Keys)
                hash.Add(key);
            return hash.ToHashCode();
        }

        public static implicit operator KeyCombination(InputKey singleKey) => new KeyCombination(ImmutableArray.Create(singleKey));

        public static implicit operator KeyCombination(string stringRepresentation) => new KeyCombination(stringRepresentation);

        public static implicit operator KeyCombination(InputKey[] keys) => new KeyCombination(keys);

        /// <summary>
        /// Get a string representation can be used with <see cref="KeyCombination(string)"/>.
        /// </summary>
        /// <returns>The string representation.</returns>
        public override string ToString() => string.Join(',', Keys.Select(k => (int)k));

        [Obsolete("Resolve ReadableKeyCombinationProvider with DI and use ReadableKeyCombinationProvider.GetReadableString(KeyCombination)")] // Can be removed 20220424
        public string ReadableString()
        {
            var sortedKeys = Keys.GetValuesInOrder().ToArray();

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

                return getReadableKey(key);
            }).Where(s => !string.IsNullOrEmpty(s)));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsModifierKey(InputKey key)
        {
            switch (key)
            {
                case InputKey.LControl:
                case InputKey.LShift:
                case InputKey.LAlt:
                case InputKey.LSuper:
                case InputKey.RControl:
                case InputKey.RShift:
                case InputKey.RAlt:
                case InputKey.RSuper:
                case InputKey.Control:
                case InputKey.Shift:
                case InputKey.Alt:
                case InputKey.Super:
                    return true;
            }

            return false;
        }

        private static string getReadableKey(InputKey key)
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

        public static InputKey FromKey(Key key)
        {
            switch (key)
            {
                case Key.LShift: return InputKey.LShift;

                case Key.RShift: return InputKey.RShift;

                case Key.LControl: return InputKey.LControl;

                case Key.RControl: return InputKey.RControl;

                case Key.LAlt: return InputKey.LAlt;

                case Key.RAlt: return InputKey.RAlt;

                case Key.LWin: return InputKey.LSuper;

                case Key.RWin: return InputKey.RSuper;
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

        public static IEnumerable<InputKey> FromScrollDelta(Vector2 scrollDelta)
        {
            if (scrollDelta.Y > 0)
                yield return InputKey.MouseWheelUp;

            if (scrollDelta.Y < 0)
                yield return InputKey.MouseWheelDown;

            if (scrollDelta.X > 0)
                yield return InputKey.MouseWheelRight;

            if (scrollDelta.X < 0)
                yield return InputKey.MouseWheelLeft;
        }

        public static InputKey FromMidiKey(MidiKey key) => (InputKey)((int)InputKey.MidiA0 + key - MidiKey.A0);

        public static InputKey FromTabletPenButton(TabletPenButton penButton) => (InputKey)((int)InputKey.FirstTabletPenButton + penButton);

        public static InputKey FromTabletAuxiliaryButton(TabletAuxiliaryButton auxiliaryButton) => (InputKey)((int)InputKey.FirstTabletAuxiliaryButton + auxiliaryButton);

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

            if (scrollDelta is Vector2 v && (v.X != 0 || v.Y != 0))
                keys.AddRange(FromScrollDelta(v));

            if (state.Keyboard != null)
            {
                foreach (var key in state.Keyboard.Keys)
                {
                    var iKey = FromKey(key);

                    if (!keys.Contains(iKey))
                        keys.Add(iKey);
                }
            }

            if (state.Joystick != null)
            {
                foreach (var joystickButton in state.Joystick.Buttons)
                    keys.Add(FromJoystickButton(joystickButton));
            }

            if (state.Midi != null)
                keys.AddRange(state.Midi.Keys.Select(FromMidiKey));

            if (state.Tablet != null)
            {
                keys.AddRange(state.Tablet.PenButtons.Select(FromTabletPenButton));
                keys.AddRange(state.Tablet.AuxiliaryButtons.Select(FromTabletAuxiliaryButton));
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
