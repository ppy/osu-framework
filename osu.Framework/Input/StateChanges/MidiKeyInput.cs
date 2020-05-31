// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Input.States;

namespace osu.Framework.Input.StateChanges
{
    public class MidiKeyInput : ButtonInput<MidiKey>
    {
        public readonly MidiKey Button;
        public readonly byte Velocity;
        public readonly bool IsPressed;

        public MidiKeyInput(MidiKey button, byte velocity, bool isPressed)
            : base(button, isPressed)
        {
            Button = button;
            Velocity = velocity;
            IsPressed = isPressed;
        }

        protected override ButtonStates<MidiKey> GetButtonStates(InputState state) => state.Midi.Keys;

        public override void Apply(InputState state, IInputStateChangeHandler handler)
        {
            state.Midi.Velocities[Button] = Velocity;

            base.Apply(state, handler);
        }
    }
}
