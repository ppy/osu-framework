// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using JetBrains.Annotations;
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
        /// The <see cref="Drawable"/> that has lost focus, or <c>null</c> if nothing was previously focused.
        /// </summary>
        [CanBeNull]
        public readonly Drawable PreviouslyFocused;

        public FocusEvent(InputState state, Drawable previouslyFocused)
            : base(state)
        {
            PreviouslyFocused = previouslyFocused;
        }
    }
}
