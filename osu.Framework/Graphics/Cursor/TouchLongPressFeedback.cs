// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics.Containers;

namespace osu.Framework.Graphics.Cursor
{
    public abstract partial class TouchLongPressFeedback : CompositeDrawable
    {
        /// <summary>
        /// Begins long-press animation with the specified duration.
        /// </summary>
        /// <param name="duration">The animation duration.</param>
        public abstract void BeginAnimation(double duration);

        /// <summary>
        /// Cancels an ongoing long-press animation.
        /// </summary>
        public abstract void CancelAnimation();
    }
}
