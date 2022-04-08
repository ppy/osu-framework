// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace osu.Framework.Screens
{
    public class ScreenSuspendEvent : ScreenEvent
    {
        /// <summary>
        /// The <see cref="IScreen"/> that will be entered next.
        /// </summary>
        public IScreen Next;

        public ScreenSuspendEvent(IScreen next)
        {
            Next = next;
        }
    }
}
