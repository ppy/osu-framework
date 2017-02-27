﻿// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Framework.Caching;
using osu.Framework.Graphics.Transforms;
using OpenTK;

namespace osu.Framework.Graphics.Containers
{
    public class FlowContainer : FlowContainer<Drawable>
    { }

    public class FlowContainer<T> : Container<T>
        where T : Drawable
    {
        internal event Action OnLayout;

        public EasingTypes LayoutEasing
        {
            get
            {
                return AutoSizeEasing;
            }
            set
            {
                AutoSizeEasing = value;
            }
        }

        public float LayoutDuration
        {
            get
            {
                return AutoSizeDuration * 2;
            }
            set
            {
                //coupling with autosizeduration allows us to smoothly transition our size
                //when no children are left to dictate autosize.
                AutoSizeDuration = value / 2;
            }
        }

        private Cached layout = new Cached();

        private FlowDirections direction = FlowDirections.Both;

        public FlowDirections Direction
        {
            get { return direction; }
            set
            {
                if (value == direction) return;
                direction = value;

                layout.Invalidate();
            }
        }

        Vector2 maximumSize;

        /// <summary>
        /// Optional maximum dimensions for this container.
        /// </summary>
        public Vector2 MaximumSize
        {
            get { return maximumSize; }
            set
            {
                if (maximumSize == value) return;

                maximumSize = value;
                Invalidate(Invalidation.Geometry);
            }
        }

        /// <summary>
        /// Pixel spacing added between our Children
        /// </summary>
        Vector2 spacing;
        public Vector2 Spacing
        {
            get { return spacing; }
            set
            {
                if (spacing == value) return;

                spacing = value;
                Invalidate(Invalidation.Geometry);
            }
        }

        public void TransformSpacingTo(Vector2 newSpacing, double duration = 0, EasingTypes easing = EasingTypes.None)
        {
            UpdateTransformsOfType(typeof(TransformSpacing));
            TransformVectorTo(spacing, newSpacing, duration, easing, new TransformSpacing());
        }

        public class TransformSpacing : TransformVector
        {
            public override void Apply(Drawable d)
            {
                base.Apply(d);
                FlowContainer<T> flowContainer = (FlowContainer<T>)d;
                flowContainer.Spacing = CurrentValue;
            }
        }

        protected override bool RequiresChildrenUpdate => base.RequiresChildrenUpdate || !layout.IsValid;

        public override bool Invalidate(Invalidation invalidation = Invalidation.All, Drawable source = null, bool shallPropagate = true)
        {
            if ((invalidation & Invalidation.SizeInParentSpace) > 0)
                layout.Invalidate();

            return base.Invalidate(invalidation, source, shallPropagate);
        }

        protected override bool UpdateChildrenLife()
        {
            bool changed = base.UpdateChildrenLife();

            if (changed)
                layout.Invalidate();

            return changed;
        }

        public override void InvalidateFromChild(Invalidation invalidation, IDrawable source)
        {
            if ((invalidation & Invalidation.SizeInParentSpace) > 0)
                layout.Invalidate();

            base.InvalidateFromChild(invalidation, source);
        }

        protected virtual IEnumerable<T> SortedChildren => AliveInternalChildren;

        protected override void UpdateAfterChildren()
        {
            base.UpdateAfterChildren();

            if (!layout.EnsureValid())
            {
                layout.Refresh(delegate
                {
                    OnLayout?.Invoke();

                    if (Children.FirstOrDefault() == null) return;

                    Vector2 current = Vector2.Zero;

                    Vector2 max = maximumSize;
                    if (direction == FlowDirections.Both && maximumSize == Vector2.Zero)
                    {
                        var s = ChildSize;

                        //If we are autosize and haven't specified a maximum size, we should allow infinite expansion.
                        //If we are inheriting then we need to use the parent size (our ActualSize).
                        max.X = (AutoSizeAxes & Axes.X) > 0 ? float.MaxValue : s.X;
                        max.Y = (AutoSizeAxes & Axes.Y) > 0 ? float.MaxValue : s.Y;
                    }

                    float rowMaxHeight = 0;
                    foreach (T d in SortedChildren)
                    {
                        Vector2 size = Vector2.Zero;

                        if (d.IsPresent)
                        {
                            size = d.LayoutSize * d.Scale;

                            //We've exceeded our allowed width, move to a new row
                            if (Direction != FlowDirections.Horizontal && current.X + size.X > max.X)
                            {
                                current.X = 0;
                                current.Y += rowMaxHeight;

                                rowMaxHeight = 0;
                            }

                            //todo: check this is correct
                            if (size.X > 0) size.X = Math.Max(0, size.X + Spacing.X);
                            if (size.Y > 0) size.Y = Math.Max(0, size.Y + Spacing.Y);

                            if (size.Y > rowMaxHeight) rowMaxHeight = size.Y;
                        }

                        if (current != d.DrawPosition)
                            d.MoveTo(current, LayoutDuration, LayoutEasing);

                        current.X += size.X;
                    }
                });
            }
        }
    }

    [Flags]
    public enum FlowDirections
    {
        Horizontal = 1 << 0,
        Vertical = 1 << 1,

        Both = Horizontal | Vertical,
    }
}
