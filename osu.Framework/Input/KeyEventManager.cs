// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System.Collections.Generic;
using System.Linq;
using osu.Framework.Graphics;
using osu.Framework.Input.Events;
using osu.Framework.Input.States;
using osuTK.Input;

namespace osu.Framework.Input
{
    /// <summary>
    /// Manages state events for a single key.
    /// </summary>
    public class KeyEventManager : ButtonEventManager<Key>
    {
        public KeyEventManager(Key key)
            : base(key)
        {
        }

        public void HandleRepeat(InputState state)
        {
            // Only drawables that can still handle input should handle the repeat
            var drawables = ButtonDownInputQueue.Intersect(InputQueue).Where(t => t.IsAlive && t.IsPresent);

            PropagateButtonEvent(drawables, new KeyDownEvent(state, Button, true));
        }

        protected override Drawable HandleButtonDown(InputState state, List<Drawable> targets) => PropagateButtonEvent(targets, new KeyDownEvent(state, Button));

        protected override void HandleButtonUp(InputState state, List<Drawable> targets)
        {
            if (targets == null)
                return;

            PropagateButtonEvent(targets, new KeyUpEvent(state, Button));
        }
    }
}
