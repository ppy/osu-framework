// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Graphics.Primitives;

namespace osu.Framework.Graphics.Containers
{
    public interface ISnapTargetContainer : IContainer
    {
        RectangleF SnapRectangle { get; }

        Quad SnapRectangleToSpaceOfOtherDrawable(IDrawable other);
    }

    [Cached(Type = typeof(ISnapTargetContainer))]
    public class SnapTargetContainer : SnapTargetContainer<Drawable>
    {
    }

    /// <summary>
    /// A <see cref="Container{T}"/> that acts a target for <see cref="EdgeSnappingContainer{T}"/>s.
    /// It is automatically cached as <see cref="ISnapTargetContainer"/> so that it may be resolved by any
    /// child <see cref="EdgeSnappingContainer{T}"/>s.
    /// </summary>
    [Cached(Type = typeof(ISnapTargetContainer))]
    public class SnapTargetContainer<T> : Container<T>, ISnapTargetContainer
        where T : Drawable
    {
        /// <summary>
        /// The <see cref="RectangleF"/> that should be snapped to by any <see cref="EdgeSnappingContainer{T}"/>s.
        /// </summary>
        public virtual RectangleF SnapRectangle => DrawRectangle;

        /// <summary>
        /// Returns the snapping rectangle in the coordinate space of the passed <see cref="IDrawable"/>.
        /// </summary>
        /// <param name="other">The target <see cref="IDrawable"/> for coordinate space translation.</param>
        public Quad SnapRectangleToSpaceOfOtherDrawable(IDrawable other) => ToSpaceOfOtherDrawable(SnapRectangle, other);
    }
}
