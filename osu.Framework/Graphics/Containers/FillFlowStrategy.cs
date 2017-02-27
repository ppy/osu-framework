using System;
using System.Collections.Generic;
using OpenTK;
using System.Linq;
using osu.Framework.MathUtils;

namespace osu.Framework.Graphics.Containers
{
    public class FillFlowStrategy : IFlowStrategy
    {
        private HorizontalDirection horizontalFlow;
        private VerticalDirection verticalFlow;

        public event Action OnInvalidateLayout;

        /// <summary>
        /// The horizontal direction of the fill. Default is <see cref="HorizontalDirection.LeftToRight"/>.
        /// </summary>
        public HorizontalDirection HorizontalFlow
        {
            get { return horizontalFlow; }
            set
            {
                if (value == HorizontalDirection.None && VerticalFlow == VerticalDirection.None)
                    throw new InvalidOperationException($"The horizontal and vertical flow direction of the {nameof(FillFlowStrategy)} cannot both be set to none.");

                if (horizontalFlow == value)
                    return;

                horizontalFlow = value;
                OnInvalidateLayout?.Invoke();
            }
        }
        /// <summary>
        /// The horizontal direction of the fill. Default is <see cref="VerticalDirection.TopToBottom"/>.
        /// </summary>
        public VerticalDirection VerticalFlow
        {
            get { return verticalFlow; }
            set
            {
                if (value == VerticalDirection.None && HorizontalFlow == HorizontalDirection.None)
                    throw new InvalidOperationException($"The horizontal and vertical flow direction of the {nameof(FillFlowStrategy)} cannot both be set to none.");

                if (verticalFlow == value)
                    return;

                verticalFlow = value;
                OnInvalidateLayout?.Invoke();
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
                OnInvalidateLayout?.Invoke();
            }
        }

        /// <summary>
        /// Constructs a new left-to-right top-to-bottom fill flow strategy with no spacing.
        /// </summary>
        public FillFlowStrategy()
        {
            HorizontalFlow = HorizontalDirection.LeftToRight;
            VerticalFlow = VerticalDirection.TopToBottom;
        }

        public IEnumerable<Vector2> UpdateLayout<T>(FlowContainer<T> container, IReadOnlyCollection<Vector2> elementSizes) where T : Drawable
        {
            var current = Vector2.Zero;

            var max = container.MaximumSize;
            if ((VerticalFlow != VerticalDirection.None || HorizontalFlow != HorizontalDirection.None) && max == Vector2.Zero)
            {
                var s = container.ChildSize;

                // If we are autosize and haven't specified a maximum size, we should allow infinite expansion.
                // If we are inheriting then we need to use the parent size (our ActualSize).
                max.X = (container.AutoSizeAxes & Axes.X) > 0 ? float.MaxValue : s.X;
                max.Y = (container.AutoSizeAxes & Axes.Y) > 0 ? float.MaxValue : s.Y;
            }
            float rowMaxHeight = 0;
            KeyValuePair<Vector2, Vector2>[] result = new KeyValuePair<Vector2, Vector2>[elementSizes.Count];

            var i = -1;
            foreach (var size in elementSizes)
            {
                ++i;

                //todo: check this is correct
                Vector2 spacing = size;
                if (spacing.X > 0)
                    spacing.X = Math.Max(0, spacing.X + Spacing.X);
                if (spacing.Y > 0)
                    spacing.Y = Math.Max(0, spacing.Y + Spacing.Y);

                //We've exceeded our allowed width, move to a new row
                if (VerticalFlow != VerticalDirection.None && (Precision.DefinitelyBigger(current.X + size.X, max.X) || HorizontalFlow == HorizontalDirection.None))
                {
                    current.X = 0;
                    current.Y += rowMaxHeight;

                    result[i] = new KeyValuePair<Vector2, Vector2>(size, current);

                    rowMaxHeight = 0;
                }
                else
                {
                    result[i] = new KeyValuePair<Vector2, Vector2>(size, current);
                }

                if (spacing.Y > rowMaxHeight)
                    rowMaxHeight = spacing.Y;
                current.X += spacing.X;
            }

            IEnumerable<KeyValuePair<Vector2, Vector2>> resultEnum = result;
            if (HorizontalFlow == HorizontalDirection.RightToLeft)
            {
                var maxX = (container.AutoSizeAxes & Axes.X) > 0 ? result.Max(kvp => kvp.Value.X) : max.X;
                resultEnum = resultEnum.Select(kvp => new KeyValuePair<Vector2, Vector2>(kvp.Key, new Vector2(maxX - kvp.Value.X - kvp.Key.X, kvp.Value.Y)));
            }
            if (VerticalFlow == VerticalDirection.BottomToTop)
            {
                var maxY = (container.AutoSizeAxes & Axes.Y) > 0 ? result.Max(kvp => kvp.Value.Y) : max.Y;
                resultEnum = resultEnum.Select(kvp => new KeyValuePair<Vector2, Vector2>(kvp.Key, new Vector2(kvp.Value.X, maxY - kvp.Value.Y - kvp.Key.Y)));
            }

            return resultEnum.Select(kvp => kvp.Value);
        }
    }

    /// <summary>
    /// Represents the horizontal direction of a fill flow.
    /// </summary>
    public enum HorizontalDirection
    {
        /// <summary>
        /// No horizontal flow occurs, one element per row.
        /// </summary>
        None = 0,

        /// <summary>
        /// Elements get arranged from left to right according to their sort order.
        /// </summary>
        LeftToRight,
        /// <summary>
        /// Elements get arranged from right to left according to their sort order.
        /// </summary>
        RightToLeft,
    }

    /// <summary>
    /// Represents the vertical direction of a fill flow.
    /// </summary>
    public enum VerticalDirection
    {
        /// <summary>
        /// No vertical flow occurs, only one row in the container.
        /// </summary>
        None = 0,

        /// <summary>
        /// Rows are arranged vertically from top to bottom.
        /// </summary>
        TopToBottom,
        /// <summary>
        /// Rows are arranged vertically from bottom to top.
        /// </summary>
        BottomToTop,
    }
}