//Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
//Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using osu.Framework.Graphics.Primitives;
using OpenTK;
using System.Diagnostics;
using osu.Framework.Cached;

namespace osu.Framework.Graphics.Containers
{
    public class AutoSizeContainer : Container
    {
        protected bool RequireAutoSize => SizeMode != InheritMode.XY && !autoSize.IsValid;

        internal event Action OnAutoSize;

        private Cached<Vector2> autoSize = new Cached<Vector2>();

        public override bool Invalidate(Invalidation invalidation = Invalidation.All, Drawable source = null, bool shallPropagate = true)
        {
            if ((invalidation & Invalidation.SizeInParentSpace) > 0)
                autoSize.Invalidate();

            bool alreadyInvalidated = base.Invalidate(invalidation, source, shallPropagate);

            return !alreadyInvalidated;
        }

        protected override Quad DrawQuadForBounds
        {
            get
            {
                if (SizeMode == InheritMode.XY) return base.DrawQuadForBounds;

                Vector2 size = Vector2.Zero;
                Vector2 maxInheritingSize = Vector2.One;

                // Find the maximum width/height of children
                foreach (Drawable c in CurrentChildren)
                {
                    if (!c.IsVisible)
                        continue;

                    Vector2 boundingSize = c.BoundingSize;
                    Vector2 inheritingSize = c.Size * c.Scale * ContentScale;

                    if ((c.SizeMode & InheritMode.X) == 0)
                        size.X = Math.Max(size.X, boundingSize.X);
                    else
                        maxInheritingSize.X = Math.Max(maxInheritingSize.X, inheritingSize.X);

                    if ((c.SizeMode & InheritMode.Y) == 0)
                        size.Y = Math.Max(size.Y, boundingSize.Y);
                    else
                        maxInheritingSize.Y = Math.Max(maxInheritingSize.Y, inheritingSize.Y);
                }

                if (size.X == 0) size.X = Parent?.ActualSize.X ?? 0;
                if (size.Y == 0) size.Y = Parent?.ActualSize.Y ?? 0;

                if ((SizeMode & InheritMode.X) > 0)
                    size.X = ActualSize.X;
                if ((SizeMode & InheritMode.Y) > 0)
                    size.Y = ActualSize.Y;

                return new Quad(0, 0, size.X * maxInheritingSize.X, size.Y * maxInheritingSize.Y);
            }
        }

        protected override bool UpdateChildrenLife()
        {
            bool childChangedStatus = base.UpdateChildrenLife();
            if (childChangedStatus)
                Invalidate(Invalidation.ScreenSpaceQuad);

            return childChangedStatus;
        }

        internal override void UpdateSubTree()
        {
            base.UpdateSubTree();

            if (!autoSize.IsValid)
                autoSize.Refresh(delegate
                {
                    Vector2 b = DrawQuadForBounds.BottomRight;

                    Size = new Vector2((SizeMode & InheritMode.X) > 0 ? Size.X : b.X, (SizeMode & InheritMode.Y) > 0 ? Size.Y : b.Y);

                    //note that this is called before autoSize becomes valid. may be something to consider down the line.
                    //might work better to add an OnRefresh event in Cached<> and invoke there.
                    OnAutoSize?.Invoke();

                    return b;
                });
        }

        public override Drawable Add(Drawable drawable)
        {
            Drawable result = base.Add(drawable);
            if (result != null)
                Invalidate(Invalidation.ScreenSpaceQuad);

            return result;
        }

        public override bool Remove(Drawable p, bool dispose = true)
        {
            bool result = base.Remove(p, dispose);
            if (result)
                Invalidate(Invalidation.ScreenSpaceQuad);

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
            if ((childInvalidation & (Invalidation.Visibility | Invalidation.ScreenSpaceQuad)) > 0)
                return Invalidation.ScreenSpaceQuad;
            else
                return base.InvalidationEffectByChildren(childInvalidation);
        }

        public virtual InheritMode SizeMode
        {
            get { return base.SizeMode; }
            set
            {
                Debug.Assert(SizeMode != InheritMode.XY);
                base.SizeMode = value;
            }
        }
    }
}
