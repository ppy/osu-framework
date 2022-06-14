// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using JetBrains.Annotations;
using osu.Framework.Graphics;
using osu.Framework.Input.States;

namespace osu.Framework.Input.Events
{
    /// <summary>
    /// An event representing that a drawable lost the focus.
    /// </summary>
    public class FocusLostEvent : UIEvent
    {
        /// <summary>
        /// The <see cref="Drawable"/> that will gain focus, or <c>null</c> if nothing will gain focus.
        /// </summary>
        [CanBeNull]
        public readonly Drawable NextFocused;

        public FocusLostEvent(InputState state, Drawable nextFocused)
            : base(state)
        {
            NextFocused = nextFocused;
        }
    }
}
