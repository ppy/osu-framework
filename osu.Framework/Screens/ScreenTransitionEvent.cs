// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

namespace osu.Framework.Screens
{
    /// <summary>
    /// Denotes a screen transition event.
    /// </summary>
    public class ScreenTransitionEvent
    {
        /// <summary>
        /// The <see cref="IScreen"/> which has been transitioned from.
        /// </summary>
        public IScreen Last { get; }

        /// <summary>
        /// The <see cref="IScreen"/> which has been transitioned to.
        /// </summary>
        public IScreen Next { get; }

        public ScreenTransitionEvent(IScreen last, IScreen next)
        {
            Last = last;
            Next = next;
        }
    }
}
