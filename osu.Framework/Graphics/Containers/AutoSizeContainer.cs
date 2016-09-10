//Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
//Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using osu.Framework.Graphics.Primitives;
using OpenTK;

namespace osu.Framework.Graphics.Containers
{
    public class AutoSizeContainer : Container
    {
        protected bool RequireAutoSize => autoSizeUpdatePending && SizeMode != InheritMode.XY;

        internal event Action OnAutoSize;

        private bool autoSizeUpdatePending;

        public override bool Invalidate(bool affectsSize = true, bool affectsPosition = true, Drawable source = null)
        {
            if (affectsSize)
                autoSizeUpdatePending = true;

            bool alreadyInvalidated = base.Invalidate(affectsSize, affectsPosition, source);

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
                foreach (Drawable c in Children)
                {
                    if (!c.IsVisible)
                        continue;

                    Vector2 boundingSize = c.GetBoundingSize(this);
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

        internal override void UpdateSubTree()
        {
            base.UpdateSubTree();

            if (RequireAutoSize)
            {
                Vector2 b = GetBoundingSize(this);
                Vector2 newSize = new Vector2((SizeMode & InheritMode.X) > 0 ? Size.X : b.X, (SizeMode & InheritMode.Y) > 0 ? Size.Y : b.Y);

                // TODO: Figure out why this if check introduces flickering.
                //if (newSize != Size)
                {
                    size = newSize;

                    // Once we have a better general implementation of "Invalidate()", then we can hopefully get rid of "InvalidateDrawInfoAndDrawQuad"
                    //Invalidate();
                    InvalidateDrawInfoAndDrawQuad();
                }

                autoSizeUpdatePending = false;
                OnAutoSize?.Invoke();
            }
        }

        public override Vector2 ActualSize
        {
            get
            {
                if (HasDefinedSize)
                    return base.ActualSize;

                if (SizeMode == InheritMode.None)
                    return new Vector2(0);

                var actual = base.ActualSize;
                
                return new Vector2((SizeMode & InheritMode.X) > 0 ? actual.X : 0, (SizeMode & InheritMode.Y) > 0 ? actual.Y : 0);
            }
        }

        protected override bool HasDefinedSize => !RequireAutoSize;

        protected override bool ChildrenShouldInvalidate => true;
    }
}
