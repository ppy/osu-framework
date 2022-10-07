// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System.Collections.Generic;
using JetBrains.Annotations;
using osu.Framework.Extensions.TypeExtensions;
using osu.Framework.Input.States;

namespace osu.Framework.Input.Events
{
    public abstract class MidiEvent : UIEvent
    {
        public readonly MidiKey Key;
        public readonly byte Velocity;

        /// <summary>
        /// Whether a specific key is pressed.
        /// </summary>
        public bool IsPressed(MidiKey key) => CurrentState.Midi.Keys.IsPressed(key);

        /// <summary>
        /// Whether any key is pressed.
        /// </summary>
        public bool HasAnyKeyPressed => CurrentState.Midi.Keys.HasAnyButtonPressed;

        /// <summary>
        /// List of currently pressed keys.
        /// </summary>
        public IEnumerable<MidiKey> PressedKeys => CurrentState.Midi.Keys;

        protected MidiEvent([NotNull] InputState state, MidiKey key, byte velocity)
            : base(state)
        {
            Key = key;
            Velocity = velocity;
        }

        public override string ToString() => $"{GetType().ReadableName()}({Key})";
    }
}
