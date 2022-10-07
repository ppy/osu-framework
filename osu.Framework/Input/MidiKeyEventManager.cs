// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System.Collections.Generic;
using osu.Framework.Graphics;
using osu.Framework.Input.Events;
using osu.Framework.Input.States;

namespace osu.Framework.Input
{
    public class MidiKeyEventManager : ButtonEventManager<MidiKey>
    {
        public MidiKeyEventManager(MidiKey button)
            : base(button)
        {
        }

        protected override Drawable HandleButtonDown(InputState state, List<Drawable> targets) => PropagateButtonEvent(targets, new MidiDownEvent(state, Button, state.Midi.Velocities[Button]));

        protected override void HandleButtonUp(InputState state, List<Drawable> targets)
        {
            if (targets == null)
                return;

            PropagateButtonEvent(targets, new MidiUpEvent(state, Button));
        }
    }
}
