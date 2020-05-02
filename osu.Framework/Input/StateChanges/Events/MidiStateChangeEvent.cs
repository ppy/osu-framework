// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Input.States;

namespace osu.Framework.Input.StateChanges.Events
{
    public class MidiStateChangeEvent : ButtonStateChangeEvent<MidiKey>
    {
        public MidiStateChangeEvent(InputState state, IInput input, MidiKey button, ButtonStateChangeKind kind, byte velocity)
            : base(state, input, button, kind)
        {
            state.Midi.Velocities[button] = kind == ButtonStateChangeKind.Pressed ? velocity : (byte)0;
        }
    }
}
