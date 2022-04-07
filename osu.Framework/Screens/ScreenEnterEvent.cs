// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace osu.Framework.Screens
{
    public class ScreenEnterEvent : ScreenEvent
    {
        /// <summary>
        /// The <see cref="IScreen"/> which has suspended.
        /// </summary>
        public IScreen Last;

        public ScreenEnterEvent(IScreen last)
        {
            Last = last;
        }
    }
}
