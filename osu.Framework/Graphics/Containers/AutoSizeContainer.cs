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
        protected bool RequireAutoSize => RelativeSizeAxes != Axes.Both && !autoSize.IsValid;

        internal event Action OnAutoSize;

        private Cached<Vector2> autoSize = new Cached<Vector2>();

        public override bool Invalidate(Invalidation invalidation = Invalidation.All, Drawable source = null, bool shallPropagate = true)
        {
            if ((invalidation & Invalidation.SizeInParentSpace) > 0)
                autoSize.Invalidate();

            return base.Invalidate(invalidation, source, shallPropagate);
        }

        protected override Quad DrawQuadForBounds
        {
            get
            {
                if (RelativeSizeAxes == Axes.Both) return base.DrawQuadForBounds;

                Vector2 maxBoundSize = Vector2.Zero;

                // Find the maximum width/height of children
                foreach (Drawable c in AliveChildren)
                {
                    if (!c.IsVisible)
                        continue;

                    Vector2 cBound = c.BoundingSize;

                    if ((c.RelativeSizeAxes & Axes.X) == 0)
                        maxBoundSize.X = Math.Max(maxBoundSize.X, cBound.X);
                    else
                        Debug.Assert(c.InternalSize.X <= 1, @"Can't extend AutoSize container by setting inheriting dimension over one");

                    if ((c.RelativeSizeAxes & Axes.Y) == 0)
                        maxBoundSize.Y = Math.Max(maxBoundSize.Y, cBound.Y);
                    else
                        Debug.Assert(c.InternalSize.Y <= 1, @"Can't extend AutoSize container by setting inheriting dimension over one");
                }

                if ((RelativeSizeAxes & Axes.X) > 0)
                    maxBoundSize.X = Size.X;
                if ((RelativeSizeAxes & Axes.Y) > 0)
                    maxBoundSize.Y = Size.Y;

                return new Quad(0, 0, maxBoundSize.X, maxBoundSize.Y);
            }
        }

        protected override bool UpdateChildrenLife()
        {
            bool childChangedStatus = base.UpdateChildrenLife();
            if (childChangedStatus)
                Invalidate(Invalidation.Position | Invalidation.SizeInParentSpace);

            return childChangedStatus;
        }

        internal override bool UpdateSubTree()
        {
            if (!base.UpdateSubTree()) return false;

            if (!autoSize.EnsureValid())
            {
                autoSize.Refresh(delegate
                {
                    Vector2 b = DrawQuadForBounds.BottomRight;

                    InternalSize = new Vector2((RelativeSizeAxes & Axes.X) > 0 ? InternalSize.X : b.X, (RelativeSizeAxes & Axes.Y) > 0 ? InternalSize.Y : b.Y);
                    Invalidate(Invalidation.Position);

                    //note that this is called before autoSize becomes valid. may be something to consider down the line.
                    //might work better to add an OnRefresh event in Cached<> and invoke there.
                    OnAutoSize?.Invoke();

                    return b;
                });
            }

            return true;
        }

        public override Drawable Add(Drawable drawable)
        {
            Drawable result = base.Add(drawable);
            if (result != null)
                Invalidate(Invalidation.Position | Invalidation.SizeInParentSpace);

            return result;
        }

        public override bool Remove(Drawable p, bool dispose = true)
        {
            bool result = base.Remove(p, dispose);
            if (result)
                Invalidate(Invalidation.Position | Invalidation.SizeInParentSpace);

            return result;
        }

        //public override Vector2 ActualSize
        //{
        //    get
        //    {
        //        if (HasDefinedSize)
        //            return base.ActualSize;

        //        if (SizeMode == InheritMode.None)
        //            return new Vector2(0);

        //        var actual = base.ActualSize;

        //        return new Vector2((SizeMode & InheritMode.X) > 0 ? actual.X : 0, (SizeMode & InheritMode.Y) > 0 ? actual.Y : 0);
        //    }
        //}

        protected override bool HasDefinedSize => !RequireAutoSize;

        protected override Invalidation InvalidationEffectByChildren(Invalidation childInvalidation)
        {
            if ((childInvalidation & (Invalidation.Visibility | Invalidation.Position | Invalidation.SizeInParentSpace)) > 0)
                return Invalidation.Position | Invalidation.SizeInParentSpace;
            return base.InvalidationEffectByChildren(childInvalidation);
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
