// Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Diagnostics;
using osu.Framework.Cached;
using osu.Framework.Graphics.Primitives;
using OpenTK;

namespace osu.Framework.Graphics.Containers
{
    public class AutoSizeContainer : Container
    {
        internal event Action OnAutoSize;

        private Cached<Vector2> autoSize = new Cached<Vector2>();

        public override Vector2 Size
        {
            set
            {
                Debug.Assert((RelativeSizeAxes & Axes.X) > 0 || value.X == -1, @"The Size of an AutoSizeContainer should never be manually set.");
                Debug.Assert((RelativeSizeAxes & Axes.Y) > 0 || value.Y == -1, @"The Size of an AutoSizeContainer should never be manually set.");

                if (value != base.Size)
                {
                    base.Size = value;
                    autoSize.Invalidate();
                }
            }
        }

        private Vector2 computeAutoSize()
        {
            MarginPadding padding = Padding;
            MarginPadding margin = Margin;

            try
            {
                Padding = new MarginPadding();
                Margin = new MarginPadding();

                if (RelativeSizeAxes == Axes.Both) return DrawSize;

                Vector2 maxBoundSize = Vector2.Zero;

                // Find the maximum width/height of children
                foreach (Drawable c in AliveChildren)
                {
                    if (!c.IsVisible)
                        continue;

                    Vector2 cBound = c.BoundingSize;

                    if ((c.RelativeSizeAxes & Axes.X) == 0 && (c.RelativePositionAxes & Axes.X) == 0)
                        maxBoundSize.X = Math.Max(maxBoundSize.X, cBound.X);

                    if ((c.RelativeSizeAxes & Axes.Y) == 0 && (c.RelativePositionAxes & Axes.Y) == 0)
                        maxBoundSize.Y = Math.Max(maxBoundSize.Y, cBound.Y);
                }

                if ((RelativeSizeAxes & Axes.X) > 0)
                    maxBoundSize.X = DrawSize.X;
                if ((RelativeSizeAxes & Axes.Y) > 0)
                    maxBoundSize.Y = DrawSize.Y;

                return new Vector2(maxBoundSize.X, maxBoundSize.Y);
            }
            finally
            {
                Padding = padding;
                Margin = margin;
            }
        }

        protected override bool UpdateChildrenLife()
        {
            bool childChangedStatus = base.UpdateChildrenLife();
            if (childChangedStatus)
                Invalidate(Invalidation.Geometry);

            return childChangedStatus;
        }

        protected internal override bool UpdateSubTree()
        {
            if (!base.UpdateSubTree()) return false;

            if (!autoSize.EnsureValid())
            {
                autoSize.Refresh(delegate
                {
                    Vector2 b = computeAutoSize() + Padding.Total;
                    base.Size = new Vector2(
                        (RelativeSizeAxes & Axes.X) > 0 ? Size.X : b.X,
                        (RelativeSizeAxes & Axes.Y) > 0 ? Size.Y : b.Y
                    );

                    //note that this is called before autoSize becomes valid. may be something to consider down the line.
                    //might work better to add an OnRefresh event in Cached<> and invoke there.
                    OnAutoSize?.Invoke();

                    return b;
                });
            }

            return true;
        }

        public override void Add(Drawable drawable)
        {
            base.Add(drawable);
            InvalidateFromChild(Invalidation.Geometry, drawable);
        }

        public override bool Remove(Drawable p, bool dispose = false)
        {
            bool result = base.Remove(p, dispose);
            if (result)
                InvalidateFromChild(Invalidation.Geometry, p);

            return result;
        }

        internal override void InvalidateFromChild(Invalidation invalidation, Drawable source)
        {
            if ((invalidation & (Invalidation.Visibility | Invalidation.Geometry)) > 0)
                autoSize.Invalidate();

            base.InvalidateFromChild(invalidation, source);
        }

        public override Axes RelativeSizeAxes
        {
            get { return base.RelativeSizeAxes; }
            set
            {
                Debug.Assert(RelativeSizeAxes != Axes.Both);
                base.RelativeSizeAxes = value;
            }
        }
    }
}
