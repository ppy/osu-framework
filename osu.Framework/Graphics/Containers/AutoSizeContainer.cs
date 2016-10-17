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
            get
            {
                return base.Size;
            }

            set
            {
                Debug.Assert((RelativeSizeAxes & Axes.X) > 0 || value.X == -1, @"The Size of an AutoSizeContainer should never be manually set.");
                Debug.Assert((RelativeSizeAxes & Axes.Y) > 0 || value.Y == -1, @"The Size of an AutoSizeContainer should never be manually set.");

                base.Size = value;
            }
        }

        protected override RectangleF DrawRectangleForBounds
        {
            get
            {
                if (RelativeSizeAxes == Axes.Both) return base.DrawRectangleForBounds;

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
                    maxBoundSize.X = Size.X;
                if ((RelativeSizeAxes & Axes.Y) > 0)
                    maxBoundSize.Y = Size.Y;

                return new RectangleF(0, 0, maxBoundSize.X + Padding.TotalHorizontal, maxBoundSize.Y + Padding.TotalVertical);
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
                    Vector2 b = DrawRectangleForBounds.BottomRight;
                    base.Size = new Vector2((RelativeSizeAxes & Axes.X) > 0 ? InternalSize.X : b.X, (RelativeSizeAxes & Axes.Y) > 0 ? InternalSize.Y : b.Y);

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
            Invalidate(Invalidation.Geometry);
        }

        public override bool Remove(Drawable p, bool dispose = true)
        {
            bool result = base.Remove(p, dispose);
            if (result)
                Invalidate(Invalidation.Geometry);

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
