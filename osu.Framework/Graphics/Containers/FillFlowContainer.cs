// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using osu.Framework.Extensions.EnumExtensions;
using osu.Framework.Utils;
using osuTK;

namespace osu.Framework.Graphics.Containers
{
    /// <summary>
    /// A <see cref="FlowContainer{Drawable}"/> that fills space by arranging its children
    /// next to each other.
    /// <see cref="Container{T}.Children"/> can be arranged horizontally, vertically, and in a
    /// combined fashion, which is controlled by <see cref="Direction"/>.
    /// <see cref="Container{T}.Children"/> are arranged from left-to-right if their
    /// <see cref="Drawable.Anchor"/> is to the left or centered horizontally.
    /// They are arranged from right-to-left otherwise.
    /// <see cref="Container{T}.Children"/> are arranged from top-to-bottom if their
    /// <see cref="Drawable.Anchor"/> is to the top or centered vertically.
    /// They are arranged from bottom-to-top otherwise.
    /// If non-<see cref="Drawable"/> <see cref="Container{T}.Children"/> are desired, use
    /// <see cref="FillFlowContainer{T}"/>.
    /// </summary>
    public class FillFlowContainer : FillFlowContainer<Drawable>
    {
    }

    /// <summary>
    /// A <see cref="FlowContainer{T}"/> that fills space by arranging its children
    /// next to each other.
    /// <see cref="Container{T}.Children"/> can be arranged horizontally, vertically, and in a
    /// combined fashion, which is controlled by <see cref="Direction"/>.
    /// <see cref="Container{T}.Children"/> are arranged from left-to-right if their
    /// <see cref="Drawable.Anchor"/> is to the left or centered horizontally.
    /// They are arranged from right-to-left otherwise.
    /// <see cref="Container{T}.Children"/> are arranged from top-to-bottom if their
    /// <see cref="Drawable.Anchor"/> is to the top or centered vertically.
    /// They are arranged from bottom-to-top otherwise.
    /// </summary>
    public class FillFlowContainer<T> : FlowContainer<T>, IFillFlowContainer where T : Drawable
    {
        private FillDirection direction = FillDirection.Full;

        /// <summary>
        /// If <see cref="FillDirection.Full"/> or <see cref="FillDirection.Horizontal"/>,
        /// <see cref="Container{T}.Children"/> are arranged from left-to-right if their
        /// <see cref="Drawable.Anchor"/> is to the left or centered horizontally.
        /// They are arranged from right-to-left otherwise.
        /// If <see cref="FillDirection.Full"/> or <see cref="FillDirection.Vertical"/>,
        /// <see cref="Container{T}.Children"/> are arranged from top-to-bottom if their
        /// <see cref="Drawable.Anchor"/> is to the top or centered vertically.
        /// They are arranged from bottom-to-top otherwise.
        /// </summary>
        public FillDirection Direction
        {
            get => direction;
            set
            {
                if (direction == value)
                    return;

                direction = value;
                InvalidateLayout();
            }
        }

        private Vector2 spacing;

        /// <summary>
        /// The spacing between individual elements. Default is <see cref="Vector2.Zero"/>.
        /// </summary>
        public Vector2 Spacing
        {
            get => spacing;
            set
            {
                if (spacing == value)
                    return;

                spacing = value;
                InvalidateLayout();
            }
        }

        private Vector2 spacingFactor(Drawable c)
        {
            Vector2 result = c.RelativeOriginPosition;
            if (c.Anchor.HasFlagFast(Anchor.x2))
                result.X = 1 - result.X;
            if (c.Anchor.HasFlagFast(Anchor.y2))
                result.Y = 1 - result.Y;
            return result;
        }

        protected override IEnumerable<Vector2> ComputeLayoutPositions()
        {
            var max = MaximumSize;

            if (max == Vector2.Zero)
            {
                var s = ChildSize;

                // If we are autosize and haven't specified a maximum size, we should allow infinite expansion.
                // If we are inheriting then we need to use the parent size (our ActualSize).
                max.X = AutoSizeAxes.HasFlagFast(Axes.X) ? float.MaxValue : s.X;
                max.Y = AutoSizeAxes.HasFlagFast(Axes.Y) ? float.MaxValue : s.Y;
            }

            var children = FlowingChildren.ToArray();
            if (children.Length == 0)
                yield break;

            // The positions for each child we will return later on.
            var layoutPositions = ArrayPool<Vector2>.Shared.Rent(children.Length);

            // We need to keep track of row widths such that we can compute correct
            // positions for horizontal centre anchor children.
            // We also store for each child to which row it belongs.
            int[] rowIndices = ArrayPool<int>.Shared.Rent(children.Length);

            var rowOffsetsToMiddle = new List<float> { 0 };

            // Variables keeping track of the current state while iterating over children
            // and computing initial flow positions.
            float rowHeight = 0;
            float rowBeginOffset = 0;
            var current = Vector2.Zero;

            // First pass, computing initial flow positions
            Vector2 size = Vector2.Zero;

            // defer the return of the rented lists
            try
            {
                for (int i = 0; i < children.Length; ++i)
                {
                    Drawable c = children[i];

                    static Axes toAxes(FillDirection direction)
                    {
                        switch (direction)
                        {
                            case FillDirection.Full:
                                return Axes.Both;

                            case FillDirection.Horizontal:
                                return Axes.X;

                            case FillDirection.Vertical:
                                return Axes.Y;

                            default:
                                throw new ArgumentException($"{direction.ToString()} is not defined");
                        }
                    }

                    // In some cases (see the right hand side of the conditional) we want to permit relatively sized children
                    // in our fill direction; specifically, when children use FillMode.Fit to preserve the aspect ratio.
                    // Consider the following use case: A fill flow container has a fixed width but an automatic height, and fills
                    // in the vertical direction. Now, we can add relatively sized children with FillMode.Fit to make sure their
                    // aspect ratio is preserved while still allowing them to fill vertically. This special case can not result
                    // in an autosize-related feedback loop, and we can thus simply allow it.
                    if ((c.RelativeSizeAxes & AutoSizeAxes & toAxes(Direction)) != 0
                        && (c.FillMode != FillMode.Fit || c.RelativeSizeAxes != Axes.Both || c.Size.X > RelativeChildSize.X
                            || c.Size.Y > RelativeChildSize.Y || AutoSizeAxes == Axes.Both))
                    {
                        throw new InvalidOperationException(
                            "Drawables inside a fill flow container may not have a relative size axis that the fill flow container is filling in and auto sizing for. " +
                            $"The fill flow container is set to flow in the {Direction} direction and autosize in {AutoSizeAxes} axes and the child is set to relative size in {c.RelativeSizeAxes} axes.");
                    }

                    // Populate running variables with sane initial values.
                    if (i == 0)
                    {
                        size = c.BoundingBox.Size;
                        rowBeginOffset = spacingFactor(c).X * size.X;
                    }

                    float rowWidth = rowBeginOffset + current.X + (1 - spacingFactor(c).X) * size.X;

                    //We've exceeded our allowed width, move to a new row
                    if (direction != FillDirection.Horizontal && (Precision.DefinitelyBigger(rowWidth, max.X) || direction == FillDirection.Vertical || ForceNewRow(c)))
                    {
                        current.X = 0;
                        current.Y += rowHeight;

                        layoutPositions[i] = current;

                        rowOffsetsToMiddle.Add(0);
                        rowBeginOffset = spacingFactor(c).X * size.X;

                        rowHeight = 0;
                    }
                    else
                    {
                        layoutPositions[i] = current;

                        // Compute offset to the middle of the row, to be applied in case of centre anchor
                        // in a second pass.
                        rowOffsetsToMiddle[^1] = rowBeginOffset - rowWidth / 2;
                    }

                    rowIndices[i] = rowOffsetsToMiddle.Count - 1;
                    Vector2 stride = Vector2.Zero;

                    if (i < children.Length - 1)
                    {
                        // Compute stride. Note, that the stride depends on the origins of the drawables
                        // on both sides of the step to be taken.
                        stride = (Vector2.One - spacingFactor(c)) * size;

                        c = children[i + 1];
                        size = c.BoundingBox.Size;

                        stride += spacingFactor(c) * size;
                    }

                    stride += Spacing;

                    if (stride.Y > rowHeight)
                        rowHeight = stride.Y;
                    current.X += stride.X;
                }

                float height = layoutPositions[children.Length - 1].Y;

                Vector2 ourRelativeAnchor = children[0].RelativeAnchorPosition;

                // Second pass, adjusting the positions for anchors of children.
                // Uses rowWidths and height for centre-anchors.
                for (int i = 0; i < children.Length; i++)
                {
                    var c = children[i];

                    switch (Direction)
                    {
                        case FillDirection.Vertical:
                            if (c.RelativeAnchorPosition.Y != ourRelativeAnchor.Y)
                            {
                                throw new InvalidOperationException(
                                    $"All drawables in a {nameof(FillFlowContainer)} must use the same RelativeAnchorPosition for the given {nameof(FillDirection)}({Direction}) ({ourRelativeAnchor.Y} != {c.RelativeAnchorPosition.Y}). "
                                    + $"Consider using multiple instances of {nameof(FillFlowContainer)} if this is intentional.");
                            }

                            break;

                        case FillDirection.Horizontal:
                            if (c.RelativeAnchorPosition.X != ourRelativeAnchor.X)
                            {
                                throw new InvalidOperationException(
                                    $"All drawables in a {nameof(FillFlowContainer)} must use the same RelativeAnchorPosition for the given {nameof(FillDirection)}({Direction}) ({ourRelativeAnchor.X} != {c.RelativeAnchorPosition.X}). "
                                    + $"Consider using multiple instances of {nameof(FillFlowContainer)} if this is intentional.");
                            }

                            break;

                        default:
                            if (c.RelativeAnchorPosition != ourRelativeAnchor)
                            {
                                throw new InvalidOperationException(
                                    $"All drawables in a {nameof(FillFlowContainer)} must use the same RelativeAnchorPosition for the given {nameof(FillDirection)}({Direction}) ({ourRelativeAnchor} != {c.RelativeAnchorPosition}). "
                                    + $"Consider using multiple instances of {nameof(FillFlowContainer)} if this is intentional.");
                            }

                            break;
                    }

                    var layoutPosition = layoutPositions[i];
                    if (c.Anchor.HasFlagFast(Anchor.x1))
                        // Begin flow at centre of row
                        layoutPosition.X += rowOffsetsToMiddle[rowIndices[i]];
                    else if (c.Anchor.HasFlagFast(Anchor.x2))
                        // Flow right-to-left
                        layoutPosition.X = -layoutPosition.X;

                    if (c.Anchor.HasFlagFast(Anchor.y1))
                        // Begin flow at centre of total height
                        layoutPosition.Y -= height / 2;
                    else if (c.Anchor.HasFlagFast(Anchor.y2))
                        // Flow bottom-to-top
                        layoutPosition.Y = -layoutPosition.Y;

                    yield return layoutPosition;
                }
            }
            finally
            {
                ArrayPool<Vector2>.Shared.Return(layoutPositions);
                ArrayPool<int>.Shared.Return(rowIndices);
            }
        }

        /// <summary>
        /// Returns true if the given child should be placed on a new row, false otherwise. This will be called automatically for each child in this FillFlowContainers FlowingChildren-List.
        /// </summary>
        /// <param name="child">The child to check.</param>
        /// <returns>True if the given child should be placed on a new row, false otherwise.</returns>
        protected virtual bool ForceNewRow(Drawable child) => false;
    }

    /// <summary>
    /// Represents the direction children of a <see cref="FillFlowContainer{T}"/> should be filled in.
    /// </summary>
    public enum FillDirection
    {
        /// <summary>
        /// Fill horizontally first, then fill vertically via multiple rows.
        /// </summary>
        Full,

        /// <summary>
        /// Fill only horizontally.
        /// </summary>
        Horizontal,

        /// <summary>
        /// Fill only vertically.
        /// </summary>
        Vertical
    }
}
