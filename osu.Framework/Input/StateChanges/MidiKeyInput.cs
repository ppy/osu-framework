// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Input.StateChanges.Events;
using osu.Framework.Input.States;

namespace osu.Framework.Input.StateChanges
{
    public class MidiKeyInput : IInput
    {
        public readonly MidiKey Key;
        public readonly byte Velocity;
        public readonly bool IsPressed;

        public MidiKeyInput(MidiKey button, bool isPressed)
            : this(button, 0, isPressed)
        {
        }

        public MidiKeyInput(MidiKey button, byte velocity, bool isPressed)
        {
            Key = button;
            Velocity = velocity;
            IsPressed = isPressed;
        }

        public void Apply(InputState state, IInputStateChangeHandler handler)
        {
            handler.HandleInputStateChange(IsPressed
                ? new MidiStateChangeEvent(state, this, Key, ButtonStateChangeKind.Pressed, Velocity)
                : new MidiStateChangeEvent(state, this, Key, ButtonStateChangeKind.Released, 0));
        }
    }
}
