// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using osu.Framework.Input.StateChanges.Events;
using osu.Framework.Input.States;

namespace osu.Framework.Input.StateChanges
{
    public class MidiKeyInput : ButtonInput<MidiKey>
    {
        public MidiKeyInput(IEnumerable<ButtonInputEntry<MidiKey>> entries)
            : base(entries)
        {
        }

        public MidiKeyInput(MidiKey button, bool isPressed)
            : base(button, isPressed)
        {
        }

        public MidiKeyInput(ButtonStates<MidiKey> current, ButtonStates<MidiKey> previous)
            : base(current, previous)
        {
        }

        protected override ButtonStateChangeEvent<MidiKey> CreateEvent(InputState state, MidiKey button, ButtonStateChangeKind kind) => new MidiStateChangeEvent(state, this, button, kind);

        protected override ButtonStates<MidiKey> GetButtonStates(InputState state) => state.Midi.Keys;
    }
}
