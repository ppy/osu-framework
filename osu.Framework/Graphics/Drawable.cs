// Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Cached;
using osu.Framework.DebugUtils;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Transformations;
using osu.Framework.Lists;
using osu.Framework.Timing;
using OpenTK;
using OpenTK.Graphics;
using osu.Framework.Graphics.Containers;

namespace osu.Framework.Graphics
{
    public abstract partial class Drawable : IDisposable, IHasLifetime
    {
        public event Action OnUpdate;

        internal event Action OnInvalidate;

        /// <summary>
        /// A name used to identify this Drawable internally.
        /// </summary>
        public virtual string Name => string.Empty;

        private LifetimeList<ITransform> transforms;

        /// <summary>
        /// The list of transforms applied to this drawable. Initialised on first access.
        /// </summary>
        public LifetimeList<ITransform> Transforms
        {
            get
            {
                ThreadSafety.EnsureUpdateThread();

                if (transforms == null)
                {
                    transforms = new LifetimeList<ITransform>(new TransformTimeComparer());
                    transforms.OnRemoved += transforms_OnRemoved;
                }

                return transforms;
            }
        }

        private Axes relativePositionAxes;

        public Axes RelativePositionAxes
        {
            get { return relativePositionAxes; }
            set
            {
                if (value == relativePositionAxes)
                    return;
                relativePositionAxes = value;

                Invalidate(Invalidation.Position | Invalidation.SizeInParentSpace);
            }
        }

        internal Vector2 InternalPosition;

        /// <summary>
        /// The getter returns position of this drawable in its parent's space.
        /// The setter accepts relative values in inheriting dimensions.
        /// </summary>
        public Vector2 Position
        {
            get
            {
                Vector2 pos = InternalPosition;
                if (RelativePositionAxes != Axes.None)
                {
                    Vector2 parent = Parent?.Size ?? Vector2.One;
                    if ((RelativePositionAxes & Axes.X) > 0)
                        pos.X *= parent.X;
                    if ((RelativePositionAxes & Axes.Y) > 0)
                        pos.Y *= parent.Y;
                }

                return pos;
            }
            set
            {
                if (InternalPosition == value) return;
                InternalPosition = value;

                Invalidate(Invalidation.Position | Invalidation.SizeInParentSpace);
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
                    origin.X += Size.X / 2f;
                else if ((Origin & Anchor.x2) > 0)
                    origin.X += Size.X;

                if ((Origin & Anchor.y1) > 0)
                    origin.Y += Size.Y / 2f;
                else if ((Origin & Anchor.y2) > 0)
                    origin.Y += Size.Y;

                return origin;
            }

            set
            {
                customOrigin = value;
                Origin = Anchor.Custom;
            }
        }

        private Vector2 scale = Vector2.One;

        public Vector2 Scale
        {
            get { return scale; }

            set
            {
                if (scale == value) return;
                scale = value;

                Invalidate(Invalidation.Position | Invalidation.SizeInParentSpace);
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

                Invalidate(Invalidation.Position | Invalidation.SizeInParentSpace);
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

                Invalidate(Invalidation.Position | Invalidation.SizeInParentSpace);
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

        internal Vector2 InternalSize;

        /// <summary>
        /// The getter returns size of this drawable in its parent's space.
        /// The setter accepts relative values in inheriting dimensions.
        /// </summary>
        public virtual Vector2 Size
        {
            get
            {
                Vector2 size = InternalSize;
                if (RelativeSizeAxes != Axes.None)
                {
                    Vector2 parent = Parent?.Size ?? Vector2.One;
                    if ((RelativeSizeAxes & Axes.X) > 0)
                        size.X = size.X * parent.X;
                    if ((RelativeSizeAxes & Axes.Y) > 0)
                        size.Y = size.Y * parent.Y;
                }

                return size;
            }
            set
            {
                if (InternalSize == value) return;
                InternalSize = value;

                Invalidate(Invalidation.Position | Invalidation.SizeInParentSpace);
            }
        }

        private Axes relativeSizeAxes;

        public virtual Axes RelativeSizeAxes
        {
            get { return relativeSizeAxes; }
            set
            {
                if (value == relativeSizeAxes)
                    return;

                if (InternalSize == Vector2.Zero)
                    InternalSize = Vector2.One;

                relativeSizeAxes = value;

                Invalidate(Invalidation.Position | Invalidation.SizeInParentSpace);
            }
        }

        public virtual Quad ScreenSpaceInputQuad => ScreenSpaceDrawQuad;
        private Cached<Quad> screenSpaceDrawQuadBacking = new Cached<Quad>();

        public Quad ScreenSpaceDrawQuad => screenSpaceDrawQuadBacking.EnsureValid()
            ? screenSpaceDrawQuadBacking.Value
            : screenSpaceDrawQuadBacking.Refresh(delegate
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
                    Debug.Assert((origin & Anchor.x1) == 0, @"Can't flip with a centre origin set");
                    origin ^= Anchor.x2;
                }

                if (flipVertical)
                {
                    Debug.Assert((origin & Anchor.y1) == 0, @"Can't flip with a centre origin set");
                    origin ^= Anchor.y2;
                }

                return origin;
            }
            set
            {
                if (origin == value)
                    return;
                origin = value;
                Invalidate(Invalidation.Position | Invalidation.SizeInParentSpace);
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

        protected virtual IFrameBasedClock Clock => clockBacking.EnsureValid() ? clockBacking.Value : clockBacking.Refresh(() => Parent?.Clock);
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
                Invalidate(Invalidation.Position | Invalidation.SizeInParentSpace);
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
                Invalidate(Invalidation.Position | Invalidation.SizeInParentSpace);
            }
        }

        private Cached<bool> isVisibleBacking = new Cached<bool>();
        public virtual bool IsVisible => isVisibleBacking.EnsureValid() ? isVisibleBacking.Value : isVisibleBacking.Refresh(() => Alpha > 0.0001f && Parent?.IsVisible == true);

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

        protected DrawInfo DrawInfo => drawInfoBacking.EnsureValid() ? drawInfoBacking.Value : drawInfoBacking.Refresh(delegate
            {
                DrawInfo di = BaseDrawInfo;

                float alpha = Alpha;
                if (Colour.A > 0 && Colour.A < 1)
                    alpha *= Colour.A;

                Color4 colour = new Color4(Colour.R, Colour.G, Colour.B, alpha);

                if (Parent == null)
                    di.ApplyTransform(ref di, GetAnchoredPosition(Position), Scale, Rotation, OriginPosition, colour, new BlendingInfo(Additive ?? false));
                else
                    Parent.DrawInfo.ApplyTransform(ref di, GetAnchoredPosition(Position), Scale * Parent.ChildrenScale, Rotation, OriginPosition, colour,
                              !Additive.HasValue ? (BlendingInfo?)null : new BlendingInfo(Additive.Value));

                return di;
            });

        protected virtual DrawInfo BaseDrawInfo => new DrawInfo(null, null, null);

        protected virtual Quad DrawQuad
        {
            get
            {
                if (!HasDefinedSize)
                    return new Quad();

                Vector2 s = Size;

                //most common use case gets a shortcut
                if (!flipHorizontal && !flipVertical) return new Quad(0, 0, s.X, s.Y);

                if (flipHorizontal && flipVertical)
                    return new Quad(s.X, s.Y, -s.X, -s.Y);
                if (flipHorizontal)
                    return new Quad(s.X, 0, -s.X, s.Y);
                return new Quad(0, s.Y, s.X, -s.Y);
            }
        }

        public Container Parent { get; set; }

        protected virtual IComparer<Drawable> DepthComparer => new DepthComparer();

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



        protected virtual Quad DrawQuadForBounds => DrawQuad;

        private Cached<Vector2> boundingSizeBacking = new Cached<Vector2>();

        internal Vector2 BoundingSize => boundingSizeBacking.EnsureValid()
            ? boundingSizeBacking.Value
            : boundingSizeBacking.Refresh(() =>
            {
                //field will be none when the drawable isn't requesting auto-sizing
                Quad q = GetScreenSpaceQuad(DrawQuadForBounds) * Parent.DrawInfo.MatrixInverse;

                Vector2 bounds = new Vector2(0, 0);

                // Without this, 0x0 objects (like FontText with no string) produce weird results.
                // When all vertices of the quad are at the same location, then the object is effectively invisible.
                // Thus we don't need its actual bounding box, but can just assume a size of 0.
                if (q.TopLeft == q.TopRight && q.TopLeft == q.BottomLeft && q.TopLeft == q.BottomRight)
                    return bounds;

                Vector2 a = GetAnchoredPosition(Vector2.Zero);

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


        private List<DrawNode> validDrawNodes = new List<DrawNode>();

        /// <summary>
        /// Generates the DrawNode for ourselves.
        /// </summary>
        /// <param name="node">An existing DrawNode which may need to be updated, or null if a node needs to be created.</param>
        /// <returns>A complete and updated DrawNode.</returns>
        internal virtual DrawNode GenerateDrawNodeSubtree(DrawNode node = null)
        {
            if (node == null)
            {
                //we don't have a previous node, so we need to initialise fresh.
                node = CreateDrawNode();
                node.Drawable = this;
            }

            if (!validDrawNodes.Contains(node))
            {
                //we need to update the node if it has been invalidated.
                ApplyDrawNode(node);
                validDrawNodes.Add(node);
            }

            return node;
        }

        protected virtual void ApplyDrawNode(DrawNode node)
        {
            node.DrawInfo = DrawInfo;
            node.Drawable = this;
        }

        protected virtual DrawNode CreateDrawNode() => new DrawNode();

        /// <summary>
        /// Updates this drawable, once every frame.
        /// </summary>
        /// <returns>False if the drawable should not be updated.</returns>
        internal virtual bool UpdateSubTree()
        {
            transformationDelay = 0;

            //todo: this should be moved to after the IsVisible condition once we have TOL for transformations (and some better logic).
            updateTransforms();

            if (!IsVisible)
                return false;

            Update();
            OnUpdate?.Invoke();
            return true;
        }

        protected virtual void Update()
        {
        }

        protected virtual Quad GetScreenSpaceQuad(Quad input)
        {
            return input * DrawInfo.Matrix;
        }

        protected virtual bool CheckForcedPixelSnapping(Quad screenSpaceQuad)
        {
            return false;
        }

        internal void ChangeParent(Container parent)
        {
            if (Parent != parent)
            {
                Parent?.Remove(this, false);
                Parent = parent;
            }

            if (Parent?.Game != Game)
                ChangeRoot(Parent?.Game);
        }

        internal virtual void ChangeRoot(Game root)
        {
            if (root == null) return;

            Game = root;
            clockBacking.Invalidate();
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
        public virtual bool IsAlive
        {
            get
            {
                if (Parent == null || Game == null) return false;

                if (LifetimeStart == double.MinValue && LifetimeEnd == double.MaxValue)
                    return true;

                double t = Time;
                return t >= LifetimeStart && t < LifetimeEnd;
            }
        }

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
            loaded = true;
            Invalidate();
        }

        private void updateTransformsOfType(Type specificType)
        {
            foreach (ITransform t in Transforms.AliveItems)
                if (t.GetType() == specificType)
                    t.Apply(this);
        }

        /// <summary>
        /// Process updates to this drawable based on loaded transforms.
        /// </summary>
        /// <returns>Whether we should draw this drawable.</returns>
        private void updateTransforms()
        {
            if (transforms == null || transforms.Count == 0) return;

            transforms.Update();

            foreach (ITransform t in transforms.AliveItems)
                t.Apply(this);
        }

        private void transforms_OnRemoved(ITransform t)
        {
            t.Apply(this); //make sure we apply one last time.
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

            // Either ScreenSize OR ScreenPosition OR Colour
            if ((invalidation & (Invalidation.Position | Invalidation.Colour | Invalidation.SizeInParentSpace)) > 0)
            {
                if ((invalidation & (Invalidation.Position | Invalidation.SizeInParentSpace)) > 0)
                {
                    if ((invalidation & Invalidation.SizeInParentSpace) > 0)
                        alreadyInvalidated &= !boundingSizeBacking.Invalidate();

                    alreadyInvalidated &= !screenSpaceDrawQuadBacking.Invalidate();
                }

                alreadyInvalidated &= !drawInfoBacking.Invalidate();
                validDrawNodes.Clear();
            }

            if ((invalidation & Invalidation.Visibility) > 0)
            {
                alreadyInvalidated &= !isVisibleBacking.Invalidate();
                validDrawNodes.Clear();
            }

            return !alreadyInvalidated;
        }

        protected Vector2 GetAnchoredPosition(Vector2 pos)
        {
            if (!HasDefinedSize || Anchor == Anchor.TopLeft)
                return pos;

            Vector2 parentSize = Parent?.Size ?? Vector2.Zero;

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

            if (!string.IsNullOrEmpty(Name))
                shortClass = $@"{Name} ({shortClass})";

            if (HasDefinedSize)
                return $@"{shortClass} ({Position.X:#,0},{Position.Y:#,0}) @ {Size.X:#,0}x{Size.Y:#,0}";
            else
                return $@"{shortClass} ({Position.X:#,0},{Position.Y:#,0}) size -uncalculated-";
        }

        public virtual Drawable Clone()
        {
            Drawable thisNew = (Drawable)MemberwiseClone();

            if (transforms != null)
            {
                thisNew.transforms = new LifetimeList<ITransform>(new TransformTimeComparer());
                Transforms.Select(t => thisNew.transforms.Add(t.Clone()));
            }

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

        // Meta
        None = 0,
        All = Position | SizeInParentSpace | Visibility | Colour,
    }

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
    public enum Axes
    {
        None = 0,

        X = 1 << 0,
        Y = 1 << 1,

        Both = X | Y
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
