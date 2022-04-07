// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace osu.Framework.Screens
{
    public class ScreenSuspendEvent : ScreenEvent
    {
        /// <summary>
        /// The new Screen.
        /// </summary>
        public IScreen Next;

        public ScreenSuspendEvent(IScreen next)
        {
            Next = next;
        }
    }
}
