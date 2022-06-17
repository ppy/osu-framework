// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using osu.Framework.Bindables;
using osu.Framework.Graphics.Primitives;

namespace osu.Framework.Graphics.Containers
{
    /// <summary>
    /// Containers that implement this interface act as a target for any child <see cref="SafeAreaContainer"/>s.
    /// </summary>
    public interface ISafeArea : IContainer
    {
        /// <summary>
        /// The <see cref="RectangleF"/> that defines the non-safe size which can be overriden into using <see cref="SafeAreaContainer.SafeAreaOverrideEdges"/>s.
        /// </summary>
        RectangleF AvailableNonSafeSpace { get; }

        /// <summary>
        /// The padding which should be applied to confine a child to the safe area.
        /// </summary>
        BindableSafeArea SafeAreaPadding { get; }

        /// <summary>
        /// Returns the full non-safe space rectangle in the coordinate space of the passed <see cref="IDrawable"/>.
        /// </summary>
        /// <param name="other">The target <see cref="IDrawable"/> for coordinate space translation.</param>
        Quad ExpandRectangleToSpaceOfOtherDrawable(IDrawable other);
    }
}
