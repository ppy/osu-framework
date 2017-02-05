// Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Diagnostics;
using osu.Framework.Caching;
using OpenTK;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Transformations;

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
                if (!StaticCached.AlwaysStale && !isComputingAutosize && (AutoSizeAxes & Axes.X) > 0)
                    updateAutoSize();
                return base.Width;
            }

            set
            {
                Debug.Assert((AutoSizeAxes & Axes.X) == 0, @"The width of an AutoSizeContainer should only be manually set if it is relative to its parent.");
                base.Width = value;
            }
        }

        public override float Height
        {
            get
            {
                if (!StaticCached.AlwaysStale && !isComputingAutosize && (AutoSizeAxes & Axes.Y) > 0)
                    updateAutoSize();
                return base.Height;
            }

            set
            {
                Debug.Assert((AutoSizeAxes & Axes.Y) == 0, @"The height of an AutoSizeContainer should only be manually set if it is relative to its parent.");
                base.Height = value;
            }
        }

        private bool isComputingAutosize = false;
        public override Vector2 Size
        {
            get
            {
                if (!StaticCached.AlwaysStale && !isComputingAutosize && AutoSizeAxes != Axes.None)
                    updateAutoSize();
                return base.Size;
            }

            set
            {
                //transform check here is to allow AutoSizeDuration to work below.
                Debug.Assert((AutoSizeAxes & Axes.Both) == 0 || Transforms.Find(t => t is TransformSize) != null, @"The Size of an AutoSizeContainer should only be manually set if it is relative to its parent.");
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
                                (RelativeSizeAxes & Axes.X) == 0 ? b.X : base.Width,
                                (RelativeSizeAxes & Axes.Y) == 0 ? b.Y : base.Height
                            ), AutoSizeDuration, AutoSizeEasing);
                    }
                    else
                    {
                        if ((RelativeSizeAxes & Axes.X) == 0) base.Width = b.X;
                        if ((RelativeSizeAxes & Axes.Y) == 0) base.Height = b.Y;
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

                    Vector2 cBound = c.BoundingSize;

                    if ((c.RelativeSizeAxes & Axes.X) == 0 && (c.RelativePositionAxes & Axes.X) == 0)
                        maxBoundSize.X = Math.Max(maxBoundSize.X, cBound.X);

                    if ((c.RelativeSizeAxes & Axes.Y) == 0 && (c.RelativePositionAxes & Axes.Y) == 0)
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

                Debug.Assert((RelativeSizeAxes & value) == 0, "No axis can be relatively sized and automatically sized at the same time.");

                autoSizeAxes = value;

                if (AutoSizeAxes != Axes.None)
                    autoSize.Invalidate();
            }
        }
    }
}
