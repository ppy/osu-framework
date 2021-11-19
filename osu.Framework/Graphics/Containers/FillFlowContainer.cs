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

        private CrossVector spacingFactor(Drawable c)
        {
            Vector2 result = c.RelativeOriginPosition;
            if (c.Anchor.HasFlagFast(Anchor.x2))
                result.X = 1 - result.X;
            if (c.Anchor.HasFlagFast(Anchor.y2))
                result.Y = 1 - result.Y;
            return ToCross(result);
        }

        protected override IEnumerable<Vector2> ComputeLayoutPositions()
        {
            var max = ToCross(MaximumSize);

            if (MaximumSize == Vector2.Zero)
            {
                var s = ChildSize;

                // If we are autosize and haven't specified a maximum size, we should allow infinite expansion.
                // If we are inheriting then we need to use the parent size (our ActualSize).
                max = ToCross(new Vector2(
                    AutoSizeAxes.HasFlagFast(Axes.X) ? float.MaxValue : s.X,
                    AutoSizeAxes.HasFlagFast(Axes.Y) ? float.MaxValue : s.Y
                ));
            }

            var children = FlowingChildren.ToArray();
            if (children.Length == 0)
                yield break;

            // The positions for each child we will return later on.
            var layoutPositions = ArrayPool<CrossVector>.Shared.Rent(children.Length);

            // We need to keep track of span sizes such that we can compute correct
            // positions for centre anchor children.
            // We also store for each child to which span it belongs.
            int[] spanIndices = ArrayPool<int>.Shared.Rent(children.Length);

            var spanOffsetsToMiddle = new List<float> { 0 };

            // Variables keeping track of the current state while iterating over children
            // and computing initial flow positions.
            var current = CrossVector.Zero;
            var size = ToCross(children[0].BoundingBox.Size);
            float spanBeginOffset = spacingFactor(children[0]).Main * size.Main;
            float spanCrossSize = 0;
            var ourRelativeAnchor = children[0].RelativeAnchorPosition;

            // Defer the return of the rented lists
            try
            {
                for (int i = 0; i < children.Length; ++i)
                {
                    Drawable c = children[i];
                    validateChild(c, ourRelativeAnchor);

                    var spacingFactor = this.spacingFactor(c);
                    float spanMainSize = spanBeginOffset + current.Main + (1 - spacingFactor.Main) * size.Main;

                    // We've exceeded our allowed main size, move to a new span
                    if ((Direction.AffectedAxes() == Axes.Both && Precision.DefinitelyBigger(spanMainSize, max.Main)) || ForceNewSpan(c))
                    {
                        current.Main = 0;
                        current.Cross += spanCrossSize;

                        layoutPositions[i] = current;

                        spanOffsetsToMiddle.Add(0);
                        spanBeginOffset = spacingFactor.Main * size.Main;

                        spanCrossSize = 0;
                    }
                    else
                    {
                        layoutPositions[i] = current;

                        // Compute offset to the middle of the span, to be applied in case of centre anchor
                        // in a second pass.
                        spanOffsetsToMiddle[^1] = spanBeginOffset - spanMainSize / 2;
                    }

                    spanIndices[i] = spanOffsetsToMiddle.Count - 1;
                    var stride = CrossVector.Zero;

                    if (i < children.Length - 1)
                    {
                        // Compute stride. Note, that the stride depends on the origins of the drawables
                        // on both sides of the step to be taken.
                        stride = (CrossVector.One - spacingFactor) * size;

                        c = children[i + 1];
                        size = ToCross(c.BoundingBox.Size);

                        stride += this.spacingFactor(c) * size;
                    }

                    stride += ToCross(Spacing);

                    if (stride.Cross > spanCrossSize)
                        spanCrossSize = stride.Cross;
                    current.Main += stride.Main;
                }

                float crossSize = layoutPositions[children.Length - 1].Cross;

                // Second pass, adjusting the positions for anchors of children.
                // Uses span sizes and total size for centre-anchors.
                for (int i = 0; i < children.Length; i++)
                {
                    var c = children[i];

                    var layoutPosition = ToVector(layoutPositions[i]);

                    if (c.Anchor.HasFlagFast(Anchor.x2))
                        // Flow right-to-left
                        layoutPosition.X = -layoutPosition.X;
                    else if (c.Anchor.HasFlagFast(Anchor.x1))
                    {
                        layoutPosition.X += Direction.MainAxis() == Axes.X
                            // Begin flow at centre of current row
                            ? spanOffsetsToMiddle[spanIndices[i]]
                            // Begin flow at centre of total width
                            : -(crossSize / 2);
                    }

                    if (c.Anchor.HasFlagFast(Anchor.y2))
                        // Flow bottom-to-top
                        layoutPosition.Y = -layoutPosition.Y;
                    else if (c.Anchor.HasFlagFast(Anchor.y1))
                    {
                        layoutPosition.Y += Direction.MainAxis() == Axes.Y
                            // Begin flow at centre of current column
                            ? spanOffsetsToMiddle[spanIndices[i]]
                            // Begin flow at centre of total height
                            : -(crossSize / 2);
                    }

                    yield return layoutPosition;
                }
            }
            finally
            {
                ArrayPool<CrossVector>.Shared.Return(layoutPositions);
                ArrayPool<int>.Shared.Return(spanIndices);
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
        [Obsolete("Use ForceNewSpan instead")]
        protected virtual bool ForceNewRow(Drawable child) => false;

        /// <summary>
        /// Returns true if the given child should be placed on a new span, false otherwise. This will be called automatically for each child in this FillFlowContainers FlowingChildren-List.
        /// A span can refer to either row or column, depending on <see cref="Direction"/>.
        /// </summary>
        /// <param name="child">The child to check.</param>
        /// <returns>True if the given child should be placed on a new row, false otherwise.</returns>
#pragma warning disable CS0618 // Type or member is obsolete
        protected virtual bool ForceNewSpan(Drawable child) => ForceNewRow(child);
#pragma warning restore CS0618 // Type or member is obsolete

        /// <summary>
        /// An ad-hoc <see cref="Vector2"/> which uses a concept of "main" and "cross" axes rather than "x" and "y"
        /// in order to work with both XY and YX orientations.
        /// </summary>
        protected struct CrossVector
        {
            public float Main;
            public float Cross;

            public static CrossVector Zero { get; } = new CrossVector();
            public static CrossVector One { get; } = new CrossVector { Main = 1, Cross = 1 };

            public static CrossVector operator +(CrossVector a, CrossVector b)
                => new CrossVector
                {
                    Main = a.Main + b.Main,
                    Cross = a.Cross + b.Cross
                };

            public static CrossVector operator -(CrossVector a, CrossVector b)
                => new CrossVector
                {
                    Main = a.Main - b.Main,
                    Cross = a.Cross - b.Cross
                };

            public static CrossVector operator *(CrossVector a, CrossVector b)
                => new CrossVector
                {
                    Main = a.Main * b.Main,
                    Cross = a.Cross * b.Cross
                };
        }

        /// <summary>
        /// Converts a <see cref="Vector2"/> to a <see cref="CrossVector"/> with the current <see cref="FillDirection"/>.
        /// </summary>
        protected CrossVector ToCross(Vector2 vector)
        {
            if (Direction.MainAxis() == Axes.X)
            {
                return new CrossVector
                {
                    Main = vector.X,
                    Cross = vector.Y
                };
            }
            else
            {
                return new CrossVector
                {
                    Main = vector.Y,
                    Cross = vector.X
                };
            }
        }

        /// <summary>
        /// Converts a <see cref="CrossVector"/> to a <see cref="Vector2"/> assuming its orientation is the current <see cref="FillDirection"/>.
        /// </summary>
        protected Vector2 ToVector(CrossVector vector)
        {
            if (Direction.MainAxis() == Axes.X)
            {
                return new Vector2
                {
                    X = vector.Main,
                    Y = vector.Cross
                };
            }
            else
            {
                return new Vector2
                {
                    X = vector.Cross,
                    Y = vector.Main
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
        Full,

        /// <summary>
        /// Fill only horizontally.
        /// </summary>
        Horizontal,

        /// <summary>
        /// Fill only vertically.
        /// </summary>
        Vertical,

        /// <summary>
        /// Fill vertically first, then fill horizontally via multiple columns.
        /// </summary>
        FullVertical,
    }

    public static class FillDirectionExtensions
    {
        /// <summary>
        /// The used axes - if it's a "Full" direction, <see cref="Axes.Both"/>, otherwise, the main axis.
        /// </summary>
        public static Axes AffectedAxes(this FillDirection direction)
        {
            if (direction == FillDirection.Full || direction == FillDirection.FullVertical)
                return Axes.Both;
            else
                return direction.MainAxis();
        }

        /// <summary>
        /// The primary axis.
        /// </summary>
        public static Axes MainAxis(this FillDirection direction)
        {
            if (direction == FillDirection.Full || direction == FillDirection.Horizontal)
                return Axes.X;
            else
                return Axes.Y;
        }

        /// <summary>
        /// The secondary axis, orthogonal to the main axis.
        /// </summary>
        public static Axes CrossAxis(this FillDirection direction)
            => direction.MainAxis() == Axes.X ? Axes.Y : Axes.X;
    }
}
