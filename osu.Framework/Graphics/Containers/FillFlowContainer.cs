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
        private FillDirection direction = FillDirection.FullHorizontal;

        /// <summary>
        /// If <see cref="FillDirection.FullHorizontal"/>, <see cref="FillDirection.FullVertical"/> or <see cref="FillDirection.Horizontal"/>,
        /// <see cref="Container{T}.Children"/> are arranged from left-to-right if their
        /// <see cref="Drawable.Anchor"/> is to the left or centered horizontally.
        /// They are arranged from right-to-left otherwise.
        /// If <see cref="FillDirection.FullHorizontal"/>, <see cref="FillDirection.FullVertical"/> or <see cref="FillDirection.Vertical"/>,
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

        private FlowVector calculateSpacingFactor(Drawable c)
        {
            Vector2 result = c.RelativeOriginPosition;
            if (c.Anchor.HasFlagFast(Anchor.x2))
                result.X = 1 - result.X;
            if (c.Anchor.HasFlagFast(Anchor.y2))
                result.Y = 1 - result.Y;
            return ToFlowVector(result);
        }

        protected override IEnumerable<Vector2> ComputeLayoutPositions()
        {
            var max = ToFlowVector(MaximumSize);

            if (MaximumSize == Vector2.Zero)
            {
                var s = ChildSize;

                // If we are autosize and haven't specified a maximum size, we should allow infinite expansion.
                // If we are inheriting then we need to use the parent size (our ActualSize).
                max = ToFlowVector(new Vector2(
                    AutoSizeAxes.HasFlagFast(Axes.X) ? float.MaxValue : s.X,
                    AutoSizeAxes.HasFlagFast(Axes.Y) ? float.MaxValue : s.Y
                ));
            }

            var children = FlowingChildren.ToArray();
            if (children.Length == 0)
                yield break;

            // The positions for each child we will return later on.
            var layoutPositions = ArrayPool<FlowVector>.Shared.Rent(children.Length);

            // We need to keep track of line sizes such that we can compute correct
            // positions for centre anchor children.
            // We also store for each child to which line it belongs.
            int[] lineIndices = ArrayPool<int>.Shared.Rent(children.Length);

            var lineOffsetsToMiddle = new List<float> { 0 };

            // Variables keeping track of the current state while iterating over children
            // and computing initial flow positions.
            var current = FlowVector.Zero;
            var size = ToFlowVector(children[0].BoundingBox.Size);
            float lineBeginOffset = calculateSpacingFactor(children[0]).Flow * size.Flow;
            float lineLineSize = 0;
            var ourRelativeAnchor = children[0].RelativeAnchorPosition;

            // Defer the return of the rented lists
            try
            {
                for (int i = 0; i < children.Length; ++i)
                {
                    Drawable c = children[i];
                    validateChild(c, ourRelativeAnchor);

                    var spacingFactor = calculateSpacingFactor(c);
                    float lineFlowSize = lineBeginOffset + current.Flow + (1 - spacingFactor.Flow) * size.Flow;

                    // We've exceeded our allowed flow size, move to a new line
                    if ((Direction.AffectedAxes() == Axes.Both && Precision.DefinitelyBigger(lineFlowSize, max.Flow)) || ForceNewLine(c))
                    {
                        current.Flow = 0;
                        current.Line += lineLineSize;

                        layoutPositions[i] = current;

                        lineOffsetsToMiddle.Add(0);
                        lineBeginOffset = spacingFactor.Flow * size.Flow;

                        lineLineSize = 0;
                    }
                    else
                    {
                        layoutPositions[i] = current;

                        // Compute offset to the middle of the line, to be applied in case of centre anchor
                        // in a second pass.
                        lineOffsetsToMiddle[^1] = lineBeginOffset - lineFlowSize / 2;
                    }

                    lineIndices[i] = lineOffsetsToMiddle.Count - 1;
                    var stride = FlowVector.Zero;

                    if (i < children.Length - 1)
                    {
                        // Compute stride. Note, that the stride depends on the origins of the drawables
                        // on both sides of the step to be taken.
                        stride = (FlowVector.One - spacingFactor) * size;

                        c = children[i + 1];
                        size = ToFlowVector(c.BoundingBox.Size);

                        stride += spacingFactor * size;
                    }

                    stride += ToFlowVector(Spacing);

                    if (stride.Line > lineLineSize)
                        lineLineSize = stride.Line;
                    current.Flow += stride.Flow;
                }

                float lineSize = layoutPositions[children.Length - 1].Line;

                // Second pass, adjusting the positions for anchors of children.
                // Uses line sizes and total flow size for centre-anchors.
                for (int i = 0; i < children.Length; i++)
                {
                    var c = children[i];

                    var layoutPosition = ToVector(layoutPositions[i]);

                    if (c.Anchor.HasFlagFast(Anchor.x2))
                        // Flow right-to-left
                        layoutPosition.X = -layoutPosition.X;
                    else if (c.Anchor.HasFlagFast(Anchor.x1))
                    {
                        layoutPosition.X += Direction.FlowAxis() == Axes.X
                            // Begin flow at centre of current row
                            ? lineOffsetsToMiddle[lineIndices[i]]
                            // Begin flow at centre of total width
                            : -(lineSize / 2);
                    }

                    if (c.Anchor.HasFlagFast(Anchor.y2))
                        // Flow bottom-to-top
                        layoutPosition.Y = -layoutPosition.Y;
                    else if (c.Anchor.HasFlagFast(Anchor.y1))
                    {
                        layoutPosition.Y += Direction.FlowAxis() == Axes.Y
                            // Begin flow at centre of current column
                            ? lineOffsetsToMiddle[lineIndices[i]]
                            // Begin flow at centre of total height
                            : -(lineSize / 2);
                    }

                    yield return layoutPosition;
                }
            }
            finally
            {
                ArrayPool<FlowVector>.Shared.Return(layoutPositions);
                ArrayPool<int>.Shared.Return(lineIndices);
            }
        }

        private void validateChild(Drawable c, Vector2 ourRelativeAnchor)
        {
            // In some cases (see the right hand side of the conditional) we want to permit relatively sized children
            // in our fill direction; specifically, when children use FillMode.Fit to preserve the aspect ratio.
            // Consider the following use case: A fill flow container has a fixed width but an automatic height, and fills
            // in the vertical direction. Now, we can add relatively sized children with FillMode.Fit to make sure their
            // aspect ratio is preserved while still allowing them to fill vertically. This special case can not result
            // in an autosize-related feedback loop, and we can thus simply allow it.
            if ((c.RelativeSizeAxes & AutoSizeAxes & Direction.AffectedAxes()) != 0
                && (c.FillMode != FillMode.Fit || c.RelativeSizeAxes != Axes.Both || c.Size.X > RelativeChildSize.X
                    || c.Size.Y > RelativeChildSize.Y || AutoSizeAxes == Axes.Both))
            {
                throw new InvalidOperationException(
                    "Drawables inside a fill flow container may not have a relative size axis that the fill flow container is filling in and auto sizing for. " +
                    $"The fill flow container is set to flow in the {Direction} direction and autosize in {AutoSizeAxes} axes and the child is set to relative size in {c.RelativeSizeAxes} axes.");
            }

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
        }

        /// <summary>
        /// Returns true if the given child should be placed on a new row, false otherwise. This will be called automatically for each child in this FillFlowContainers FlowingChildren-List.
        /// </summary>
        /// <param name="child">The child to check.</param>
        /// <returns>True if the given child should be placed on a new row, false otherwise.</returns>
        [Obsolete("Use ForceNewLine instead")] // Can be removed 20220520
        protected virtual bool ForceNewRow(Drawable child) => false;

        /// <summary>
        /// Returns true if the given child should be placed on a new line, false otherwise. This will be called automatically for each child in this FillFlowContainers FlowingChildren-List.
        /// A line can refer to either row or column, depending on <see cref="Direction"/>.
        /// </summary>
        /// <param name="child">The child to check.</param>
        /// <returns>True if the given child should be placed on a new line, false otherwise.</returns>
#pragma warning disable CS0618 // Type or member is obsolete
        protected virtual bool ForceNewLine(Drawable child) => ForceNewRow(child);
#pragma warning restore CS0618 // Type or member is obsolete

        /// <summary>
        /// Converts a <see cref="Vector2"/> to a <see cref="FlowVector"/> with the current <see cref="FillDirection"/>.
        /// </summary>
        protected FlowVector ToFlowVector(Vector2 vector)
        {
            if (Direction.FlowAxis() == Axes.X)
            {
                return new FlowVector
                {
                    Flow = vector.X,
                    Line = vector.Y
                };
            }
            else
            {
                return new FlowVector
                {
                    Flow = vector.Y,
                    Line = vector.X
                };
            }
        }

        /// <summary>
        /// Converts a <see cref="FlowVector"/> to a <see cref="Vector2"/> assuming its orientation is the current <see cref="FillDirection"/>.
        /// </summary>
        protected Vector2 ToVector(FlowVector vector)
        {
            if (Direction.FlowAxis() == Axes.X)
            {
                return new Vector2
                {
                    X = vector.Flow,
                    Y = vector.Line
                };
            }
            else
            {
                return new Vector2
                {
                    X = vector.Line,
                    Y = vector.Flow
                };
            }
        }
    }

    /// <summary>
    /// Represents the direction children of a <see cref="FillFlowContainer{T}"/> should be filled in.
    /// </summary>
    public enum FillDirection
    {
        /// <summary>
        /// Fill horizontally first, then fill vertically via multiple rows.
        /// </summary>
        FullHorizontal,

        /// <summary>
        /// Fill vertically first, then fill horizontally via multiple columns.
        /// </summary>
        FullVertical,

        /// <summary>
        /// Fill only horizontally.
        /// </summary>
        Horizontal,

        /// <summary>
        /// Fill only vertically.
        /// </summary>
        Vertical,

        /// <summary>
        /// Fill horizontally first, then fill vertically via multiple rows.
        /// </summary>
        [Obsolete("Use FullHorizontal instead")] // Can be removed 20220520
        Full = FullHorizontal
    }

    public static class FillDirectionExtensions
    {
        /// <summary>
        /// The used axes - if it's a "Full" direction, <see cref="Axes.Both"/>, otherwise, the flow axis.
        /// </summary>
        public static Axes AffectedAxes(this FillDirection direction)
        {
            if (direction == FillDirection.FullHorizontal || direction == FillDirection.FullVertical)
                return Axes.Both;
            else
                return direction.FlowAxis();
        }

        /// <summary>
        /// The primary axis.
        /// </summary>
        public static Axes FlowAxis(this FillDirection direction)
        {
            if (direction == FillDirection.FullHorizontal || direction == FillDirection.Horizontal)
                return Axes.X;
            else
                return Axes.Y;
        }

        /// <summary>
        /// The secondary axis, orthogonal to the flow axis.
        /// </summary>
        public static Axes LineAxis(this FillDirection direction)
            => direction.FlowAxis() == Axes.X ? Axes.Y : Axes.X;
    }
}
