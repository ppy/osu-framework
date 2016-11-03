// Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.DebugUtils;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Transformations;
using osu.Framework.Lists;
using osu.Framework.Timing;
using OpenTK;
using OpenTK.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Threading;
using System.Threading;
using System.Threading.Tasks;
using osu.Framework.Caching;
using osu.Framework.Graphics.Shaders;
using osu.Framework.Logging;
using osu.Framework.Platform;
using osu.Framework.Statistics;

namespace osu.Framework.Graphics
{
    public abstract partial class Drawable : IDisposable, IHasLifetime
    {
        public event Action OnUpdate;

        /// <summary>
        /// DO NOT USE THIS.
        /// (Right now, this is required by the draw visualizer.
        ///  Attempting to use this for anything else will likely result in bad behaviour.)
        /// </summary>
        internal event Action OnInvalidate;

        /// <summary>
        /// A name used to identify this Drawable internally.
        /// </summary>
        public virtual string Name => string.Empty;

        private static AtomicCounter creationCounter = new AtomicCounter();
        internal long CreationID;

        public Drawable()
        {
            CreationID = creationCounter.Increment();
        }

        /// <summary>
        /// A lazily-initialized scheduler used to schedule tasks to be invoked in future Update calls.
        /// </summary>
        private Scheduler scheduler;
        private Thread mainThread;
        protected Scheduler Scheduler
        {
            get
            {
                if (scheduler == null)
                    //mainThread could be null at this point.
                    scheduler = new Scheduler(mainThread);

                return scheduler;
            }
        }

        private LifetimeList<ITransform> transforms;

        /// <summary>
        /// The list of transforms applied to this drawable. Initialised on first access.
        /// </summary>
        public LifetimeList<ITransform> Transforms
        {
            get
            {
                if (transforms == null)
                {
                    transforms = new LifetimeList<ITransform>(new TransformTimeComparer());
                    transforms.Removed += transforms_OnRemoved;
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

                Invalidate(Invalidation.Geometry);
            }
        }


        private Vector2 position;
        public Vector2 Position
        {
            get
            {
                return position;
            }

            set
            {
                if (position == value) return;
                position = value;

                Invalidate(Invalidation.Geometry);
            }
        }

        /// <summary>
        /// The getter returns position of this drawable in its parent's space.
        /// The setter accepts relative values in inheriting dimensions.
        /// </summary>
        public Vector2 DrawPosition
        {
            get
            {
                Vector2 pos = Position;
                if (RelativePositionAxes != Axes.None)
                {
                    Vector2 parent = Parent?.ChildSize ?? Vector2.One;
                    if ((RelativePositionAxes & Axes.X) > 0)
                        pos.X *= parent.X;
                    if ((RelativePositionAxes & Axes.Y) > 0)
                        pos.Y *= parent.Y;
                }

                return pos;
            }
        }

        private Vector2 customOrigin;

        public virtual Vector2 OriginPosition
        {
            get
            {
                if (Origin == Anchor.Custom)
                    return customOrigin;

                Vector2 origin = Vector2.Zero;

                if ((Origin & Anchor.x1) > 0)
                    origin.X += DrawSize.X / 2f;
                else if ((Origin & Anchor.x2) > 0)
                    origin.X += DrawSize.X;

                if ((Origin & Anchor.y1) > 0)
                    origin.Y += DrawSize.Y / 2f;
                else if ((Origin & Anchor.y2) > 0)
                    origin.Y += DrawSize.Y;

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

                Invalidate(Invalidation.Geometry);
            }
        }

        private Vector2 shear = Vector2.Zero;

        public Vector2 Shear
        {
            get { return shear; }

            set
            {
                if (shear == value) return;
                shear = value;

                Invalidate(Invalidation.Geometry);
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

        private Anchor anchor = Anchor.TopLeft;

        public Anchor Anchor
        {
            get { return anchor; }

            set
            {
                if (anchor == value) return;

                Debug.Assert(value != 0, "Cannot set anchor to 0.");
                anchor = value;

                Invalidate(Invalidation.Geometry);
            }
        }

        private float rotation;

        public float Rotation
        {
            get { return rotation; }

            set
            {
                if (value == rotation) return;
                rotation = value;

                Invalidate(Invalidation.Geometry);
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
                if (alpha <= visibility_cutoff || value <= visibility_cutoff)
                    i |= Invalidation.Visibility;

                Invalidate(i);

                alpha = value;
            }
        }

        private float width;
        private float height;

        public virtual float Width
        {
            get { return width; }
            set
            {
                if (width == value) return;
                width = value;

                Invalidate(Invalidation.Geometry);
            }
        }

        public float DrawWidth
        {
            get { return DrawSize.X; }
        }

        public virtual float Height
        {
            get { return height; }
            set
            {
                if (height == value) return;
                height = value;

                Invalidate(Invalidation.Geometry);
            }
        }

        public float DrawHeight
        {
            get { return DrawSize.Y; }
        }

        private Vector2 size
        {
            get { return new Vector2(width, height); }
            set { width = value.X; height = value.Y; }
        }

        public virtual Vector2 Size
        {
            get
            {
                return size;
            }

            set
            {
                if (size == value) return;
                size = value;

                Invalidate(Invalidation.Geometry);
            }
        }


        /// <summary>
        /// The getter returns size of this drawable in its parent's space.
        /// The setter accepts relative values in inheriting dimensions.
        /// </summary>
        public virtual Vector2 DrawSize
        {
            get
            {
                Vector2 size = Size;
                if (RelativeSizeAxes != Axes.None)
                {
                    Vector2 parent = Parent?.ChildSize ?? Vector2.One;
                    if ((RelativeSizeAxes & Axes.X) > 0)
                        size.X = size.X * parent.X;
                    if ((RelativeSizeAxes & Axes.Y) > 0)
                        size.Y = size.Y * parent.Y;
                }

                return size;
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

                if ((value & Axes.X) > 0 && Width == 0) Width = 1;
                if ((value & Axes.Y) > 0 && Height == 0) Height = 1;

                relativeSizeAxes = value;

                Invalidate(Invalidation.Geometry);
            }
        }


        private Cached<Quad> screenSpaceDrawQuadBacking = new Cached<Quad>();

        public virtual Quad ScreenSpaceDrawQuad => screenSpaceDrawQuadBacking.EnsureValid()
            ? screenSpaceDrawQuadBacking.Value
            : screenSpaceDrawQuadBacking.Refresh(delegate
            {
                Quad result = ToScreenSpace(DrawRectangle);

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

        private Anchor origin = Anchor.TopLeft;

        public virtual Anchor Origin
        {
            get
            {
                return origin;
            }
            set
            {
                if (origin == value)
                    return;

                Debug.Assert(value != 0, "Cannot set origin to 0.");

                origin = value;
                Invalidate(Invalidation.Geometry);
            }
        }

        public float Depth;

        //todo: remove recursive lookup of clock
        //we can use the private time value below once we isolate cases of it being used before it is updated (TransformHelpers).
        protected internal virtual IFrameBasedClock Clock => Parent?.Clock;

        private double time;

        protected double Time => Clock?.CurrentTime ?? time;

        const float visibility_cutoff = 0.0001f;

        private Cached<bool> isVisibleBacking = new Cached<bool>();
        public virtual bool IsVisible => isVisibleBacking.EnsureValid() ? isVisibleBacking.Value : isVisibleBacking.Refresh(() => Alpha > visibility_cutoff && Parent?.IsVisible == true);
        public bool IsMaskedAway = false;

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

        protected virtual DrawInfo DrawInfo => drawInfoBacking.EnsureValid() ? drawInfoBacking.Value : drawInfoBacking.Refresh(delegate
            {
                DrawInfo di = new DrawInfo(null);

                float alpha = Alpha;
                if (Colour.A > 0 && Colour.A < 1)
                    alpha *= Colour.A;

                Color4 colour = new Color4(Colour.R, Colour.G, Colour.B, alpha);

                if (Parent == null)
                    di.ApplyTransform(ref di, GetAnchoredPosition(DrawPosition), Scale, Rotation, Shear, OriginPosition, colour, new BlendingInfo(Additive ?? false));
                else
                    Parent.DrawInfo.ApplyTransform(ref di, GetAnchoredPosition(DrawPosition) + Parent.ChildOffset, Scale * Parent.ChildScale, Rotation, Shear, OriginPosition, colour,
                              !Additive.HasValue ? (BlendingInfo?)null : new BlendingInfo(Additive.Value));

                return di;
            });

        protected RectangleF DrawRectangle
        {
            get
            {
                Vector2 s = DrawSize;
                return new RectangleF(0, 0, s.X, s.Y);
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

        protected virtual RectangleF BoundingBox => ToParentSpace(DrawRectangle).AABBf;

        private Cached<Vector2> boundingSizeBacking = new Cached<Vector2>();

        internal Vector2 BoundingSize => boundingSizeBacking.EnsureValid()
            ? boundingSizeBacking.Value
            : boundingSizeBacking.Refresh(() =>
            {
                //field will be none when the drawable isn't requesting auto-sizing
                RectangleF bbox = BoundingBox;

                Vector2 bounds = new Vector2(0, 0);

                // Without this, 0x0 objects (like FontText with no string) produce weird results.
                // When all vertices of the quad are at the same location, then the object is effectively invisible.
                // Thus we don't need its actual bounding box, but can just assume a size of 0.
                if (bbox.Width <= 0 && bbox.Height <= 0)
                    return bounds;

                Vector2 a = GetAnchoredPosition(Vector2.Zero);

                foreach (Vector2 p in new[] { new Vector2(bbox.Left, bbox.Top), new Vector2(bbox.Right, bbox.Bottom) })
                {
                    // Compute the clipped offset depending on anchoring.
                    Vector2 offset;

                    // Right
                    if ((Anchor & Anchor.x2) > 0)
                        offset.X = a.X - p.X;
                    // Left
                    else if ((Anchor & Anchor.x0) > 0)
                        offset.X = p.X - a.X;
                    // Centre
                    else
                        offset.X = Math.Abs(p.X - a.X);

                    // Bottom
                    if ((Anchor & Anchor.y2) > 0)
                        offset.Y = a.Y - p.Y;
                    // Top
                    else if ((Anchor & Anchor.y0) > 0)
                        offset.Y = p.Y - a.Y;
                    // Centre
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

        private DrawNode[] drawNodes = new DrawNode[3];

        /// <summary>
        /// Generates the DrawNode for ourselves.
        /// </summary>
        /// <returns>A complete and updated DrawNode, or null if the DrawNode would be invisible.</returns>
        protected internal virtual DrawNode GenerateDrawNodeSubtree(int treeIndex, RectangleF bounds)
        {
            DrawNode node = drawNodes[treeIndex];
            if (node == null)
            {
                drawNodes[treeIndex] = node = CreateDrawNode();
                FrameStatistics.Increment(StatisticsCounterType.DrawNodeCtor);
            }

            if (invalidationID != node.InvalidationID)
            {
                ApplyDrawNode(node);
                FrameStatistics.Increment(StatisticsCounterType.DrawNodeAppl);
            }

            return node;
        }

        protected virtual void ApplyDrawNode(DrawNode node)
        {
            node.DrawInfo = DrawInfo;
            node.InvalidationID = invalidationID;
        }

        protected virtual DrawNode CreateDrawNode() => new DrawNode();

        /// <summary>
        /// Updates this drawable, once every frame.
        /// </summary>
        /// <returns>False if the drawable should not be updated.</returns>
        protected internal virtual bool UpdateSubTree()
        {
            if (LoadState < LoadState.Alive)
                if (!loadComplete()) return false;

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
            if (scheduler != null)
            {
                int amountScheduledTasks = scheduler.Update();
                FrameStatistics.Increment(StatisticsCounterType.ScheduleInvk, amountScheduledTasks);
            }
        }

        /// <summary>
        /// Accepts a vector in local coordinates and converts it to coordinates in Parent's space.
        /// </summary>
        /// <param name="input">A vector in local coordinates.</param>
        /// <returns>The vector in Parent's coordinates.</returns>
        protected Vector2 ToParentSpace(Vector2 input)
        {
            return (input * DrawInfo.Matrix) * Parent.DrawInfo.MatrixInverse;
        }

        /// <summary>
        /// Accepts a rectangle in local coordinates and converts it to a quad in Parent's space.
        /// </summary>
        /// <param name="input">A rectangle in local coordinates.</param>
        /// <returns>The quad in Parent's coordinates.</returns>
        protected Quad ToParentSpace(RectangleF input)
        {
            return Quad.FromRectangle(input) * (DrawInfo.Matrix * Parent.DrawInfo.MatrixInverse);
        }

        /// <summary>
        /// Accepts a rectangle in local coordinates and converts it to a quad in screen space.
        /// </summary>
        /// <param name="input">A rectangle in local coordinates.</param>
        /// <returns>The quad in screen coordinates.</returns>
        protected Quad ToScreenSpace(RectangleF input)
        {
            return Quad.FromRectangle(input) * DrawInfo.Matrix;
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
        }

        /// <summary>
        /// The time at which this drawable becomes valid (and is considered for drawing).
        /// </summary>
        public double LifetimeStart { get; set; } = double.MinValue;

        /// <summary>
        /// The time at which this drawable is no longer valid (and is considered for disposal).
        /// </summary>
        public double LifetimeEnd { get; set; } = double.MaxValue;

        public void UpdateTime(double time)
        {
            this.time = time;
        }

        /// <summary>
        /// Whether this drawable is alive.
        /// </summary>
        public bool IsAlive
        {
            get
            {
                //we have been loaded but our parent has since been nullified
                if (Parent == null && IsLoaded) return false;

                if (LifetimeStart == double.MinValue && LifetimeEnd == double.MaxValue)
                    return true;

                return Time >= LifetimeStart && Time < LifetimeEnd;
            }
        }

        /// <summary>
        /// Whether to remove the drawable from its parent's children when it's not alive.
        /// </summary>
        public virtual bool RemoveWhenNotAlive => Parent == null || Time > LifetimeStart;

        /// <summary>
        /// Override to add delayed load abilities (ie. using IsAlive)
        /// </summary>
        public virtual bool IsLoaded => LoadState >= LoadState.Loaded;

        protected volatile LoadState LoadState;

        public Task Preload(BaseGame game, Action<Drawable> onLoaded = null)
        {
            if (LoadState == LoadState.NotLoaded)
                return Task.Run(() => PerformLoad(game)).ContinueWith(obj => game.Schedule(() => onLoaded?.Invoke(this)));

            onLoaded?.Invoke(this);
            return null;
        }

        private static StopwatchClock perf = new StopwatchClock(true);

        protected internal virtual void PerformLoad(BaseGame game)
        {
            switch (LoadState)
            {
                case LoadState.Loaded:
                case LoadState.Alive:
                    return;
                case LoadState.Loading:
                    //loading on another thread
                    while (!IsLoaded) Thread.Sleep(1);
                    return;
                case LoadState.NotLoaded:
                    LoadState = LoadState.Loading;
                    break;
            }

            double t1 = perf.CurrentTime;
            Load(game);
            double elapsed = perf.CurrentTime - t1;
            if (elapsed > 50 && ThreadSafety.IsUpdateThread)
                Logger.Log($@"Drawable [{ToString()}] took {elapsed:0.00}ms to load and was not async!", LoggingTarget.Performance);
            LoadState = LoadState.Loaded;
        }

        /// <summary>
        /// Runs once on the update thread after loading has finished.
        /// </summary>
        private bool loadComplete()
        {
            if (LoadState < LoadState.Loaded) return false;

            mainThread = Thread.CurrentThread;

            scheduler?.SetCurrentThread(mainThread);

            LifetimeStart = Time;
            Invalidate();
            LoadState = LoadState.Alive;
            LoadComplete();
            return true;
        }

        /// <summary>
        /// Load resources etc.
        /// </summary>
        protected virtual void Load(BaseGame game) { }

        /// <summary>
        /// Play initial animation etc.
        /// </summary>
        protected virtual void LoadComplete() { }

        private void updateTransformsOfType(Type specificType)
        {
            //For simplicity let's just update *all* transforms.
            //The commented (more optimised code) below doesn't consider past "removed" transforms, which can cause discrepancies.
            updateTransforms();

            //foreach (ITransform t in Transforms.AliveItems)
            //    if (t.GetType() == specificType)
            //        t.Apply(this);
        }

        /// <summary>
        /// Process updates to this drawable based on loaded transforms.
        /// </summary>
        /// <returns>Whether we should draw this drawable.</returns>
        private void updateTransforms()
        {
            if (transforms == null || transforms.Count == 0) return;

            transforms.Update(Time);

            foreach (ITransform t in transforms.AliveItems)
                t.Apply(this);
        }

        private void transforms_OnRemoved(ITransform t)
        {
            t.Apply(this); //make sure we apply one last time.
        }


        private static AtomicCounter invalidationCounter = new AtomicCounter();
        private long invalidationID;

        /// <summary>
        /// Invalidates draw matrix and autosize caches.
        /// </summary>
        /// <returns>If the invalidate was actually necessary.</returns>
        public virtual bool Invalidate(Invalidation invalidation = Invalidation.All, Drawable source = null, bool shallPropagate = true)
        {
            if (invalidation == Invalidation.None)
                return false;

            OnInvalidate?.Invoke();

            if (shallPropagate && Parent != null && source != Parent)
                Parent.InvalidateFromChild(invalidation, this);

            bool alreadyInvalidated = true;

            // Either ScreenSize OR ScreenPosition OR Colour
            if ((invalidation & (Invalidation.Geometry | Invalidation.Colour)) > 0)
            {
                if ((invalidation & Invalidation.SizeInParentSpace) > 0)
                    alreadyInvalidated &= !boundingSizeBacking.Invalidate();

                alreadyInvalidated &= !screenSpaceDrawQuadBacking.Invalidate();
                alreadyInvalidated &= !drawInfoBacking.Invalidate();
            }

            if ((invalidation & Invalidation.Visibility) > 0)
                alreadyInvalidated &= !isVisibleBacking.Invalidate();

            if (!alreadyInvalidated || (invalidation & Invalidation.DrawNode) > 0)
                invalidationID = invalidationCounter.Increment();

            return !alreadyInvalidated;
        }

        internal virtual Vector2 GetAnchoredPosition(Vector2 pos)
        {
            if (Anchor == Anchor.TopLeft)
                return pos;

            Vector2 parentSize = Parent?.ChildSize ?? Vector2.Zero;

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
            scheduler?.Dispose();
            scheduler = null;

            OnUpdate = null;
            OnInvalidate = null;
        }

        public override string ToString()
        {
            string shortClass = base.ToString();
            shortClass = shortClass.Substring(shortClass.LastIndexOf('.') + 1);

            if (!string.IsNullOrEmpty(Name))
                shortClass = $@"{Name} ({shortClass})";

            return $@"{shortClass} ({DrawPosition.X:#,0},{DrawPosition.Y:#,0}) @ {DrawSize.X:#,0}x{DrawSize.Y:#,0}";
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
        DrawNode = 1 << 4,

        // Meta
        None = 0,
        Geometry = Position | SizeInParentSpace,
        All = DrawNode | Geometry | Visibility | Colour,
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
        y0 = 1 << 0,

        /// <summary>
        /// The vertical counterpart is at "Centre" position.
        /// </summary>
        y1 = 1 << 1,

        /// <summary>
        /// The vertical counterpart is at "Bottom" position.
        /// </summary>
        y2 = 1 << 2,

        /// <summary>
        /// The horizontal counterpart is at "Left" position.
        /// </summary>
        x0 = 1 << 3,

        /// <summary>
        /// The horizontal counterpart is at "Centre" position.
        /// </summary>
        x1 = 1 << 4,

        /// <summary>
        /// The horizontal counterpart is at "Right" position.
        /// </summary>
        x2 = 1 << 5,

        /// <summary>
        /// The user is manually updating the outcome, so we shouldn't.
        /// </summary>
        Custom = 1 << 6,
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
            int i = x.Depth.CompareTo(y.Depth);
            if (i != 0) return i;
            return x.CreationID.CompareTo(y.CreationID);
        }
    }

    public interface ILoadable<T>
    {
        void Load(T reference);
    }

    public interface ILoadableAsync<T>
    {
        Task LoadAsync(T reference);
    }

    public enum LoadState
    {
        NotLoaded,
        Loading,
        Loaded,
        Alive
    }
}
