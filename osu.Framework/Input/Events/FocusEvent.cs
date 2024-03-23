// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using osu.Framework.Graphics;
using osu.Framework.Input.States;

namespace osu.Framework.Input.Events
{
    /// <summary>
    /// An event representing that a drawable gained the focus.
    /// </summary>
    public class FocusEvent : UIEvent
    {
        /// <summary>
        /// A list of <see cref="Drawable"/>s that lost focus. The list is empty if nothing was previously focused.
        /// </summary>
        public readonly IReadOnlyList<Drawable> PreviouslyFocused;

        public FocusEvent(InputState state, IReadOnlyList<Drawable> previouslyFocused)
            : base(state)
        {
            PreviouslyFocused = previouslyFocused;
        }
    }
}
