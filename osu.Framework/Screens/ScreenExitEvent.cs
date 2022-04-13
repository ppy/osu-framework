// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace osu.Framework.Screens
{
    public class ScreenExitEvent : ScreenEvent
    {
        /// <summary>
        /// The <see cref="IScreen"/> that will be resumed next.
        /// </summary>
        public IScreen Next;

        /// <summary>
        /// The final <see cref="IScreen"/> of this exit operation.
        /// </summary>
        public IScreen Destination;

        public ScreenExitEvent(IScreen next, IScreen destination)
        {
            Next = next;
            Destination = destination;
        }
    }
}
