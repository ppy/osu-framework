// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using JetBrains.Annotations;
using osu.Framework.Input.States;

namespace osu.Framework.Input.Events
{
    public class MidiDownEvent : MidiEvent
    {
        public MidiDownEvent([NotNull] InputState state, MidiKey key, byte velocity)
            : base(state, key, velocity)
        {
        }
    }
}
