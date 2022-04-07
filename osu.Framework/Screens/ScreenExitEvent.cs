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

        public ScreenExitEvent(IScreen next)
        {
            Next = next;
        }
    }
}
