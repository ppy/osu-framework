// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Collections.Generic;
using OpenTK;
using System.Linq;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Transforms;
using osu.Framework.MathUtils;

namespace osu.Framework.Graphics.Containers
{
    using static FillDirection;

    public class FillFlowContainer : FillFlowContainer<Drawable>
    { }

    public class FillFlowContainer<T> : FlowContainer<T> where T : Drawable
    {
        private FillDirection direction = RightDown;

        /// <summary>
        /// The direction of the fill. Default is <see cref="RightDown"/>.
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

        private bool flowsRightToLeft => direction == LeftDown || direction == LeftUp || direction == Left;
        private bool flowsBottomToTop => direction == RightUp || direction == LeftUp || direction == Up;
        private bool flowsVertical => direction != Right && direction != Left;
        private bool flowsHorizontal => direction != Up && direction != Down;

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
            UpdateTransformsOfType(typeof(TransformSpacing));
            TransformVectorTo(Spacing, newSpacing, duration, easing, new TransformSpacing());
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
            var elementSizes = FlowingChildren.Select(d => d.BoundingBox.Size).ToArray();

            var current = Vector2.Zero;

            var max = MaximumSize;
            if (max == Vector2.Zero)
            {
                var s = ChildSize;

                // If we are autosize and haven't specified a maximum size, we should allow infinite expansion.
                // If we are inheriting then we need to use the parent size (our ActualSize).
                max.X = (AutoSizeAxes & Axes.X) > 0 ? float.MaxValue : s.X;
                max.Y = (AutoSizeAxes & Axes.Y) > 0 ? float.MaxValue : s.Y;
            }
            float rowMaxHeight = 0;
            KeyValuePair<Vector2, Vector2>[] result = new KeyValuePair<Vector2, Vector2>[elementSizes.Length];

            var i = -1;
            foreach (var size in elementSizes)
            {
                ++i;
                
                Vector2 spacing = size;
                if (spacing.X > 0)
                    spacing.X = Math.Max(0, spacing.X + Spacing.X);
                if (spacing.Y > 0)
                    spacing.Y = Math.Max(0, spacing.Y + Spacing.Y);

                //We've exceeded our allowed width, move to a new row
                if (flowsVertical && (Precision.DefinitelyBigger(current.X + size.X, max.X) || !flowsHorizontal))
                {
                    current.X = 0;
                    current.Y += rowMaxHeight;

                    result[i] = new KeyValuePair<Vector2, Vector2>(size, current);

                    rowMaxHeight = 0;
                }
                else
                    result[i] = new KeyValuePair<Vector2, Vector2>(size, current);

                if (spacing.Y > rowMaxHeight)
                    rowMaxHeight = spacing.Y;
                current.X += spacing.X;
            }

            IEnumerable<KeyValuePair<Vector2, Vector2>> resultEnum = result;
            if (flowsRightToLeft)
            {
                var maxX = (AutoSizeAxes & Axes.X) > 0 ? result.Max(kvp => kvp.Value.X) : max.X;
                resultEnum = resultEnum.Select(kvp => new KeyValuePair<Vector2, Vector2>(kvp.Key, new Vector2(maxX - kvp.Value.X - kvp.Key.X, kvp.Value.Y)));
            }
            if (flowsBottomToTop)
            {
                var maxY = (AutoSizeAxes & Axes.Y) > 0 ? result.Max(kvp => kvp.Value.Y) : max.Y;
                resultEnum = resultEnum.Select(kvp => new KeyValuePair<Vector2, Vector2>(kvp.Key, new Vector2(kvp.Value.X, maxY - kvp.Value.Y - kvp.Key.Y)));
            }

            return resultEnum.Select(kvp => kvp.Value);
        }
    }

    /// <summary>
    /// Represents the horizontal direction of a fill flow.
    /// </summary>
    public enum FillDirection
    {
        /// <summary>
        /// Flow left to right, then top to bottom.
        /// </summary>
        RightDown,
        /// <summary>
        /// Flow left to right, then bottom to top.
        /// </summary>
        RightUp,
        /// <summary>
        /// Flow left to right.
        /// </summary>
        Right,
        /// <summary>
        /// Flow right to left, then top to bottom.
        /// </summary>
        LeftDown,
        /// <summary>
        /// Flow right to left, then bottom to top.
        /// </summary>
        LeftUp,
        /// <summary>
        /// Flow right to left.
        /// </summary>
        Left,
        /// <summary>
        /// Flow top to bottom.
        /// </summary>
        Down,
        /// <summary>
        /// Flow bottom to top.
        /// </summary>
        Up,
    }
}