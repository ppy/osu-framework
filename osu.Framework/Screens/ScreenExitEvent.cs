// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

namespace osu.Framework.Screens
{
    /// <summary>
    /// Denotes a screen exit event.
    /// </summary>
    public class ScreenExitEvent : ScreenTransitionEvent
    {
        /// <summary>
        /// The final <see cref="IScreen"/> of this exit operation.
        /// </summary>
        public IScreen Destination { get; }

        public ScreenExitEvent(IScreen last, IScreen next, IScreen destination)
            : base(last, next)
        {
            Destination = destination;
        }
    }
}
