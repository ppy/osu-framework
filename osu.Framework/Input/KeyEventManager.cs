// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using osu.Framework.Graphics;
using osu.Framework.Input.Events;
using osu.Framework.Input.States;
using osuTK.Input;

namespace osu.Framework.Input
{
    /// <summary>
    /// Manages state events for a single key.
    /// </summary>
    public class KeyEventManager : ButtonEventManager
    {
        /// <summary>
        /// The key this manager manages.
        /// </summary>
        public readonly Key Key;

        public KeyEventManager(Key key)
        {
            Key = key;
        }

        public void HandleRepeat(InputState state) => PropagateButtonEvent(ButtonDownInputQueue, new KeyDownEvent(state, Key, true));

        protected override Drawable HandleButtonDownInternal(InputState state, List<Drawable> targets) => PropagateButtonEvent(targets, new KeyDownEvent(state, Key));

        protected override void HandleButtonUpInternal(InputState state, List<Drawable> targets)
        {
            if (targets == null)
                return;

            PropagateButtonEvent(targets, new KeyUpEvent(state, Key));
        }
    }
}
