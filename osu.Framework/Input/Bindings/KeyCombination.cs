// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using osu.Framework.Extensions;
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
        public KeyCombination(ICollection<InputKey>? keys)
        {
            if (keys == null || keys.Count == 0)
            {
                Keys = none;
                return;
            }

            var keyBuilder = ImmutableArray.CreateBuilder<InputKey>(keys.Count);

            bool hadDuplicates = false;

            foreach (var key in keys)
            {
                if (keyBuilder.Contains(key))
                {
                    // This changes the expected count meaning we can't use the optimised MoveToImmutable() method.
                    hadDuplicates = true;
                    continue;
                }

                keyBuilder.Add(key);
            }

            keyBuilder.Sort();

            Keys = hadDuplicates ? keyBuilder.ToImmutableArray() : keyBuilder.MoveToImmutable();
        }

        /// <summary>
        /// Construct a new instance.
        /// </summary>
        /// <param name="keys">The keys.</param>
        /// <remarks>This constructor is not optimized. Hot paths are assumed to use <see cref="FromInputState(InputState, Vector2?)"/>.</remarks>
        public KeyCombination(params InputKey[] keys)
            : this((ICollection<InputKey>)keys)
        {
        }

        /// <summary>
        /// Construct a new instance from string representation provided by <see cref="ToString"/>.
        /// </summary>
        /// <param name="keys">A comma-separated (KeyCode in integer) string representation of the keys.</param>
        /// <remarks>This constructor is not optimized. Hot paths are assumed to use <see cref="FromInputState(InputState, Vector2?)"/>.</remarks>
        public KeyCombination(string keys)
            : this(keys.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(s => (InputKey)int.Parse(s)).ToArray())
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
        /// <param name="inputState">The current input state.</param>
        /// <param name="matchingMode">The method for handling exact key matches.</param>
        /// <returns>Whether the pressedKeys keys are valid.</returns>
        public bool IsPressed(KeyCombination pressedKeys, InputState inputState, KeyCombinationMatchingMode matchingMode)
        {
            Debug.Assert(!pressedKeys.Keys.Contains(InputKey.None)); // Having None in pressed keys will break IsPressed

            if (Keys == pressedKeys.Keys) // Fast test for reference equality of underlying array
                return true;

            if (Keys.SequenceEqual(none))
                return false;

            return ContainsAll(Keys, pressedKeys.Keys, matchingMode);
        }

        private static InputKey? getVirtualKey(InputKey key)
        {
            switch (key)
            {
                case InputKey.LShift:
                case InputKey.RShift:
                    return InputKey.Shift;

                case InputKey.LControl:
                case InputKey.RControl:
                    return InputKey.Control;

                case InputKey.LAlt:
                case InputKey.RAlt:
                    return InputKey.Alt;

                case InputKey.LSuper:
                case InputKey.RSuper:
                    return InputKey.Super;
            }

            return null;
        }

        /// <summary>
        /// Check whether the provided set of pressed keys matches the candidate binding.
        /// </summary>
        /// <param name="candidateKeyBinding">The candidate key binding to match against.</param>
        /// <param name="pressedPhysicalKeys">The keys which have been pressed by a user.</param>
        /// <param name="matchingMode">The matching mode to be used when checking.</param>
        /// <returns>Whether this is a match.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool ContainsAll(ImmutableArray<InputKey> candidateKeyBinding, ImmutableArray<InputKey> pressedPhysicalKeys, KeyCombinationMatchingMode matchingMode)
        {
            Debug.Assert(pressedPhysicalKeys.All(k => k.IsPhysical()));

            // first, check that all the candidate keys are contained in the provided pressed keys.
            // regardless of the matching mode, every key needs to at least be present (matching modes only change
            // the behaviour of excess keys).
            foreach (var key in candidateKeyBinding)
            {
                if (!IsPressed(pressedPhysicalKeys, key))
                    return false;
            }

            switch (matchingMode)
            {
                case KeyCombinationMatchingMode.Exact:
                    foreach (var key in pressedPhysicalKeys)
                    {
                        // in exact matching mode, every pressed key needs to be in the candidate.
                        if (!KeyBindingContains(candidateKeyBinding, key))
                            return false;
                    }

                    break;

                case KeyCombinationMatchingMode.Modifiers:
                    foreach (var key in pressedPhysicalKeys)
                    {
                        // in modifiers match mode, the same check applies as exact but only for modifier keys.
                        if (IsModifierKey(key) && !KeyBindingContains(candidateKeyBinding, key))
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
        /// </summary>
        /// <param name="candidateKeyBinding">The candidate key binding to match against.</param>
        /// <param name="physicalKey">The physical key that has been pressed.</param>
        /// <returns>Whether this is a match.</returns>
        internal static bool KeyBindingContains(ImmutableArray<InputKey> candidateKeyBinding, InputKey physicalKey)
        {
            return candidateKeyBinding.Contains(physicalKey) ||
                   (getVirtualKey(physicalKey) is InputKey vKey && candidateKeyBinding.Contains(vKey));
        }

        /// <summary>
        /// Check whether a single physical or virtual key from a candidate binding is relevant to the currently pressed keys.
        /// </summary>
        /// <param name="pressedPhysicalKeys">The currently pressed keys to match against.</param>
        /// <param name="candidateKey">The candidate key to check.</param>
        /// <returns>Whether this is a match.</returns>
        internal static bool IsPressed(ImmutableArray<InputKey> pressedPhysicalKeys, InputKey candidateKey)
        {
            if (candidateKey.IsPhysical())
                return pressedPhysicalKeys.Contains(candidateKey);

            Debug.Assert(candidateKey.IsVirtual());

            foreach (var pk in pressedPhysicalKeys)
            {
                if (getVirtualKey(pk) == candidateKey)
                    return true;
            }

            return false;
        }

        public bool Equals(KeyCombination other) => Keys.SequenceEqual(other.Keys);

        public override bool Equals(object? obj) => obj is KeyCombination kc && Equals(kc);

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
                yield return InputKey.MouseWheelLeft;

            if (scrollDelta.X < 0)
                yield return InputKey.MouseWheelRight;
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

            // Can't use `MoveToImmutable` here as we don't provide accurate capacity.
            return new KeyCombination(keys.ToImmutableList());
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
