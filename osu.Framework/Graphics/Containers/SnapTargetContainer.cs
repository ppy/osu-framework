// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Graphics.Primitives;

namespace osu.Framework.Graphics.Containers
{
    /// <summary>
    /// A <see cref="Container"/> that acts a target for <see cref="EdgeSnappingContainer"/>s.
    /// It is automatically cached as <see cref="ISnapTargetContainer"/> so that it may be resolved by any
    /// child <see cref="EdgeSnappingContainer"/>s.
    /// </summary>
    [Cached(typeof(ISnapTargetContainer))]
    public class SnapTargetContainer : Container<Drawable>, ISnapTargetContainer
    {
        public virtual RectangleF SnapRectangle => DrawRectangle;

        public Quad SnapRectangleToSpaceOfOtherDrawable(IDrawable other) => ToSpaceOfOtherDrawable(SnapRectangle, other);
    }
}
