// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Collections.Generic;
using OpenTK;
using System.Linq;
using osu.Framework.Graphics.Transforms;
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
    public class FillFlowContainer<T> : FlowContainer<T> where T : Drawable
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

        public void TransformSpacingTo(Vector2 newSpacing, double duration = 0, EasingTypes easing = EasingTypes.None)
        {
            TransformTo(Spacing, newSpacing, duration, easing, new TransformSpacing());
        }

        public class TransformSpacing : TransformVector
        {
            public override void Apply(Drawable d)
            {
                base.Apply(d);
                ((FillFlowContainer<T>)d).Spacing = CurrentValue;
            }
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
            List<float> rowWidths = new List<float>();

            // Variables keeping track of the current state while iterating over children
            // and computing initial flow positions.
            float rowMaxHeight = 0;
            var current = Vector2.Zero;

            // First pass, computing initial flow positions
            for (int i = 0; i < children.Length; ++i)
            {
                Vector2 size = children[i].BoundingBox.Size;

                Vector2 stride = size;
                if (stride.X > 0)
                    stride.X = Math.Max(0, stride.X + Spacing.X);
                if (stride.Y > 0)
                    stride.Y = Math.Max(0, stride.Y + Spacing.Y);

                //We've exceeded our allowed width, move to a new row
                if (direction != FillDirection.Horizontal && (Precision.DefinitelyBigger(current.X + size.X, max.X) || direction == FillDirection.Vertical))
                {
                    current.X = 0;
                    current.Y += rowMaxHeight;

                    result[i] = current;
                    rowWidths.Add(i == 0 ? 0 : result[i - 1].X);
                    rowMaxHeight = 0;
                }
                else
                    result[i] = current;

                rowIndices[i] = rowWidths.Count;

                if (stride.Y > rowMaxHeight)
                    rowMaxHeight = stride.Y;
                current.X += stride.X;
            }

            rowWidths.Add(result.Last().X);
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
                    result[i].X -= rowWidths[rowIndices[i]] / 2;
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
