//Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
//Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using OpenTK;
using osu.Framework.Graphics.Transformations;
using System.Linq;
using osu.Framework.Cached;

namespace osu.Framework.Graphics.Containers
{
    public class FlowContainer : AutoSizeContainer
    {
        internal event Action OnLayout;

        public EasingTypes LayoutEasing;
        public int LayoutDuration
        {
            get { return layout.RefreshInterval; }
            set { layout.RefreshInterval = value; }
        }

        private FlowDirection direction = FlowDirection.Full;
        public FlowDirection Direction
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
                Invalidate();
            }
        }

        Vector2 padding;
        public Vector2 Padding
        {
            get { return padding; }
            set
            {
                if (padding == value) return;

                padding = value;
                Invalidate();
            }
        }

        public override bool Invalidate(Invalidation invalidation = Invalidation.All, Drawable source = null, bool shallPropagate = true)
        {
            if ((invalidation & (Invalidation.ScreenSpaceQuad | Invalidation.Visibility)) > 0)
                layout.Invalidate();

            return base.Invalidate(invalidation, source, shallPropagate);
        }

        public override Drawable Add(Drawable drawable)
        {
            //let's force an instant re-flow on adding a new drawable for now.
            layout.Invalidate();

            return base.Add(drawable);
        }

        private Cached<Vector2> layout = new Cached<Vector2>();

        protected override void UpdateLayout()
        {
            base.UpdateLayout();

            if (layout.IsValid) return;

            layout.Refresh(delegate
            {
                OnLayout?.Invoke();

                if (Children.FirstOrDefault() == null) return Vector2.Zero;

                Vector2 current = new Vector2(Math.Max(0, Padding.X), Math.Max(0, Padding.Y));

                Vector2 max = maximumSize;
                if (direction == FlowDirection.Full && maximumSize == Vector2.Zero)
                {
                    var actual = ActualSize;

                    //If we are autosize and haven't specified a maximum size, we should allow infinite expansion.
                    //If we are inheriting then we need to use the parent size (our ActualSize).
                    max.X = (SizeMode & InheritMode.X) == 0 ? float.MaxValue : actual.X;
                    max.Y = (SizeMode & InheritMode.Y) == 0 ? float.MaxValue : actual.Y;
                }

                float rowMaxHeight = 0;
                foreach (Drawable d in Children)
                {
                    if (d.SizeMode != InheritMode.None) continue;

                    Vector2 size = Vector2.Zero;

                    if (d.IsVisible)
                    {
                        size = d.ActualSize * d.Scale * ContentScale;

                        if (Direction != FlowDirection.HorizontalOnly && current.X + size.X > max.X)
                        {
                            current.X = Math.Max(0, Padding.X);
                            current.Y += rowMaxHeight;

                            rowMaxHeight = 0;
                        }

                        //todo: check this is correct
                        if (size.X > 0) size.X = Math.Max(0, size.X + Padding.X);
                        if (size.Y > 0) size.Y = Math.Max(0, size.Y + Padding.Y);

                        if (size.Y > rowMaxHeight) rowMaxHeight = size.Y;
                    }

                    if (current != d.Position)
                    {
                        d.MoveTo(current, LayoutDuration, LayoutEasing);
                        d.UpdateSubTree();
                    }

                    current.X += size.X;
                }

                return current;
            });
        }
    }

    public enum FlowDirection
    {
        VerticalOnly,
        HorizontalOnly,
        Full
    }
}
