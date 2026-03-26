// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Diagnostics;
using osu.Framework.Input.States;
using osuTK.Input;

namespace osu.Framework.Input.StateChanges
{
    public class KeyboardKeyInput : ButtonInput<Key>
    {
        /// <summary>
        /// High-precision wall-clock timestamp captured at the moment the key event was received
        /// by the InputThread. Used for sub-frame timing correction in judgment.
        /// Value from <see cref="Stopwatch.GetTimestamp()"/>.
        /// </summary>
        public long WallTimestamp { get; init; }

        public KeyboardKeyInput(IEnumerable<ButtonInputEntry<Key>> entries)
            : base(entries)
        {
        }

        public KeyboardKeyInput(Key button, bool isPressed)
            : base(button, isPressed)
        {
            WallTimestamp = Stopwatch.GetTimestamp();
        }

        public KeyboardKeyInput(ButtonStates<Key>? current, ButtonStates<Key>? previous)
            : base(current, previous)
        {
        }

        protected override ButtonStates<Key> GetButtonStates(InputState state) => state.Keyboard.Keys;
    }
}
