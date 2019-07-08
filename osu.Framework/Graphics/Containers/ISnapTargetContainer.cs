// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics.Primitives;

namespace osu.Framework.Graphics.Containers
{
    /// <summary>
    /// Containers that implement this interface act as a target for any child <see cref="EdgeSnappingContainer"/>s.
    /// </summary>
    public interface ISnapTargetContainer : IContainer
    {
        /// <summary>
        /// The <see cref="RectangleF"/> that should be snapped to by any <see cref="EdgeSnappingContainer"/>s.
        /// </summary>
        RectangleF SnapRectangle { get; }

        /// <summary>
        /// Returns the snapping rectangle in the coordinate space of the passed <see cref="IDrawable"/>.
        /// </summary>
        /// <param name="other">The target <see cref="IDrawable"/> for coordinate space translation.</param>
        Quad SnapRectangleToSpaceOfOtherDrawable(IDrawable other);
    }
}
