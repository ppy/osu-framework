// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using osuTK;
using System.Linq;
using osu.Framework.Extensions.EnumExtensions;
using osu.Framework.Utils;

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

        private CrossAxes spacingFactor(Drawable c)
        {
            Vector2 result = c.RelativeOriginPosition;
            if (c.Anchor.HasFlagFast(Anchor.x2))
                result.X = 1 - result.X;
            if (c.Anchor.HasFlagFast(Anchor.y2))
                result.Y = 1 - result.Y;
            return new CrossAxes { Direction = Direction, Vector = result };
        }

        private CrossAxes calculateMaximumCrossSize()
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

            return new CrossAxes { Direction = Direction, Vector = max };
        }
        // Lists used to reduce allocations when computing layout positions. Take note that List.Clear does not reduce the already allocated capacity.
        private List<Drawable> children = new List<Drawable>();
        private List<Vector2> layoutPositions = new List<Vector2>();
        private List<int> rowIndices = new List<int>();
        private List<float> rowOffsetsToMiddle = new List<float>();
        protected override IEnumerable<Vector2> ComputeLayoutPositions()
        {
            if (!FlowingChildren.Any())
                return Array.Empty<Vector2>();
            children.Clear();
            children.AddRange(FlowingChildren);

            // The positions for each child we will return later on.
            layoutPositions.Clear();

            // We need to keep track of row size such that we can compute correct
            // positions for centre anchor children.
            // We also store for each child to which row it belongs.
            // Take note that the term "row" refers to column in the case of a vertical main axis.
            rowIndices.Clear();
            rowOffsetsToMiddle.Clear();
            rowOffsetsToMiddle.Add(0);

            // Variables keeping track of the current state while iterating over children
            // and computing initial flow positions.
            float rowCross = 0;
            var current = new CrossAxes { Direction = Direction };
            var max = calculateMaximumCrossSize();
            var size = new CrossAxes { Direction = Direction, Vector = children[0].BoundingBox.Size };
            Vector2 ourRelativeAnchor = children[0].RelativeAnchorPosition;
            var rowBeginOffset = spacingFactor(children[0]) * size;

            // First pass, computing initial flow positions
            for (int i = 0; i < children.Count; i++)
            {
                Drawable c = children[i];
                validateChild(c);

                float rowMain = rowBeginOffset.Main + current.Main + (1 - spacingFactor(c).Main) * size.Main;

                //We've exceeded our allowed main size, move to a new row
                if ((Direction.AffectedAxes() == Axes.Both && Precision.DefinitelyBigger(rowMain, max.Main)) || ForceNewRow(c))
                {
                    current.Main = 0;
                    current.Cross += rowCross;

                    layoutPositions.Add(current);

                    rowOffsetsToMiddle.Add(0);
                    rowBeginOffset = spacingFactor(c) * size;

                    rowCross = 0;
                }
                else
                {
                    layoutPositions.Add(current);

                    // Compute offset to the middle of the row, to be applied in case of centre anchor
                    // in a second pass.
                    rowOffsetsToMiddle[^1] = rowBeginOffset.Main - rowMain / 2;
                }

                rowIndices.Add(rowOffsetsToMiddle.Count - 1);

                CrossAxes stride = new CrossAxes { Direction = Direction };

                if (i < children.Count - 1)
                {
                    // Compute stride. Note, that the stride depends on the origins of the drawables
                    // on both sides of the step to be taken.
                    stride.Vector = (Vector2.One - spacingFactor(c)) * size;

                    c = children[i + 1];
                    size.Vector = c.BoundingBox.Size;

                    stride.Vector += spacingFactor(c) * size;
                }

                stride.Vector += Spacing;

                if (stride.Cross > rowCross)
                    rowCross = stride.Cross;
                current.Main += stride.Main;
            }

            float cross = Direction.MainAxis() == Axes.X ? layoutPositions.Last().Y : layoutPositions.Last().X;

            // Second pass, adjusting the positions for anchors of children.
            for (int i = 0; i < children.Count; i++)
            {
                var c = children[i];

                if (c.Anchor.HasFlagFast(Anchor.x2))
                    // Flow right-to-left
                    layoutPositions[i] = new Vector2(-layoutPositions[i].X, layoutPositions[i].Y);
                else if (c.Anchor.HasFlagFast(Anchor.x1))
                {
                    // Begin flow at centre of row
                    if (Direction.MainAxis() == Axes.X)
                        layoutPositions[i] += new Vector2(rowOffsetsToMiddle[rowIndices[i]], 0);
                    else
                        layoutPositions[i] -= new Vector2(cross / 2, 0);
                }

                if (c.Anchor.HasFlagFast(Anchor.y2))
                    // Flow bottom-to-top
                    layoutPositions[i] = new Vector2(layoutPositions[i].X, -layoutPositions[i].Y);
                else if (c.Anchor.HasFlagFast(Anchor.y1))
                {
                    // Begin flow at centre of total height
                    if (Direction.MainAxis() == Axes.Y)
                        layoutPositions[i] += new Vector2(0, rowOffsetsToMiddle[rowIndices[i]]);
                    else
                        layoutPositions[i] -= new Vector2(0, cross / 2);
                }
            }

            void validateChild(Drawable c)
            {
                // In some cases (see the right hand side of the conditional) we want to permit relatively sized children
                // in our fill direction; specifically, when children use FillMode.Fit to preserve the aspect ratio.
                // Consider the following use case: A fill flow container has a fixed width but an automatic height, and fills
                // in the vertical direction. Now, we can add relatively sized children with FillMode.Fit to make sure their
                // aspect ratio is preserved while still allowing them to fill vertically. This special case can not result
                // in an autosize-related feedback loop, and we can thus simply allow it.
                if ((c.RelativeSizeAxes & AutoSizeAxes & Direction.AffectedAxes()) != 0 && (c.FillMode != FillMode.Fit || c.RelativeSizeAxes != Axes.Both || c.Size.X > RelativeChildSize.X || c.Size.Y > RelativeChildSize.Y || AutoSizeAxes == Axes.Both))
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

            return layoutPositions;
        }

        /// <summary>
        /// Returns true if the given child should be placed on a new row, false otherwise. This will be called automatically for each child in this FillFlowContainers FlowingChildren-List.
        /// Take note that if the main axis is vertical this will force a new column instead.
        /// </summary>
        /// <param name="child">The child to check.</param>
        /// <returns>True if the given child should be placed on a new row, false otherwise.</returns>
        protected virtual bool ForceNewRow(Drawable child) => false;
    }

    internal struct CrossAxes
    {
        public FillDirection Direction;
        public float Main;
        public float Cross;
        public float X
        {
            get => Direction.MainAxis() == Axes.X ? Main : Cross;
            set
            {
                if (Direction.MainAxis() == Axes.X)
                    Main = value;
                else
                    Cross = value;
            }
        }
        public float Y
        {
            get => Direction.MainAxis() == Axes.Y ? Main : Cross;
            set
            {
                if (Direction.MainAxis() == Axes.Y)
                    Main = value;
                else
                    Cross = value;
            }
        }
        public Vector2 Vector
        {
            get => new Vector2(X, Y);
            set
            {
                X = value.X;
                Y = value.Y;
            }
        }
        public static implicit operator Vector2(CrossAxes axes)
            => axes.Vector;
        public static CrossAxes operator *(CrossAxes a, CrossAxes scale)
        {
            if (a.Direction != scale.Direction) throw new InvalidOperationException("Cannot multiply cross axes with different orientations");
            return new CrossAxes { Direction = a.Direction, Vector = a.Vector * scale.Vector };
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
        FullVertical
    }

    public static class FillDirectionExtensions
    {
        public static Axes AffectedAxes(this FillDirection direction)
        {
            return direction switch
            {
                FillDirection.Full => Axes.Both,
                FillDirection.FullVertical => Axes.Both,
                FillDirection.Horizontal => Axes.X,
                FillDirection.Vertical => Axes.Y,
                _ => throw new ArgumentException($"{nameof(FillDirection)} {direction} is not defined")
            };
        }

        public static Axes MainAxis(this FillDirection direction)
        {
            return direction switch
            {
                FillDirection.Full => Axes.X,
                FillDirection.Horizontal => Axes.X,
                FillDirection.FullVertical => Axes.Y,
                FillDirection.Vertical => Axes.Y,
                _ => throw new ArgumentException($"{nameof(FillDirection)} {direction} is not defined")
            };
        }

        public static Axes CrossAxis(this FillDirection direction)
            => direction.MainAxis() == Axes.X ? Axes.Y : Axes.X;
    }
}
