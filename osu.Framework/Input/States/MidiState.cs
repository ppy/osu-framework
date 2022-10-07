// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System.Collections.Generic;

namespace osu.Framework.Input.States
{
    public class MidiState
    {
        public readonly ButtonStates<MidiKey> Keys = new ButtonStates<MidiKey>();
        public readonly Dictionary<MidiKey, byte> Velocities = new Dictionary<MidiKey, byte>();
    }
}
