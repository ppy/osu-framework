// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using osu.Framework.Input.States;

namespace osu.Framework.Input.StateChanges
{
    public class MidiKeyInput : ButtonInput<MidiKey>
    {
        public readonly IReadOnlyDictionary<MidiKey, byte> Velocities;

        public MidiKeyInput(MidiKey button, byte velocity, bool isPressed)
            : base(button, isPressed)
        {
            Velocities = new Dictionary<MidiKey, byte> { [button] = velocity };
        }

        public MidiKeyInput(MidiState currentState, MidiState previousState)
            : base(currentState.Keys, previousState.Keys)
        {
            // newer velocities always take precedence
            // if the newer midi state doesn't specify a velocity for a key, it will be preserved after Apply()
            Velocities = new Dictionary<MidiKey, byte>(currentState.Velocities);
        }

        protected override ButtonStates<MidiKey> GetButtonStates(InputState state) => state.Midi.Keys;

        public override void Apply(InputState state, IInputStateChangeHandler handler)
        {
            foreach (var (key, velocity) in Velocities)
                state.Midi.Velocities[key] = velocity;

            base.Apply(state, handler);
        }
    }
}
