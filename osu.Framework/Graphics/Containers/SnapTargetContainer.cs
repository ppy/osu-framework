// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Graphics.Primitives;

namespace osu.Framework.Graphics.Containers
{
    public class SnapTargetContainer : SnapTargetContainer<Drawable>
    {
    }

    /// <summary>
    /// A <see cref="Container{T}"/> that acts a target for <see cref="EdgeSnappingContainer{T}"/>s.
    /// It is automatically cached as <see cref="ISnapTargetContainer"/> so that it may be resolved by any
    /// child <see cref="EdgeSnappingContainer{T}"/>s.
    /// </summary>
    [Cached(typeof(ISnapTargetContainer))]
    public class SnapTargetContainer<T> : Container<T>, ISnapTargetContainer
        where T : Drawable
    {
        public virtual RectangleF SnapRectangle => DrawRectangle;

        public Quad SnapRectangleToSpaceOfOtherDrawable(IDrawable other) => ToSpaceOfOtherDrawable(SnapRectangle, other);
    }
}
