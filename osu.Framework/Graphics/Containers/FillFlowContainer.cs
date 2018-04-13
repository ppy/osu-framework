// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Collections.Generic;
using OpenTK;
using System.Linq;
using osu.Framework.MathUtils;

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
            get { return direction; }
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
            get { return spacing; }
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
            if ((c.Anchor & Anchor.x2) > 0)
                result.X = 1 - result.X;
            if ((c.Anchor & Anchor.y2) > 0)
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
                max.X = (AutoSizeAxes & Axes.X) > 0 ? float.MaxValue : s.X;
                max.Y = (AutoSizeAxes & Axes.Y) > 0 ? float.MaxValue : s.Y;
            }

            var children = FlowingChildren.ToArray();
            if (children.Length == 0)
                return new List<Vector2>();

            // The positions for each child we will return later on.
            Vector2[] result = new Vector2[children.Length];

            // We need to keep track of row widths such that we can compute correct
            // positions for horizontal centre anchor children.
            // We also store for each child to which row it belongs.
            int[] rowIndices = new int[children.Length];
            List<float> rowOffsetsToMiddle = new List<float> { 0 };

            // Variables keeping track of the current state while iterating over children
            // and computing initial flow positions.
            float rowHeight = 0;
            float rowBeginOffset = 0;
            var current = Vector2.Zero;

            // First pass, computing initial flow positions
            Vector2 size = Vector2.Zero;
            for (int i = 0; i < children.Length; ++i)
            {
                Drawable c = children[i];

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

                    result[i] = current;

                    rowOffsetsToMiddle.Add(0);
                    rowBeginOffset = spacingFactor(c).X * size.X;

                    rowHeight = 0;
                }
                else
                {
                    result[i] = current;

                    // Compute offset to the middle of the row, to be applied in case of centre anchor
                    // in a second pass.
                    rowOffsetsToMiddle[rowOffsetsToMiddle.Count - 1] = rowBeginOffset - rowWidth / 2;
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

            float height = result.Last().Y;

            Vector2 ourRelativeAnchor = children[0].RelativeAnchorPosition;

            // Second pass, adjusting the positions for anchors of children.
            // Uses rowWidths and height for centre-anchors.
            for (int i = 0; i < children.Length; ++i)
            {
                var c = children[i];

                switch (Direction)
                {
                    case FillDirection.Vertical:
                        if (c.RelativeAnchorPosition.Y != ourRelativeAnchor.Y)
                            throw new InvalidOperationException(
                                $"All drawables in a {nameof(FillFlowContainer)} must use the same RelativeAnchorPosition for the given {nameof(FillDirection)}({Direction}) ({ourRelativeAnchor.Y} != {c.RelativeAnchorPosition.Y}). "
                                + $"Consider using multiple instances of {nameof(FillFlowContainer)} if this is intentional.");
                        break;
                    case FillDirection.Horizontal:
                        if (c.RelativeAnchorPosition.X != ourRelativeAnchor.X)
                            throw new InvalidOperationException(
                                $"All drawables in a {nameof(FillFlowContainer)} must use the same RelativeAnchorPosition for the given {nameof(FillDirection)}({Direction}) ({ourRelativeAnchor.X} != {c.RelativeAnchorPosition.X}). "
                                + $"Consider using multiple instances of {nameof(FillFlowContainer)} if this is intentional.");
                        break;
                    default:
                        if (c.RelativeAnchorPosition != ourRelativeAnchor)
                            throw new InvalidOperationException(
                                $"All drawables in a {nameof(FillFlowContainer)} must use the same RelativeAnchorPosition for the given {nameof(FillDirection)}({Direction}) ({ourRelativeAnchor} != {c.RelativeAnchorPosition}). "
                                + $"Consider using multiple instances of {nameof(FillFlowContainer)} if this is intentional.");
                        break;
                }

                if ((c.Anchor & Anchor.x1) > 0)
                    // Begin flow at centre of row
                    result[i].X += rowOffsetsToMiddle[rowIndices[i]];
                else if ((c.Anchor & Anchor.x2) > 0)
                    // Flow right-to-left
                    result[i].X = -result[i].X;

                if ((c.Anchor & Anchor.y1) > 0)
                    // Begin flow at centre of total height
                    result[i].Y -= height / 2;
                else if ((c.Anchor & Anchor.y2) > 0)
                    // Flow bottom-to-top
                    result[i].Y = -result[i].Y;
            }

            return result;
        }

        /// <summary>
        /// Returns true if the given child should be placed on a new row, false otherwise. This will be called automatically for each child in this FillFlowContainers FlowingChildren-List.
        /// </summary>
        /// <param name="child">The child to check.</param>
        /// <returns>True if the given child should be placed on a new row, false otherwise.</returns>
        protected virtual bool ForceNewRow(Drawable child) => false;
    }

    /// <summary>
    /// Represents the horizontal direction of a fill flow.
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
    }
}
