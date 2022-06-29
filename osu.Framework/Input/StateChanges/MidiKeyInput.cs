// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System.Collections.Generic;
using System.Linq;
using osu.Framework.Input.States;

namespace osu.Framework.Input.StateChanges
{
    /// <summary>
    /// Represents an input from a MIDI device.
    /// </summary>
    public class MidiKeyInput : ButtonInput<MidiKey>
    {
        /// <summary>
        /// A mapping of <see cref="MidiKey"/>s to the velocities they were pressed or released with.
        /// </summary>
        public readonly IReadOnlyList<(MidiKey, byte)> Velocities;

        /// <summary>
        /// Creates a <see cref="MidiKeyInput"/> for a single key state.
        /// </summary>
        /// <param name="button">The <see cref="MidiKey"/> whose state changed.</param>
        /// <param name="velocity">The velocity with which with the input was performed.</param>
        /// <param name="isPressed">Whether <paramref name="button"/> was pressed or released.</param>
        public MidiKeyInput(MidiKey button, byte velocity, bool isPressed)
            : base(button, isPressed)
        {
            Velocities = new List<(MidiKey, byte)> { (button, velocity) };
        }

        /// <summary>
        /// Creates a <see cref="MidiKeyInput"/> from the difference of two <see cref="MidiState"/>s.
        /// </summary>
        /// <remarks>
        /// Buttons that are pressed in <paramref name="previousState"/> and not pressed in <paramref name="currentState"/> will be listed as <see cref="ButtonStateChangeKind.Released"/>.
        /// Buttons that are not pressed in <paramref name="previousState"/> and pressed in <paramref name="currentState"/> will be listed as <see cref="ButtonStateChangeKind.Pressed"/>.
        /// Key velocities from <paramref name="currentState"/> always take precedence over velocities from <paramref name="previousState"/>.
        /// </remarks>
        /// <param name="currentState">The newer <see cref="MidiState"/>.</param>
        /// <param name="previousState">The older <see cref="MidiState"/>.</param>
        public MidiKeyInput(MidiState currentState, MidiState previousState)
            : base(currentState.Keys, previousState.Keys)
        {
            // newer velocities always take precedence
            // if the newer midi state doesn't specify a velocity for a key, it will be preserved after Apply()
            Velocities = currentState.Velocities.Select(entry => (entry.Key, entry.Value)).ToList();
        }

        /// <summary>
        /// Retrieves the <see cref="ButtonStates{TButton}"/> from a <see cref="MidiKeyInput"/>.
        /// </summary>
        protected override ButtonStates<MidiKey> GetButtonStates(InputState state) => state.Midi.Keys;

        public override void Apply(InputState state, IInputStateChangeHandler handler)
        {
            foreach ((var key, byte velocity) in Velocities)
                state.Midi.Velocities[key] = velocity;

            base.Apply(state, handler);
        }
    }
}
