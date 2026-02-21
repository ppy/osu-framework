// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Layout;
using osu.Framework.Utils;

namespace osu.Framework.Graphics.Containers
{
    /// <summary>
    /// A container that can place a child on each on its borders and will size its children to achieve the following layout:
    /// +-------------------------+
    /// |          Top            |
    /// +------+----------+-------+
    /// |      |          |       |
    /// | Left |  Center  | Right |
    /// |      |          |       |
    /// +------+----------+-------+
    /// |         Bottom          |
    /// +-------------------------+
    /// </summary>
    public partial class BorderLayoutContainer : CompositeDrawable
    {
        /// <summary>
        /// <see cref="Drawable"/> to place at the top edge of this <see cref="BorderLayoutContainer"/>
        /// </summary>
        public Drawable Top
        {
            set => top.Child = value;
        }

        /// <summary>
        /// <see cref="Drawable"/> to place at the bottom edge of this <see cref="BorderLayoutContainer"/>
        /// </summary>
        public Drawable Bottom
        {
            set => bottom.Child = value;
        }

        /// <summary>
        /// <see cref="Drawable"/> to place at the left edge of this <see cref="BorderLayoutContainer"/>
        /// </summary>
        public Drawable Left
        {
            set => left.Child = value;
        }

        /// <summary>
        /// <see cref="Drawable"/> to place at the right edge of this <see cref="BorderLayoutContainer"/>
        /// </summary>
        public Drawable Right
        {
            set => right.Child = value;
        }

        /// <summary>
        /// <see cref="Drawable"/> to place at the center of this <see cref="BorderLayoutContainer"/>
        /// </summary>
        public Drawable Center
        {
            set => center.Child = value;
        }

        private readonly Container top = new Container
        {
            Name = "Top",
            RelativeSizeAxes = Axes.X,
            AutoSizeAxes = Axes.Y,
        };

        private readonly Container bottom = new Container
        {
            Name = "Bottom",
            RelativeSizeAxes = Axes.X,
            AutoSizeAxes = Axes.Y,
            Anchor = Anchor.BottomLeft,
            Origin = Anchor.BottomLeft,
        };

        private readonly Container left = new Container
        {
            Name = "Left",
            RelativeSizeAxes = Axes.Y,
            AutoSizeAxes = Axes.X,
        };

        private readonly Container right = new Container
        {
            Name = "Right",
            RelativeSizeAxes = Axes.Y,
            AutoSizeAxes = Axes.X,
            Anchor = Anchor.TopRight,
            Origin = Anchor.TopRight,
        };

        private readonly Container center = new Container
        {
            Name = "Center",
            RelativeSizeAxes = Axes.Both,
        };

        private Direction layoutDirection = Direction.Vertical;

        /// <summary>
        /// Whether the layout of the children at horizontal or vertical edges get computed first.
        ///
        /// Layout for <see cref="Direction.Vertical"/>:
        /// +-------------------------+
        /// |          Top            |
        /// +------+----------+-------+
        /// |      |          |       |
        /// | Left |  Center  | Right |
        /// |      |          |       |
        /// +------+----------+-------+
        /// |         Bottom          |
        /// +-------------------------+
        ///
        /// Layout for <see cref="Direction.Horizontal"/>:
        /// +-------------------------+
        /// |      |   Top    |       |
        /// |      +----------+       |
        /// |      |          |       |
        /// | Left |  Center  | Right |
        /// |      |          |       |
        /// |      +----------+       |
        /// |      |  Bottom  |       |
        /// +------+----------+-------+
        /// </summary>
        public Direction LayoutDirection
        {
            get => layoutDirection;
            set
            {
                if (layoutDirection == value)
                    return;

                layoutDirection = value;
                layoutBacking.Invalidate();
            }
        }

        private MarginPadding spacing;

        /// <summary>
        /// Spacing to put between the drawable at each edge and its neighbors.
        /// </summary>
        /// <remarks>If an edge has no drawable present the spacing for the given edge is ignored.</remarks>
        public MarginPadding Spacing
        {
            get => spacing;
            set
            {
                if (spacing.Equals(value))
                    return;

                if (!Validation.IsFinite(value)) throw new ArgumentException($@"{nameof(Spacing)} must be finite, but is {value}.");

                spacing = value;
                layoutBacking.Invalidate();
            }
        }

        private readonly LayoutValue layoutBacking = new LayoutValue(Invalidation.DrawSize, InvalidationSource.Self | InvalidationSource.Child);

        public BorderLayoutContainer()
        {
            RelativeSizeAxes = Axes.Both;

            InternalChildren = new[]
            {
                center,
                right,
                left,
                bottom,
                top,
            };

            AddLayout(layoutBacking);
        }

        protected override void UpdateAfterChildren()
        {
            base.UpdateAfterChildren();

            if (!layoutBacking.IsValid)
            {
                updateLayout();
                layoutBacking.Validate();
            }
        }

        private void updateLayout()
        {
            if (layoutDirection == Direction.Vertical)
            {
                top.Padding = default;
                bottom.Padding = default;

                var padding = new MarginPadding
                {
                    Top = getPadding(top, Direction.Vertical, spacing.Top),
                    Bottom = getPadding(bottom, Direction.Vertical, spacing.Bottom),
                };

                right.Padding = padding;
                left.Padding = padding;

                center.Padding = padding with
                {
                    Left = getPadding(left, Direction.Horizontal, spacing.Left),
                    Right = getPadding(right, Direction.Horizontal, spacing.Right),
                };
            }
            else
            {
                left.Padding = default;
                right.Padding = default;

                var padding = new MarginPadding
                {
                    Left = getPadding(left, Direction.Horizontal, spacing.Left),
                    Right = getPadding(right, Direction.Horizontal, spacing.Right),
                };

                top.Padding = padding;
                bottom.Padding = padding;

                center.Padding = padding with
                {
                    Top = getPadding(top, Direction.Vertical, spacing.Top),
                    Bottom = getPadding(bottom, Direction.Vertical, spacing.Bottom),
                };
            }

            static float getPadding(Container container, Direction direction, float spacing)
            {
                if (container.Children.Count == 0 || !container.Children[0].IsPresent)
                    return 0;

                var drawable = container.Children[0];

                switch (direction)
                {
                    case Direction.Horizontal:
                        if ((drawable.RelativeSizeAxes & Axes.X) != 0)
                            throw new InvalidOperationException($"Drawables positioned on the left/right edge of a {nameof(BorderLayoutContainer)} cannot be sized relatively along the X axis.");

                        return drawable.LayoutSize.X + spacing;

                    case Direction.Vertical:
                        if ((drawable.RelativeSizeAxes & Axes.Y) != 0)
                            throw new InvalidOperationException($"Drawables positioned on the top/bottom edge of a {nameof(BorderLayoutContainer)} cannot be sized relatively along the Y axis.");

                        return drawable.LayoutSize.Y + spacing;

                    default:
                        throw new ArgumentOutOfRangeException(nameof(direction), direction, null);
                }
            }
        }
    }
}
