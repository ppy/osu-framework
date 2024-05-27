﻿// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using osu.Framework.Extensions;
using osu.Framework.Input.States;
using osuTK.Input;
using KeyboardState = osu.Framework.Input.States.KeyboardState;

namespace osu.Framework.Input.StateChanges
{
    public class KeyboardKeyInput : ButtonInput<Key>
    {
        public readonly IReadOnlyList<(Key, char?)> Characters;

        public KeyboardKeyInput(IEnumerable<ButtonInputEntry<Key>> entries)
            : base(entries)
        {
            Characters = Entries.Select(entry => (entry.Button, entry.Button.GetDefaultCharacter())).ToList();
        }

        public KeyboardKeyInput(KeyboardKey key, bool isPressed)
            : base(key.Key, isPressed)
        {
            if (isPressed)
                Characters = new List<(Key, char?)> { (key.Key, key.Character) };
            else
                // if the character changes between the first press and the release, don't update and use the original character for consistency
                Characters = ImmutableList<(Key, char?)>.Empty;
        }

        public KeyboardKeyInput(KeyboardState current, KeyboardState previous)
            : base(current.Keys, previous.Keys)
        {
            Characters = current.Characters.Select(entry => (entry.Key, entry.Value)).ToList();
        }

        protected override ButtonStates<Key> GetButtonStates(InputState state) => state.Keyboard.Keys;

        public override void Apply(InputState state, IInputStateChangeHandler handler)
        {
            foreach ((Key key, char? character) in Characters)
                state.Keyboard.Characters[key] = character;

            base.Apply(state, handler);
        }
    }
}
