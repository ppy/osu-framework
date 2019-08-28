// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace osu.Framework.Input.States
{
    public class MidiState
    {
        public ButtonStates<MidiKey> Keys { get; private set; } = new ButtonStates<MidiKey>();

        public MidiState Clone()
        {
            var clone = (MidiState)MemberwiseClone();
            clone.Keys = Keys.Clone();

            return clone;
        }
    }
}
