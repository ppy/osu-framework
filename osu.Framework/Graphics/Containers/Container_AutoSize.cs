// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using osu.Framework.Caching;
using OpenTK;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Transformations;
using System.Linq;

namespace osu.Framework.Graphics.Containers
{
    public partial class Container<T>
    {
        internal event Action OnAutoSize;

        public EasingTypes AutoSizeEasing;
        public float AutoSizeDuration { get; set; }

        private Cached autoSize = new Cached();

        public override float Width
        {
            get
            {
                if (!StaticCached.ALWAYS_STALE && !isComputingAutosize && (AutoSizeAxes & Axes.X) > 0)
                    updateAutoSize();
                return base.Width;
            }

            set
            {
                if ((AutoSizeAxes & Axes.X) != 0)
                    throw new InvalidOperationException("The width of an AutoSizeContainer should only be manually set if it is relative to its parent.");
                base.Width = value;
            }
        }

        public override float Height
        {
            get
            {
                if (!StaticCached.ALWAYS_STALE && !isComputingAutosize && (AutoSizeAxes & Axes.Y) > 0)
                    updateAutoSize();
                return base.Height;
            }

            set
            {
                if ((AutoSizeAxes & Axes.Y) != 0)
                    throw new InvalidOperationException("The height of an AutoSizeContainer should only be manually set if it is relative to its parent.");
                base.Height = value;
            }
        }

        private bool isComputingAutosize;
        public override Vector2 Size
        {
            get
            {
                if (!StaticCached.ALWAYS_STALE && !isComputingAutosize && AutoSizeAxes != Axes.None)
                    updateAutoSize();
                return base.Size;
            }

            set
            {
                //transform check here is to allow AutoSizeDuration to work below.
                if ((AutoSizeAxes & Axes.Both) != 0 && !Transforms.Any(t => t is TransformSize))
                    throw new InvalidOperationException("The Size of an AutoSizeContainer should only be manually set if it is relative to its parent.");
                base.Size = value;
            }
        }

        private void updateAutoSize()
        {
            isComputingAutosize = true;
            try
            {
                if (autoSize.EnsureValid()) return;

                autoSize.Refresh(delegate
                {
                    Vector2 b = computeAutoSize() + Padding.Total;

                    if (AutoSizeDuration > 0)
                    {
                        ResizeTo(new Vector2(
                                (AutoSizeAxes & Axes.X) > 0 ? b.X : base.Width,
                                (AutoSizeAxes & Axes.Y) > 0 ? b.Y : base.Height
                            ), AutoSizeDuration, AutoSizeEasing);
                    }
                    else
                    {
                        if ((AutoSizeAxes & Axes.X) > 0) base.Width = b.X;
                        if ((AutoSizeAxes & Axes.Y) > 0) base.Height = b.Y;
                    }

                    //note that this is called before autoSize becomes valid. may be something to consider down the line.
                    //might work better to add an OnRefresh event in Cached<> and invoke there.
                    OnAutoSize?.Invoke();
                });
            }
            finally
            {
                isComputingAutosize = false;
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

                if (AutoSizeAxes == Axes.None) return DrawSize;

                Vector2 maxBoundSize = Vector2.Zero;

                // Find the maximum width/height of children
                foreach (T c in AliveChildren)
                {
                    if (!c.IsPresent)
                        continue;

                    Vector2 cBound = c.BoundingSizeWithOrigin;

                    if ((c.BypassAutoSizeAxes & Axes.X) == 0)
                        maxBoundSize.X = Math.Max(maxBoundSize.X, cBound.X);

                    if ((c.BypassAutoSizeAxes & Axes.Y) == 0)
                        maxBoundSize.Y = Math.Max(maxBoundSize.Y, cBound.Y);
                }

                if ((AutoSizeAxes & Axes.X) == 0)
                    maxBoundSize.X = DrawSize.X;
                if ((AutoSizeAxes & Axes.Y) == 0)
                    maxBoundSize.Y = DrawSize.Y;

                return new Vector2(maxBoundSize.X, maxBoundSize.Y);
            }
            finally
            {
                Padding = padding;
                Margin = margin;
            }
        }

        private Axes autoSizeAxes;

        public Axes AutoSizeAxes
        {
            get { return autoSizeAxes; }
            set
            {
                if (value == autoSizeAxes)
                    return;

                if ((RelativeSizeAxes & value) != 0)
                    throw new InvalidOperationException("No axis can be relatively sized and automatically sized at the same time.");

                autoSizeAxes = value;

                if (AutoSizeAxes != Axes.None)
                    autoSize.Invalidate();
            }
        }
    }
}
