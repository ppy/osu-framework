//Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
//Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using OpenTK;
using OpenTK.Graphics;
using osu.Framework.Cached;
using osu.Framework.DebugUtils;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Transformations;
using osu.Framework.Lists;
using osu.Framework.Timing;

namespace osu.Framework.Graphics
{
    public abstract partial class Drawable : IDisposable, IHasLifetime
    {
        public event Action OnUpdate;

        internal event Action OnInvalidate;

        private LifetimeList<Drawable> children;
        private IEnumerable<Drawable> pendingChildren;
        internal IEnumerable<Drawable> Children
        {
            get { return children; }
            set
            {
                if (!IsLoaded)
                    pendingChildren = value;
                else
                {
                    Clear();
                    Add(value);
                }
            }
        }

        internal IEnumerable<Drawable> CurrentChildren => children.Current;

        private LifetimeList<ITransform> transforms = new LifetimeList<ITransform>(new TransformTimeComparer());
        public LifetimeList<ITransform> Transforms
        {
            get
            {
                ThreadSafety.EnsureUpdateThread();
                return transforms;
            }
        }

        private Vector2 position;
        public Vector2 Position
        {
            get { return position; }
            set
            {
                if (position == value) return;
                position = value;

                Invalidate(Invalidation.ScreenSpaceQuad);
            }
        }

        private Vector2 customOrigin;
        public virtual Vector2 OriginPosition
        {
            get
            {
                if (Origin == Anchor.Custom)
                    return customOrigin;

                if (!HasDefinedSize) return Vector2.Zero;

                Vector2 origin = Vector2.Zero;

                if ((Origin & Anchor.x1) > 0)
                    origin.X += ActualSize.X / 2f;
                else if ((Origin & Anchor.x2) > 0)
                    origin.X += ActualSize.X;

                if ((Origin & Anchor.y1) > 0)
                    origin.Y += ActualSize.Y / 2f;
                else if ((Origin & Anchor.y2) > 0)
                    origin.Y += ActualSize.Y;

                return origin;
            }

            set
            {
                customOrigin = value;
                Origin = Anchor.Custom;
            }
        }

        /// <summary>
        /// Scale which is only applied to Children.
        /// </summary>
        protected Vector2 ContentScale = Vector2.One;

        private Vector2 scale = Vector2.One;

        public Vector2 Scale
        {
            get
            {
                return scale;
            }

            set
            {
                if (scale == value) return;
                scale = value;

                Invalidate(Invalidation.ScreenSpaceQuad);
            }
        }

        private Color4 colour = Color4.White;
        public Color4 Colour
        {
            get { return colour; }

            set
            {
                if (colour == value) return;
                colour = value;

                Invalidate(Invalidation.Colour);
            }
        }

        private Anchor anchor;
        public Anchor Anchor
        {
            get { return anchor; }

            set
            {
                if (anchor == value) return;
                anchor = value;

                Invalidate(Invalidation.ScreenSpaceQuad);
            }
        }

        public virtual string ToolTip { get; set; }

        private float rotation;
        public float Rotation
        {
            get { return rotation; }

            set
            {
                if (value == rotation) return;
                rotation = value;

                Invalidate(Invalidation.ScreenSpaceQuad);
            }
        }

        private float alpha = 1.0f;
        public float Alpha
        {
            get { return alpha; }

            set
            {
                if (alpha == value) return;

                Invalidation i = Invalidation.Colour;
                //we may have changed the visible state.
                if (alpha == 0 || value == 0)
                    i |= Invalidation.Visibility;

                Invalidate(i);

                alpha = value;
            }
        }

        public bool IsDisposable;

        protected Vector2 size = Vector2.One;
        public virtual Vector2 Size
        {
            get { return size; }
            set
            {
                if (size == value)
                    return;
                size = value;

                Invalidate(Invalidation.ScreenSpaceQuad);
            }
        }

        private InheritMode sizeMode;
        public virtual InheritMode SizeMode
        {
            get { return sizeMode; }
            set
            {
                if (value == sizeMode)
                    return;
                sizeMode = value;

                Invalidate(Invalidation.ScreenSpaceQuad);
            }
        }

        private InheritMode positionMode;
        public InheritMode PositionMode
        {
            get { return positionMode; }
            set
            {
                if (value == positionMode)
                    return;
                positionMode = value;

                Invalidate(Invalidation.ScreenSpaceQuad);
            }
        }

        /// <summary>
        /// The real pixel size of this drawable.
        /// </summary>
        public virtual Vector2 ActualSize
        {
            get
            {
                Vector2 size = Size;
                if (SizeMode != InheritMode.None)
                {
                    Vector2 parent = Parent?.ActualSize ?? Vector2.One;
                    if ((SizeMode & InheritMode.X) > 0)
                        size.X *= parent.X;
                    if ((SizeMode & InheritMode.Y) > 0)
                        size.Y *= parent.Y;
                }

                return size;
            }
        }

        /// <summary>
        /// The real pixel position of this drawable.
        /// </summary>
        public Vector2 ActualPosition
        {
            get
            {
                Vector2 pos = Position;
                if (PositionMode != InheritMode.None)
                {
                    Vector2 parent = Parent?.ActualSize ?? Vector2.One;
                    if ((PositionMode & InheritMode.X) > 0)
                        pos.X *= parent.X;
                    if ((PositionMode & InheritMode.Y) > 0)
                        pos.Y *= parent.Y;
                }

                return pos;
            }
        }

        public virtual Quad ScreenSpaceInputQuad => ScreenSpaceDrawQuad;
        private Cached<Quad> screenSpaceDrawQuadBacking = new Cached<Quad>();
        public Quad ScreenSpaceDrawQuad => screenSpaceDrawQuadBacking.IsValid ? screenSpaceDrawQuadBacking.Value : screenSpaceDrawQuadBacking.Refresh(delegate
        {
            Quad result = GetScreenSpaceQuad(DrawQuad);

            //if (PixelSnapping ?? CheckForcedPixelSnapping(result))
            //{
            //    Vector2 adjust = new Vector2(
            //        (float)Math.Round(result.TopLeft.X) - result.TopLeft.X,
            //        (float)Math.Round(result.TopLeft.Y) - result.TopLeft.Y
            //        );

            //    result.TopLeft += adjust;
            //    result.TopRight += adjust;
            //    result.BottomLeft += adjust;
            //    result.BottomRight += adjust;
            //}

            return result;
        });

        private Anchor origin;
        public virtual Anchor Origin
        {
            get
            {
                Anchor origin = this.origin;
                if (flipHorizontal)
                {
                    if ((origin & Anchor.x0) > 0)
                        origin = (origin & ~Anchor.x0) | Anchor.x2;
                    else if ((origin & Anchor.x2) > 0)
                        origin = (origin & ~Anchor.x2) | Anchor.x0;
                }
                if (flipVertical)
                {
                    if ((origin & Anchor.y0) > 0)
                        origin = (origin & ~Anchor.y0) | Anchor.y2;
                    else if ((origin & Anchor.y2) > 0)
                        origin = (origin & ~Anchor.y2) | Anchor.y0;
                }
                return origin;
            }
            set
            {
                if (origin == value)
                    return;
                origin = value;
                Invalidate(Invalidation.ScreenSpaceQuad);
            }
        }

        public float Depth;

        protected virtual bool HasDefinedSize => true;

        public float Width
        {
            get { return Size.X; }
            set { Size = new Vector2(value, Size.Y); }
        }
        public float Height
        {
            get { return Size.Y; }
            set { Size = new Vector2(Size.X, value); }
        }

        protected virtual IFrameBasedClock Clock => clockBacking.IsValid ? clockBacking.Value : clockBacking.Refresh(() => Parent?.Clock);
        private Cached<IFrameBasedClock> clockBacking = new Cached<IFrameBasedClock>();

        protected double Time => Clock?.CurrentTime ?? 0;

        private bool flipVertical;
        public bool FlipVertical
        {
            get { return flipVertical; }
            set
            {
                if (FlipVertical == value)
                    return;
                flipVertical = value;
                Invalidate(Invalidation.ScreenSpaceQuad);
            }
        }

        private bool flipHorizontal;
        public bool FlipHorizontal
        {
            get { return flipHorizontal; }
            set
            {
                if (FlipHorizontal == value)
                    return;
                flipHorizontal = value;
                Invalidate(Invalidation.ScreenSpaceQuad);
            }
        }

        private Cached<bool> isVisibleBacking = new Cached<bool>();
        public virtual bool IsVisible => isVisibleBacking.IsValid ? isVisibleBacking.Value : isVisibleBacking.Refresh(() => Alpha > 0.0001f && Parent?.IsVisible == true);

        private bool? additive;
        public bool? Additive
        {
            get { return additive; }

            set
            {
                if (additive == value) return;

                Invalidate(Invalidation.Colour);

                additive = value;
            }
        }

        protected virtual bool? PixelSnapping { get; set; }

        private Cached<DrawInfo> drawInfoBacking = new Cached<DrawInfo>();
        protected DrawInfo DrawInfo => drawInfoBacking.IsValid ? drawInfoBacking.Value : drawInfoBacking.Refresh(delegate
       {
           DrawInfo di = BaseDrawInfo;

           float alpha = Alpha;
           if (Colour.A > 0 && Colour.A < 1)
               alpha *= Colour.A;

           Color4 colour = new Color4(Colour.R, Colour.G, Colour.B, alpha);

           if (Parent == null)
               di.ApplyTransform(ref di, GetAnchoredPosition(ActualPosition), Scale, Rotation, OriginPosition, colour, new BlendingInfo(Additive ?? false));
           else
               Parent.DrawInfo.ApplyTransform(ref di, GetAnchoredPosition(ActualPosition), Scale * Parent.ContentScale, Rotation, OriginPosition, colour, !Additive.HasValue ? (BlendingInfo?)null : new BlendingInfo(Additive.Value));

           return di;
       });

        protected virtual DrawInfo BaseDrawInfo => new DrawInfo(null, null, null);

        protected virtual Quad DrawQuad
        {
            get
            {
                if (!HasDefinedSize)
                    return new Quad();

                Vector2 s = ActualSize;

                //most common use case gets a shortcut
                if (!flipHorizontal && !flipVertical) return new Quad(0, 0, s.X, s.Y);

                if (flipHorizontal && flipVertical)
                    return new Quad(s.X, s.Y, -s.X, -s.Y);
                if (flipHorizontal)
                    return new Quad(s.X, 0, -s.X, s.Y);
                return new Quad(0, s.Y, s.X, -s.Y);
            }
        }

        public Drawable Parent { get; private set; }

        protected virtual IComparer<Drawable> DepthComparer => new DepthComparer();

        public Drawable()
        {
            children = new LifetimeList<Drawable>(DepthComparer);
        }

        /// <summary>
        /// Checks if this drawable is a child of parent regardless of nesting depth.
        /// </summary>
        /// <param name="parent">The parent to search for.</param>
        /// <returns>If this drawable is a child of parent.</returns>
        public bool IsChildOfRecursive(Drawable parent)
        {
            if (parent == null)
                return false;

            // Do a bottom-up recursion for efficiency
            Drawable currentParent = Parent;
            while (currentParent != null)
            {
                if (currentParent == parent)
                    return true;
                currentParent = currentParent.Parent;
            }

            return false;
        }

        /// <summary>
        /// Checks if this drawable is a parent of child regardless of nesting depth.
        /// </summary>
        /// <param name="child">The child to search for.</param>
        /// <returns>If this drawable is a parent of child.</returns>
        public bool IsParentOfRecursive(Drawable child)
        {
            if (child == null)
                return false;

            // Do a bottom-up recursion for efficiency
            Drawable currentParent = child.Parent;
            while (currentParent != null)
            {
                if (currentParent == this)
                    return true;
                currentParent = currentParent.Parent;
            }

            return false;
        }

        protected Drawable Add(Drawable drawable)
        {
            if (drawable == null)
                return null;

            drawable.changeParent(this);
            children.Add(drawable);

            return drawable;
        }

        protected void Add(IEnumerable<Drawable> collection)
        {
            foreach (Drawable d in collection)
                Add(d);
        }

        protected bool Remove(Drawable p, bool dispose = true)
        {
            if (p == null)
                return false;

            bool result = children.Remove(p);
            p.Parent = null;

            if (dispose && p.IsDisposable)
                p.Dispose();
            else
                p.Invalidate();

            return result;
        }

        protected int RemoveAll(Predicate<Drawable> match, bool dispose = true)
        {
            List<Drawable> toRemove = children.FindAll(match);
            for (int i = 0; i < toRemove.Count; i++)
                Remove(toRemove[i]);

            return toRemove.Count;
        }

        protected void Remove(IEnumerable<Drawable> range, bool dispose = true)
        {
            if (range == null)
                return;

            foreach (Drawable p in range)
            {
                if (p.IsDisposable)
                    p.Dispose();
                Remove(p);
            }
        }

        protected void Clear(bool dispose = true)
        {
            foreach (Drawable t in children)
            {
                if (dispose)
                    t.Dispose();
                t.Parent = null;
            }

            children.Clear();

            Invalidate(Invalidation.ScreenSpaceQuad);
        }

        protected virtual Quad DrawQuadForBounds => DrawQuad;

        protected Cached<Vector2> boundingSizeBacking = new Cached<Vector2>();
        internal Vector2 BoundingSize => boundingSizeBacking.IsValid ? boundingSizeBacking.Value : boundingSizeBacking.Refresh(() =>
        {
            //field will be none when the drawable isn't requesting auto-sizing
            Quad q = Parent.DrawInfo.MatrixInverse * GetScreenSpaceQuad(DrawQuadForBounds);
            Vector2 a = Parent == null ? Vector2.Zero : ((GetAnchoredPosition(Vector2.Zero) * Parent.DrawInfo.Matrix) * Parent.DrawInfo.MatrixInverse);

            Vector2 bounds = new Vector2(0, 0);

            // Without this, 0x0 objects (like FontText with no string) produce weird results.
            // When all vertices of the quad are at the same location, then the object is effectively invisible.
            // Thus we don't need its actual bounding box, but can just assume a size of 0.
            if (q.TopLeft == q.TopRight && q.TopLeft == q.BottomLeft && q.TopLeft == q.BottomRight)
                return bounds;

            foreach (Vector2 p in new[] { q.TopLeft, q.TopRight, q.BottomLeft, q.BottomRight })
            {
                // Compute the clipped offset depending on anchoring.
                Vector2 offset;

                if (Anchor == Anchor.CentreRight || Anchor == Anchor.TopRight || Anchor == Anchor.BottomRight)
                    offset.X = a.X - p.X;
                else if (Anchor == Anchor.CentreLeft || Anchor == Anchor.TopLeft || Anchor == Anchor.BottomLeft)
                    offset.X = p.X - a.X;
                else
                    offset.X = Math.Abs(p.X - a.X);

                if (Anchor == Anchor.BottomCentre || Anchor == Anchor.BottomLeft || Anchor == Anchor.BottomRight)
                    offset.Y = a.Y - p.Y;
                else if (Anchor == Anchor.TopCentre || Anchor == Anchor.TopLeft || Anchor == Anchor.TopRight)
                    offset.Y = p.Y - a.Y;
                else
                    offset.Y = Math.Abs(p.Y - a.Y);

                // Expand bounds according to clipped offset
                bounds.X = Math.Max(bounds.X, offset.X);
                bounds.Y = Math.Max(bounds.Y, offset.Y);
            }

            // When anchoring an object at the center of the parent, then the parent's size needs to be twice as big
            // as the child's size.
            switch (Anchor)
            {
                case Anchor.TopCentre:
                case Anchor.Centre:
                case Anchor.BottomCentre:
                    bounds.X *= 2;
                    break;
            }

            switch (Anchor)
            {
                case Anchor.CentreLeft:
                case Anchor.Centre:
                case Anchor.CentreRight:
                    bounds.Y *= 2;
                    break;
            }

            return bounds;
        });

        internal DrawNode GenerateDrawNodeSubtree()
        {
            DrawNode node = BaseDrawNode;

            foreach (Drawable child in children.Current)
                if (child.IsVisible)
                    node.Children.Add(child.GenerateDrawNodeSubtree());

            return node;
        }

        protected virtual DrawNode BaseDrawNode => new DrawNode(DrawInfo);

        /// <summary>
        /// Perform any layout changes just before autosize is calculated.		
        /// </summary>		
        protected virtual void UpdateLayout() { }

        /// <summary>
        /// Updates the life status of children according to their IsAlive property.
        /// </summary>
        /// <returns>True iff the life status of at least one child changed.</returns>
        protected virtual bool UpdateChildrenLife()
        {
            bool childChangedStatus = false;
            foreach (Drawable child in children)
            {
                bool isAlive = child.IsAlive;
                if (isAlive != child.wasAliveLastUpdate)
                {
                    child.wasAliveLastUpdate = isAlive;
                    childChangedStatus = true;
                }
            }

            children.Update(Time);

            return childChangedStatus;
        }

        internal virtual void UpdateSubTree()
        {
            transformationDelay = 0;

            //todo: this should be moved to after the IsVisible condition once we have TOL for transformations (and some better logic).
            updateTransforms();

            if (!IsVisible)
                return;

            Update();
            OnUpdate?.Invoke();

            UpdateChildrenLife();

            foreach (Drawable child in children.Current)
                child.UpdateSubTree();

            UpdateLayout();
        }

        protected virtual void Update()
        {
        }

        protected virtual Quad GetScreenSpaceQuad(Quad input)
        {
            return DrawInfo.Matrix * input;
        }

        public Quad GetSpaceQuadIn(Drawable parent)
        {
            return parent.DrawInfo.MatrixInverse * ScreenSpaceDrawQuad;
        }

        protected virtual bool CheckForcedPixelSnapping(Quad screenSpaceQuad)
        {
            return false;
        }

        private void changeParent(Drawable parent)
        {
            if (Parent == parent)
                return;

            Parent?.Remove(this, false);
            Parent = parent;

            changeRoot(Parent?.Game);
        }

        private void changeRoot(Game root)
        {
            if (root == null) return;

            Game = root;
            clockBacking.Invalidate();

            children.ForEach(c => c.changeRoot(root));
        }

        /// <summary>
        /// The time at which this drawable becomes valid (and is considered for drawing).
        /// </summary>
        public double LifetimeStart { get; set; } = double.MinValue;

        /// <summary>
        /// The time at which this drawable is no longer valid (and is considered for disposal).
        /// </summary>
        public double LifetimeEnd { get; set; } = double.MaxValue;

        /// <summary>
        /// Whether this drawable is alive.
        /// </summary>
        public bool IsAlive
        {
            get
            {
                if (Parent == null) return false;

                if (LifetimeStart == double.MinValue && LifetimeEnd == double.MaxValue)
                    return true;

                double t = Time;
                return t >= LifetimeStart && t < LifetimeEnd;
            }
        }

        private bool wasAliveLastUpdate = false;

        /// <summary>
        /// Whether to remove the drawable from its parent's children when it's not alive.
        /// </summary>
        public virtual bool RemoveWhenNotAlive => Parent == null || Time > LifetimeStart;

        /// <summary>
        /// Override to add delayed load abilities (ie. using IsAlive)
        /// </summary>
        public virtual bool IsLoaded => loaded;
        private bool loaded;

        public virtual void Load()
        {
            if (pendingChildren != null)
            {
                Add(pendingChildren);
                pendingChildren = null;
            }

            loaded = true;
            Invalidate();
        }

        private void updateTransformsOfType(Type specificType)
        {
            foreach (ITransform t in transforms.Current)
                if (t.GetType() == specificType)
                    t.Apply(this);
        }

        /// <summary>
        /// Process updates to this drawable based on loaded transforms.
        /// </summary>
        /// <returns>Whether we should draw this drawable.</returns>
        private void updateTransforms()
        {
            var removed = transforms.Update(Time);

            foreach (ITransform t in removed)
                t.Apply(this); //make sure we apply one last time.

            foreach (ITransform t in transforms.Current)
                t.Apply(this);
        }

        /// <summary>
        /// Invalidates draw matrix and autosize caches.
        /// </summary>
        /// <returns>If the invalidate was actually necessary.</returns>
        public virtual bool Invalidate(Invalidation invalidation = Invalidation.All, Drawable source = null, bool shallPropagate = true)
        {
            if (invalidation == Invalidation.None)
                return false;

            ThreadSafety.EnsureUpdateThread();

            OnInvalidate?.Invoke();

            if (shallPropagate && Parent != null && source != Parent)
                Parent.Invalidate(Parent.InvalidationEffectByChildren(invalidation), this, false);

            bool alreadyInvalidated = true;

            if ((invalidation & Invalidation.SizeInParentSpace) > 0)
                alreadyInvalidated &= !boundingSizeBacking.Invalidate();

            // Either ScreenSize OR ScreenPosition
            if ((invalidation & Invalidation.ScreenSpaceQuad) > 0)
                alreadyInvalidated &= !screenSpaceDrawQuadBacking.Invalidate();

            // Either ScreenSize OR ScreenPosition OR Colour
            if ((invalidation & Invalidation.DrawInfo) > 0)
                alreadyInvalidated &= !drawInfoBacking.Invalidate();

            if ((invalidation & Invalidation.Visibility) > 0)
                alreadyInvalidated &= !isVisibleBacking.Invalidate();

            if (alreadyInvalidated || !shallPropagate)
                return !alreadyInvalidated;

            if (children != null)
            {
                foreach (var c in children)
                {
                    Debug.Assert(c != source);

                    Invalidation childInvalidation = invalidation;
                    //if (c.SizeMode == InheritMode.None)
                        childInvalidation = childInvalidation & ~Invalidation.SizeInParentSpace;

                    c.Invalidate(childInvalidation, this);
                }
            }

            return !alreadyInvalidated;
        }

        protected Vector2 GetAnchoredPosition(Vector2 pos)
        {
            if (!HasDefinedSize || Anchor == Anchor.TopLeft)
                return pos;

            Vector2 parentSize = Parent?.ActualSize ?? Vector2.Zero;

            if ((Anchor & Anchor.x1) > 0)
                pos.X += parentSize.X / 2f;
            else if ((Anchor & Anchor.x2) > 0)
                pos.X = parentSize.X - pos.X;

            if ((Anchor & Anchor.y1) > 0)
                pos.Y += parentSize.Y / 2f;
            else if ((Anchor & Anchor.y2) > 0)
                pos.Y = parentSize.Y - pos.Y;

            return pos;
        }

        ~Drawable()
        {
            if (Game != null)
                //todo: check this scheduler call is actually required.
                Game.Scheduler.Add(() => Dispose(false));
            else
                Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private bool isDisposed;

        protected virtual void Dispose(bool isDisposing)
        {
            if (isDisposed)
                return;
            isDisposed = true;

            Parent = null;

            if (IsDisposable)
                OnUpdate = null;
        }

        public override string ToString()
        {
            string shortClass = base.ToString();
            shortClass = shortClass.Substring(shortClass.LastIndexOf('.') + 1);
            if (HasDefinedSize)
                return $@"{shortClass} pos {Position} size {Size}";
            else
                return $@"{shortClass} pos {Position} size -uncalculated-";
        }

        public virtual Drawable Clone()
        {
            Drawable thisNew = (Drawable)MemberwiseClone();

            thisNew.children = new LifetimeList<Drawable>(DepthComparer);
            children.ForEach(c => thisNew.children.Add(c.Clone()));

            thisNew.transforms = new LifetimeList<ITransform>(new TransformTimeComparer());
            Transforms.Select(t => thisNew.transforms.Add(t.Clone()));

            thisNew.drawInfoBacking.Invalidate();
            thisNew.boundingSizeBacking.Invalidate();

            return thisNew;
        }

        protected Game Game;

        protected virtual Invalidation InvalidationEffectByChildren(Invalidation childInvalidation)
        {
            return Invalidation.None;
        }
    }

    /// <summary>
    /// Specifies which type of properties are being invalidated.
    /// </summary>
    [Flags]
    public enum Invalidation
    {
        // Individual types
        Position = 1 << 0,
        SizeInParentSpace = 1 << 1,
        Visibility = 1 << 2,
        Colour = 1 << 3,

        // Combinations
        ScreenSpaceQuad = Position | SizeInParentSpace,
        DrawInfo = ScreenSpaceQuad | Colour,

        // Meta
        None = 0,
        All = Position | SizeInParentSpace | Visibility | Colour,
    };

    /// <summary>
    /// General enum to specify an "anchor" or "origin" point from the standard 9 points on a rectangle.
    /// x and y counterparts can be accessed using bitwise flags.
    /// </summary>
    [Flags]
    public enum Anchor
    {
        TopLeft = y0 | x0,
        TopCentre = y0 | x1,
        TopRight = y0 | x2,

        CentreLeft = y1 | x0,
        Centre = y1 | x1,
        CentreRight = y1 | x2,

        BottomLeft = y2 | x0,
        BottomCentre = y2 | x1,
        BottomRight = y2 | x2,

        /// <summary>
        /// The vertical counterpart is at "Top" position.
        /// </summary>
        y0 = 0,
        /// <summary>
        /// The vertical counterpart is at "Centre" position.
        /// </summary>
        y1 = 1,
        /// <summary>
        /// The vertical counterpart is at "Bottom" position.
        /// </summary>
        y2 = 2,

        /// <summary>
        /// The horizontal counterpart is at "Left" position.
        /// </summary>
        x0 = 0,
        /// <summary>
        /// The horizontal counterpart is at "Centre" position.
        /// </summary>
        x1 = 4,
        /// <summary>
        /// The horizontal counterpart is at "Right" position.
        /// </summary>
        x2 = 8,

        /// <summary>
        /// The user is manually updating the outcome, so we shouldn't.
        /// </summary>
        Custom = 32,
    }

    [Flags]
    public enum InheritMode
    {
        None = 0,

        X = 1 << 0,
        Y = 1 << 1,

        XY = X | Y
    }

    public class DepthComparer : IComparer<Drawable>
    {
        public int Compare(Drawable x, Drawable y)
        {
            return x.Depth.CompareTo(y.Depth);
        }
    }

    public class DepthComparerReverse : IComparer<Drawable>
    {
        public int Compare(Drawable x, Drawable y)
        {
            if (x.Depth == y.Depth) return 1;
            return x.Depth.CompareTo(y.Depth);
        }
    }
}
