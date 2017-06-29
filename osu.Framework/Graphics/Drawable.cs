// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using osu.Framework.Caching;
using osu.Framework.DebugUtils;
using osu.Framework.Extensions;
using osu.Framework.Extensions.TypeExtensions;
using osu.Framework.Graphics.Colour;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Transforms;
using osu.Framework.Input;
using osu.Framework.Logging;
using osu.Framework.Statistics;
using osu.Framework.Threading;
using osu.Framework.Timing;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Input;
using osu.Framework.Allocation;
using osu.Framework.Graphics.Effects;

namespace osu.Framework.Graphics
{
    /// <summary>
    /// Drawables are the basic building blocks of a scene graph in this framework.
    /// Anything that is visible or that the user interacts with has to be a Drawable.
    ///
    /// For example:
    ///  - Boxes
    ///  - Sprites
    ///  - Collections of Drawables
    ///
    /// Drawables are always rectangular in shape in their local coordinate system,
    /// which makes them quad-shaped in arbitrary (linearly transformed) coordinate systems.
    /// </summary>
    public abstract class Drawable : Transformable<Drawable>, IDisposable, IDrawable
    {
        #region Construction and disposal

        protected Drawable()
        {
            creationID = creation_counter.Increment();
        }

        ~Drawable()
        {
            dispose(false);
        }

        /// <summary>
        /// Disposes this drawable.
        /// </summary>
        public void Dispose()
        {
            dispose(true);
            GC.SuppressFinalize(this);
        }

        private bool isDisposed;

        /// <summary>
        /// Disposes this drawable.
        /// </summary>
        protected virtual void Dispose(bool isDisposing)
        {
        }

        private void dispose(bool isDisposing)
        {
            if (isDisposed)
                return;

            //we can't dispose if we are mid-load, else our children may get in a bad state.
            loadTask?.Wait();

            Dispose(isDisposing);

            Parent = null;

            scheduler?.Dispose();
            scheduler = null;

            OnUpdate = null;
            OnInvalidate = null;

            // If this Drawable is disposed, then we need to also
            // stop remotely rendering it.
            proxy?.Dispose();

            isDisposed = true;
        }

        /// <summary>
        /// Whether this Drawable should be disposed when it is automatically removed from
        /// its <see cref="Parent"/> due to <see cref="IsAlive"/> being false.
        /// </summary>
        public virtual bool DisposeOnDeathRemoval => false;

        #endregion

        #region Loading

        /// <summary>
        /// Whether this Drawable is fully loaded.
        /// Override to false for delaying the load further (e.g. using <see cref="IsAlive"/>).
        /// </summary>
        public virtual bool IsLoaded => loadState >= LoadState.Loaded;

        private volatile LoadState loadState;

        /// <summary>
        /// Describes the current state of this Drawable within the loading pipeline.
        /// </summary>
        public LoadState LoadState => loadState;

        private Task loadTask;
        private readonly object loadLock = new object();

        /// <summary>
        /// Loads this Drawable asynchronously.
        /// </summary>
        /// <param name="game">The game to load this Drawable on.</param>
        /// <param name="target">
        /// The target this Drawable may eventually be loaded into.
        /// <see cref="Clock"/> and <see cref="Dependencies"/> are inherited from the target.
        /// </param>
        /// <param name="onLoaded">Callback to be invoked asynchronously after loading is complete.</param>
        /// <returns>The task which is used for loading and callbacks.</returns>
        internal Task LoadAsync<T>(Game game, Drawable target, Action<T> onLoaded = null)
            where T : Drawable
        {
            if (loadState != LoadState.NotLoaded)
                throw new InvalidOperationException($@"{nameof(LoadAsync)} may not be called more than once on the same Drawable.");

            if (!(this is T))
                throw new InvalidOperationException($"The {nameof(T)} parameter of the {nameof(LoadAsync)} must be a subtype of {GetType().ReadableName()}.");

            loadState = LoadState.Loading;

            return loadTask = Task.Run(() => Load(target.Clock, target.Dependencies)).ContinueWith(task => game.Schedule(() =>
            {
                task.ThrowIfFaulted();
                onLoaded?.Invoke((T)this);
                loadTask = null;
            }));
        }

        private static readonly StopwatchClock perf = new StopwatchClock(true);

        /// <summary>
        /// Create a local dependency container which will be used by ourselves and all our nested children.
        /// If not overridden, the load-time parent's dependency tree will be used.
        /// </summary>
        /// <param name="parent">The parent <see cref="DependencyContainer"/> which should be passed through if we want fallback lookups to work.</param>
        /// <returns>A new dependency container to be stored for this Drawable.</returns>
        protected virtual DependencyContainer CreateLocalDependencies(DependencyContainer parent) => parent;

        /// <summary>
        /// Contains all dependencies that can be injected into this Drawable using <see cref="BackgroundDependencyLoader"/>.
        /// Add or override dependencies by calling <see cref="DependencyContainer.Cache{T}(T, bool, bool)"/>.
        /// </summary>
        protected DependencyContainer Dependencies { get; private set; }

        /// <summary>
        /// Loads this drawable, including the gathering of dependencies and initialisation of required resources.
        /// </summary>
        /// <param name="clock">The clock we should use by default.</param>
        /// <param name="dependencies">The dependency tree we will inherit by default. May be extended via <see cref="CreateLocalDependencies(DependencyContainer)"/></param>
        internal void Load(IFrameBasedClock clock, DependencyContainer dependencies)
        {
            // Blocks when loading from another thread already.
            lock (loadLock)
            {
                switch (loadState)
                {
                    case LoadState.Loaded:
                    case LoadState.Alive:
                        return;
                    case LoadState.Loading:
                        break;
                    case LoadState.NotLoaded:
                        loadState = LoadState.Loading;
                        break;
                    default:
                        Trace.Assert(false, "Impossible loading state.");
                        break;
                }

                UpdateClock(clock);

                double t1 = perf.CurrentTime;

                // get our dependencies from our parent, but allow local overriding of our inherited dependency container
                Dependencies = CreateLocalDependencies(dependencies);

                Dependencies.Initialize(this);

                double elapsed = perf.CurrentTime - t1;
                if (perf.CurrentTime > 1000 && elapsed > 50 && ThreadSafety.IsUpdateThread)
                    Logger.Log($@"Drawable [{ToString()}] took {elapsed:0.00}ms to load and was not async!", LoggingTarget.Performance);
                loadState = LoadState.Loaded;
            }
        }

        /// <summary>
        /// Runs once on the update thread after loading has finished.
        /// </summary>
        private bool loadComplete()
        {
            if (loadState < LoadState.Loaded) return false;

            MainThread = Thread.CurrentThread;
            scheduler?.SetCurrentThread(MainThread);

            Invalidate();
            loadState = LoadState.Alive;
            LoadComplete();
            OnLoadComplete?.Invoke(this);
            return true;
        }

        /// <summary>
        /// Play initial animation etc.
        /// </summary>
        protected virtual void LoadComplete()
        {
        }

        #endregion

        #region Sorting (CreationID / Depth)

        /// <summary>
        /// Captures the order in which Drawables were created. Each Drawable
        /// is assigned a unique, monotonically increasing ID upon creation in a thread-safe manner.
        /// The primary use case of this ID is stable sorting of Drawables with equal
        /// <see cref="Depth"/>.
        /// </summary>
        private long creationID { get; }

        private static readonly AtomicCounter creation_counter = new AtomicCounter();

        private float depth;

        /// <summary>
        /// Controls which Drawables are behind or in front of other Drawables.
        /// This amounts to sorting Drawables by their <see cref="Depth"/>.
        /// A Drawable with higher <see cref="Depth"/> than another Drawable is
        /// drawn behind the other Drawable.
        /// </summary>
        public float Depth
        {
            get { return depth; }
            set
            {
                // TODO: Consider automatically resorting the parents children instead of simply forbidding this.
                if (AddedToParentContainer)
                    throw new InvalidOperationException("May not change depth while inside a parent container.");
                depth = value;
            }
        }

        public class CreationOrderDepthComparer : IComparer<Drawable>
        {
            public virtual int Compare(Drawable x, Drawable y)
            {
                if (x == null) throw new ArgumentNullException(nameof(x));
                if (y == null) throw new ArgumentNullException(nameof(y));

                int i = y.Depth.CompareTo(x.Depth);
                if (i != 0) return i;
                return x.creationID.CompareTo(y.creationID);
            }
        }

        public class ReverseCreationOrderDepthComparer : IComparer<Drawable>
        {
            public virtual int Compare(Drawable x, Drawable y)
            {
                if (x == null) throw new ArgumentNullException(nameof(x));
                if (y == null) throw new ArgumentNullException(nameof(y));

                int i = y.Depth.CompareTo(x.Depth);
                if (i != 0) return i;
                return y.creationID.CompareTo(x.creationID);
            }
        }

        protected virtual IComparer<Drawable> DepthComparer => new CreationOrderDepthComparer();

        #endregion

        #region Periodic tasks (events, Scheduler, Transforms, Update)

        /// <summary>
        /// This event is fired after the <see cref="Update"/> method is called at the end of
        /// <see cref="UpdateSubTree"/>. It should be used when a simple action should be performed
        /// at the end of every update call which does not warrant overriding the Drawable.
        /// </summary>
        public Action<Drawable> OnUpdate;

        /// <summary>
        /// This event is fired after the <see cref="LoadComplete"/> method is called.
        /// It should be used when a simple action should be performed
        /// when the Drawable is loaded which does not warrant overriding the Drawable.
        /// </summary>
        public Action<Drawable> OnLoadComplete;

        /// <summary>
        /// THIS EVENT PURELY EXISTS FOR THE SCENE GRAPH VISUALIZER. DO NOT USE.
        /// This event is fired after the <see cref="Invalidate(Invalidation, Drawable, bool)"/> method is called.
        /// </summary>
        internal event Action<Drawable> OnInvalidate;

        private Scheduler scheduler;
        internal Thread MainThread { get; private set; }

        /// <summary>
        /// A lazily-initialized scheduler used to schedule tasks to be invoked in future <see cref="Update"/>s calls.
        /// The tasks are invoked at the beginning of the <see cref="Update"/> method before anything else.
        /// </summary>
        protected Scheduler Scheduler => scheduler ?? (scheduler = new Scheduler(MainThread));

        /// <summary>
        /// Updates this Drawable and all Drawables further down the scene graph.
        /// Called once every frame.
        /// </summary>
        /// <returns>False if the drawable should not be updated.</returns>
        public virtual bool UpdateSubTree()
        {
            if (isDisposed)
                throw new ObjectDisposedException(ToString(), "Disposed Drawables may never be in the scene graph.");

            if (Parent != null) //we don't want to update our clock if we are at the top of the stack. it's handled elsewhere for us.
                customClock?.ProcessFrame();

            if (loadState < LoadState.Alive)
                if (!loadComplete()) return false;

            //todo: this should be moved to after the IsVisible condition once we have TOL for transforms (and some better logic).
            UpdateTransforms();

            if (!IsPresent)
                return true;

            if (scheduler != null)
            {
                int amountScheduledTasks = scheduler.Update();
                FrameStatistics.Increment(StatisticsCounterType.ScheduleInvk, amountScheduledTasks);
            }

            Update();
            OnUpdate?.Invoke(this);
            return true;
        }

        /// <summary>
        /// Performs a once-per-frame update specific to this Drawable. A more elegant alternative to
        /// <see cref="OnUpdate"/> when deriving from <see cref="Drawable"/>. Note, that this
        /// method is always called before Drawables further down the scene graph are updated.
        /// </summary>
        protected virtual void Update()
        {
        }

        #endregion

        #region Position / Size (with margin)

        private Vector2 position
        {
            get { return new Vector2(x, y); }
            set
            {
                x = value.X;
                y = value.Y;
            }
        }

        /// <summary>
        /// Positional offset of <see cref="Origin"/> to <see cref="RelativeAnchorPosition"/> in the
        /// <see cref="Parent"/>'s coordinate system. May be in absolute or relative units
        /// (controlled by <see cref="RelativePositionAxes"/>).
        /// </summary>
        public Vector2 Position
        {
            get { return position; }

            set
            {
                if (position == value) return;
                position = value;

                Invalidate(Invalidation.MiscGeometry);
            }
        }

        private float x;
        private float y;

        /// <summary>
        /// X component of <see cref="Position"/>.
        /// </summary>
        public float X
        {
            get { return x; }
            set
            {
                if (x == value) return;
                x = value;

                Invalidate(Invalidation.MiscGeometry);
            }
        }

        /// <summary>
        /// Y component of <see cref="Position"/>.
        /// </summary>
        public float Y
        {
            get { return y; }
            set
            {
                if (y == value) return;
                y = value;

                Invalidate(Invalidation.MiscGeometry);
            }
        }

        private Axes relativePositionAxes;

        /// <summary>
        /// Controls which <see cref="Axes"/> of <see cref="Position"/> are relative w.r.t.
        /// <see cref="Parent"/>'s size (from 0 to 1) rather than absolute.
        /// The <see cref="Axes"/> set in this property are ignored by automatically sizing
        /// parents.
        /// </summary>
        /// <remarks>
        /// When setting this property, the <see cref="Position"/> is converted such that
        /// <see cref="DrawPosition"/> remains invariant.
        /// </remarks>
        public Axes RelativePositionAxes
        {
            get { return relativePositionAxes; }
            set
            {
                if (value == relativePositionAxes)
                    return;

                // Convert coordinates from relative to absolute or vice versa
                Vector2 conversion = relativeToAbsoluteFactor;
                if ((value & Axes.X) > (relativePositionAxes & Axes.X))
                    X = conversion.X == 0 ? 0 : X / conversion.X;
                else if ((relativePositionAxes & Axes.X) > (value & Axes.X))
                    X *= conversion.X;

                if ((value & Axes.Y) > (relativePositionAxes & Axes.Y))
                    Y = conversion.Y == 0 ? 0 : Y / conversion.Y;
                else if ((relativePositionAxes & Axes.X) > (value & Axes.X))
                    Y *= conversion.Y;

                relativePositionAxes = value;

                // No invalidation necessary as DrawPosition remains invariant.
            }
        }

        /// <summary>
        /// Absolute positional offset of <see cref="Origin"/> to <see cref="RelativeAnchorPosition"/>
        /// in the <see cref="Parent"/>'s coordinate system.
        /// </summary>
        public Vector2 DrawPosition
        {
            get
            {
                Vector2 offset = Vector2.Zero;
                if (Parent != null && RelativePositionAxes != Axes.None)
                {
                    offset = Parent.RelativeChildOffset;

                    if ((RelativePositionAxes & Axes.X) == 0)
                        offset.X = 0;

                    if ((RelativePositionAxes & Axes.Y) == 0)
                        offset.Y = 0;
                }

                return applyRelativeAxes(RelativePositionAxes, Position - offset);
            }
        }

        private Vector2 size
        {
            get { return new Vector2(width, height); }
            set
            {
                width = value.X;
                height = value.Y;
            }
        }

        /// <summary>
        /// Size of this Drawable in the <see cref="Parent"/>'s coordinate system.
        /// May be in absolute or relative units (controlled by <see cref="RelativeSizeAxes"/>).
        /// </summary>
        public virtual Vector2 Size
        {
            get { return size; }

            set
            {
                if (size == value) return;
                size = value;

                Invalidate(Invalidation.DrawSize);
            }
        }

        private float width;
        private float height;

        /// <summary>
        /// X component of <see cref="Size"/>.
        /// </summary>
        public virtual float Width
        {
            get { return width; }
            set
            {
                if (width == value) return;
                width = value;

                Invalidate(Invalidation.DrawSize);
            }
        }

        /// <summary>
        /// Y component of <see cref="Size"/>.
        /// </summary>
        public virtual float Height
        {
            get { return height; }
            set
            {
                if (height == value) return;
                height = value;

                Invalidate(Invalidation.DrawSize);
            }
        }

        private Axes relativeSizeAxes;

        /// <summary>
        /// Controls which <see cref="Axes"/> are relative sizes w.r.t. <see cref="Parent"/>'s size
        /// (from 0 to 1) in the <see cref="Parent"/>'s coordinate system, rather than absolute sizes.
        /// The <see cref="Axes"/> set in this property are ignored by automatically sizing
        /// parents.
        /// </summary>
        /// <remarks>
        /// If an axis becomes relatively sized and its component of <see cref="Size"/> was previously 0,
        /// then it automatically becomes 1. In all other cases <see cref="Size"/> is converted such that
        /// <see cref="DrawSize"/> remains invariant across changes of this property.
        /// </remarks>
        public virtual Axes RelativeSizeAxes
        {
            get { return relativeSizeAxes; }
            set
            {
                if (value == relativeSizeAxes)
                    return;

                // Convert coordinates from relative to absolute or vice versa
                Vector2 conversion = relativeToAbsoluteFactor;
                if ((value & Axes.X) > (relativeSizeAxes & Axes.X))
                    Width = conversion.X == 0 ? 0 : Width / conversion.X;
                else if ((relativeSizeAxes & Axes.X) > (value & Axes.X))
                    Width *= conversion.X;

                if ((value & Axes.Y) > (relativeSizeAxes & Axes.Y))
                    Height = conversion.Y == 0 ? 0 : Height / conversion.Y;
                else if ((relativeSizeAxes & Axes.Y) > (value & Axes.Y))
                    Height *= conversion.Y;

                relativeSizeAxes = value;

                if ((relativeSizeAxes & Axes.X) > 0 && Width == 0) Width = 1;
                if ((relativeSizeAxes & Axes.Y) > 0 && Height == 0) Height = 1;

                OnSizingChanged();
                // No invalidation necessary as DrawSize remains invariant.
            }
        }

        private Cached<Vector2> drawSizeBacking = new Cached<Vector2>();

        /// <summary>
        /// Absolute size of this Drawable in the <see cref="Parent"/>'s coordinate system.
        /// </summary>
        public Vector2 DrawSize => drawSizeBacking.EnsureValid()
            ? drawSizeBacking.Value
            : drawSizeBacking.Refresh(() => applyRelativeAxes(RelativeSizeAxes, Size));

        /// <summary>
        /// X component of <see cref="DrawSize"/>.
        /// </summary>
        public float DrawWidth => DrawSize.X;

        /// <summary>
        /// Y component of <see cref="DrawSize"/>.
        /// </summary>
        public float DrawHeight => DrawSize.Y;

        private MarginPadding margin;

        /// <summary>
        /// Size of an empty region around this Drawable used to manipulate
        /// layout. Does not affect <see cref="DrawSize"/> or the region of accepted input,
        /// but does affect <see cref="LayoutSize"/>.
        /// </summary>
        public MarginPadding Margin
        {
            get { return margin; }
            set
            {
                if (margin.Equals(value)) return;

                margin = value;
                margin.ThrowIfNegative();

                Invalidate(Invalidation.MiscGeometry);
            }
        }

        /// <summary>
        /// Absolute size of this Drawable's layout rectangle in the <see cref="Parent"/>'s
        /// coordinate system; i.e. <see cref="DrawSize"/> with the addition of <see cref="Margin"/>.
        /// </summary>
        public Vector2 LayoutSize => DrawSize + new Vector2(margin.TotalHorizontal, margin.TotalVertical);

        /// <summary>
        /// Absolutely sized rectangle for drawing in the <see cref="Parent"/>'s coordinate system.
        /// Based on <see cref="DrawSize"/>.
        /// </summary>
        public RectangleF DrawRectangle
        {
            get
            {
                Vector2 s = DrawSize;
                return new RectangleF(0, 0, s.X, s.Y);
            }
        }

        /// <summary>
        /// Absolutely sized rectangle for layout in the <see cref="Parent"/>'s coordinate system.
        /// Based on <see cref="LayoutSize"/> and <see cref="margin"/>.
        /// </summary>
        public RectangleF LayoutRectangle
        {
            get
            {
                Vector2 s = LayoutSize;
                return new RectangleF(-margin.Left, -margin.Top, s.X, s.Y);
            }
        }

        /// <summary>
        /// Helper function for converting potentially relative coordinates in the
        /// <see cref="Parent"/>'s space to absolute coordinates based on which
        /// axes are relative. <see cref="Axes"/>
        /// </summary>
        /// <param name="relativeAxes">Describes which axes are relative.</param>
        /// <param name="v">The coordinates to convert.</param>
        /// <returns>Absolute coordinates in <see cref="Parent"/>'s space.</returns>
        private Vector2 applyRelativeAxes(Axes relativeAxes, Vector2 v)
        {
            if (relativeAxes != Axes.None)
            {
                Vector2 conversion = relativeToAbsoluteFactor;
                if ((relativeAxes & Axes.X) > 0)
                    v.X *= conversion.X;
                if ((relativeAxes & Axes.Y) > 0)
                    v.Y *= conversion.Y;
            }
            return v;
        }

        /// <summary>
        /// Conversion factor from relative to absolute coordinates in the <see cref="Parent"/>'s space.
        /// </summary>
        private Vector2 relativeToAbsoluteFactor => Parent?.RelativeToAbsoluteFactor ?? Vector2.One;

        private Axes bypassAutoSizeAxes;

        /// <summary>
        /// Controls which <see cref="Axes"/> are ignored by parent <see cref="Parent"/> automatic sizing.
        /// Most notably, <see cref="RelativePositionAxes"/> and <see cref="RelativeSizeAxes"/> do not affect
        /// automatic sizing to avoid circular size dependencies.
        /// </summary>
        public Axes BypassAutoSizeAxes
        {
            get { return bypassAutoSizeAxes | relativeSizeAxes | relativePositionAxes; }

            set
            {
                if (value == bypassAutoSizeAxes)
                    return;

                bypassAutoSizeAxes = value;
                Parent?.InvalidateFromChild(Invalidation.RequiredParentSizeToFit);
            }
        }

        /// <summary>
        /// Computes the bounding box of this drawable in its parent's space.
        /// </summary>
        public virtual RectangleF BoundingBox => ToParentSpace(LayoutRectangle).AABBFloat;

        /// <summary>
        /// Called whenever the <see cref="RelativeSizeAxes"/> of this drawable is changed, or when the <see cref="Container{T}.AutoSizeAxes"/> are changed if this drawable is a <see cref="Container{T}"/>.
        /// </summary>
        protected virtual void OnSizingChanged() { }

        #endregion

        #region Scale / Shear / Rotation

        private Vector2 scale = Vector2.One;

        /// <summary>
        /// Base relative scaling factor around <see cref="OriginPosition"/>.
        /// </summary>
        public Vector2 Scale
        {
            get { return scale; }

            set
            {
                if (scale == value) return;
                scale = value;

                Invalidate(Invalidation.MiscGeometry);
            }
        }

        /// <summary>
        /// The method to use to fill this drawable's parent space.
        /// </summary>
        public FillMode FillMode { get; set; }

        /// <summary>
        /// Relative scaling factor around <see cref="OriginPosition"/>.
        /// </summary>
        protected virtual Vector2 DrawScale
        {
            get
            {
                if (FillMode == FillMode.None)
                    return Scale;

                Vector2 modifier = Vector2.One;
                Vector2 relativeToAbsolute = relativeToAbsoluteFactor;

                switch (FillMode)
                {
                    case FillMode.Fill:
                        modifier = new Vector2(Math.Max(relativeToAbsolute.X / DrawWidth, relativeToAbsolute.Y / DrawHeight));
                        break;
                    case FillMode.Fit:
                        modifier = new Vector2(Math.Min(relativeToAbsolute.X / DrawWidth, relativeToAbsolute.Y / DrawHeight));
                        break;
                    case FillMode.Stretch:
                        modifier = new Vector2(relativeToAbsolute.X / DrawWidth, relativeToAbsolute.Y / DrawHeight);
                        break;
                }

                return Scale * modifier;
            }
        }

        private Vector2 shear = Vector2.Zero;

        /// <summary>
        /// Relative shearing factor. The X dimension is relative w.r.t. <see cref="Height"/>
        /// and the Y dimension relative w.r.t. <see cref="Width"/>.
        /// </summary>
        public Vector2 Shear
        {
            get { return shear; }

            set
            {
                if (shear == value) return;
                shear = value;

                Invalidate(Invalidation.MiscGeometry);
            }
        }

        private float rotation;

        /// <summary>
        /// Rotation in degrees around <see cref="OriginPosition"/>.
        /// </summary>
        public float Rotation
        {
            get { return rotation; }

            set
            {
                if (value == rotation) return;
                rotation = value;

                Invalidate(Invalidation.MiscGeometry);
            }
        }

        #endregion

        #region Origin / Anchor

        private Anchor origin = Anchor.TopLeft;

        /// <summary>
        /// The origin of the local coordinate system of this Drawable.
        /// Can either be one of 9 relative positions (0, 0.5, and 1 in x and y)
        /// or a fixed absolute position via <see cref="OriginPosition"/>.
        /// </summary>
        public virtual Anchor Origin
        {
            get { return origin; }

            set
            {
                if (origin == value) return;

                if (value == 0)
                    throw new ArgumentException("Cannot set origin to 0.", nameof(value));

                origin = value;
                Invalidate(Invalidation.MiscGeometry);
            }
        }


        private Vector2 customOrigin;

        /// <summary>
        /// The origin of the local coordinate system of this Drawable
        /// in relative coordinates expressed in the coordinate system with origin at the
        /// top left corner of the <see cref="DrawRectangle"/> (not <see cref="LayoutRectangle"/>).
        /// </summary>
        public Vector2 RelativeOriginPosition
        {
            get
            {
                if (Origin == Anchor.Custom)
                    throw new InvalidOperationException(@"Can not obtain relative origin position for custom origins.");

                Vector2 result = Vector2.Zero;
                if ((origin & Anchor.x1) > 0)
                    result.X = 0.5f;
                else if ((origin & Anchor.x2) > 0)
                    result.X = 1;

                if ((origin & Anchor.y1) > 0)
                    result.Y = 0.5f;
                else if ((origin & Anchor.y2) > 0)
                    result.Y = 1;

                return result;
            }
        }

        /// <summary>
        /// The origin of the local coordinate system of this Drawable
        /// in absolute coordinates expressed in the coordinate system with origin at the
        /// top left corner of the <see cref="DrawRectangle"/> (not <see cref="LayoutRectangle"/>).
        /// </summary>
        public virtual Vector2 OriginPosition
        {
            get
            {
                Vector2 result;
                if (Origin == Anchor.Custom)
                    result = customOrigin;
                else if (Origin == Anchor.TopLeft)
                    result = Vector2.Zero;
                else
                    result = computeAnchorPosition(LayoutSize, Origin);

                return result - new Vector2(margin.Left, margin.Top);
            }

            set
            {
                customOrigin = value;
                Origin = Anchor.Custom;
            }
        }


        private Anchor anchor = Anchor.TopLeft;

        /// <summary>
        /// Specifies where <see cref="Origin"/> is attached to the <see cref="Parent"/>
        /// in the coordinate system with origin at the top left corner of the
        /// <see cref="Parent"/>'s <see cref="DrawRectangle"/>.
        /// Can either be one of 9 relative positions (0, 0.5, and 1 in x and y)
        /// or a fixed absolute position via <see cref="RelativeAnchorPosition"/>.
        /// </summary>
        public Anchor Anchor
        {
            get { return anchor; }

            set
            {
                if (anchor == value) return;

                if (value == 0)
                    throw new ArgumentException("Cannot set anchor to 0.", nameof(value));

                anchor = value;
                Invalidate(Invalidation.MiscGeometry);
            }
        }


        private Vector2 customRelativeAnchorPosition;

        /// <summary>
        /// Specifies in relative coordinates where <see cref="Origin"/> is attached
        /// to the <see cref="Parent"/> in the coordinate system with origin at the top
        /// left corner of the <see cref="Parent"/>'s <see cref="DrawRectangle"/>, and
        /// a value of <see cref="Vector2.One"/> referring to the bottom right corner of
        /// the <see cref="Parent"/>'s <see cref="DrawRectangle"/>.
        /// </summary>
        public Vector2 RelativeAnchorPosition
        {
            get
            {
                if (Anchor == Anchor.Custom)
                    return customRelativeAnchorPosition;

                Vector2 result = Vector2.Zero;
                if ((anchor & Anchor.x1) > 0)
                    result.X = 0.5f;
                else if ((anchor & Anchor.x2) > 0)
                    result.X = 1;

                if ((anchor & Anchor.y1) > 0)
                    result.Y = 0.5f;
                else if ((anchor & Anchor.y2) > 0)
                    result.Y = 1;

                return result;
            }

            set
            {
                customRelativeAnchorPosition = value;
                Anchor = Anchor.Custom;
            }
        }

        /// <summary>
        /// Specifies in absolute coordinates where <see cref="Origin"/> is attached
        /// to the <see cref="Parent"/> in the coordinate system with origin at the top
        /// left corner of the <see cref="Parent"/>'s <see cref="DrawRectangle"/>.
        /// </summary>
        public Vector2 AnchorPosition => RelativeAnchorPosition * Parent?.ChildSize ?? Vector2.Zero;

        /// <summary>
        /// Helper function to compute an absolute position given an absolute size and
        /// a relative <see cref="Graphics.Anchor"/>.
        /// </summary>
        /// <param name="size">Absolute size</param>
        /// <param name="anchor">Relative <see cref="Graphics.Anchor"/></param>
        /// <returns>Absolute position</returns>
        private static Vector2 computeAnchorPosition(Vector2 size, Anchor anchor)
        {
            Vector2 result = Vector2.Zero;

            if ((anchor & Anchor.x1) > 0)
                result.X = size.X / 2f;
            else if ((anchor & Anchor.x2) > 0)
                result.X = size.X;

            if ((anchor & Anchor.y1) > 0)
                result.Y = size.Y / 2f;
            else if ((anchor & Anchor.y2) > 0)
                result.Y = size.Y;

            return result;
        }

        #endregion

        #region Colour / Alpha / Blending

        private ColourInfo colourInfo = ColourInfo.SingleColour(Color4.White);

        /// <summary>
        /// Colours of the individual corner vertices of this Drawable in sRGB space.
        /// </summary>
        public ColourInfo ColourInfo
        {
            get { return colourInfo; }

            set
            {
                if (colourInfo.Equals(value)) return;
                colourInfo = value;

                Invalidate(Invalidation.Colour);
            }
        }

        /// <summary>
        /// Colour of this Drawable in sRGB space. Only valid if no individual colours
        /// have been specified for each corner vertex via <see cref="ColourInfo"/>.
        /// </summary>
        public SRGBColour Colour
        {
            get { return colourInfo.Colour; }

            set
            {
                if (colourInfo.HasSingleColour && colourInfo.TopLeft.Equals(value)) return;

                colourInfo.Colour = value;

                Invalidate(Invalidation.Colour);
            }
        }

        private float alpha = 1.0f;

        /// <summary>
        /// Multiplicative alpha factor applied on top of <see cref="ColourInfo"/> and its existing
        /// alpha channel(s).
        /// </summary>
        public float Alpha
        {
            get { return alpha; }

            set
            {
                if (alpha == value) return;

                Invalidate(Invalidation.Colour);

                alpha = value;
            }
        }

        private const float visibility_cutoff = 0.0001f;

        /// <summary>
        /// Determines whether this Drawable is present based on its <see cref="Alpha"/> value.
        /// Can be forced always on with <see cref="AlwaysPresent"/>.
        /// </summary>
        public virtual bool IsPresent => AlwaysPresent || Alpha > visibility_cutoff;

        private bool alwaysPresent;

        /// <summary>
        /// If true, forces <see cref="IsPresent"/> to always be true. In other words,
        /// this drawable is always considered for layout, input, and drawing, regardless
        /// of alpha value.
        /// </summary>
        public bool AlwaysPresent
        {
            get { return alwaysPresent; }

            set
            {
                if (alwaysPresent == value) return;

                Invalidate(Invalidation.Colour);

                alwaysPresent = value;
            }
        }

        private BlendingMode blendingMode;

        /// <summary>
        /// Determines how this Drawable is blended with other already drawn Drawables.
        /// Inherits the <see cref="Parent"/>'s <see cref="BlendingMode"/> by default.
        /// </summary>
        public BlendingMode BlendingMode
        {
            get { return blendingMode; }

            set
            {
                if (blendingMode == value) return;

                blendingMode = value;
                Invalidate(Invalidation.Colour);
            }
        }

        #endregion

        #region Timekeeping

        private IFrameBasedClock customClock;
        private IFrameBasedClock clock;

        /// <summary>
        /// The clock of this drawable. Used for keeping track of time across
        /// frames. By default is inherited from <see cref="Parent"/>.
        /// If set, then the provided value is used as a custom clock and the
        /// <see cref="Parent"/>'s clock is ignored.
        /// </summary>
        public override IFrameBasedClock Clock
        {
            get { return clock; }
            set
            {
                customClock = value;
                UpdateClock(customClock);
            }
        }

        /// <summary>
        /// Updates the clock to be used. Has no effect if this drawable
        /// uses a custom clock.
        /// </summary>
        /// <param name="clock">The new clock to be used.</param>
        public virtual void UpdateClock(IFrameBasedClock clock)
        {
            this.clock = customClock ?? clock;
        }

        /// <summary>
        /// The time at which this drawable becomes valid (and is considered for drawing).
        /// </summary>
        public virtual double LifetimeStart { get; set; } = double.MinValue;

        /// <summary>
        /// The time at which this drawable is no longer valid (and is considered for disposal).
        /// </summary>
        public virtual double LifetimeEnd { get; set; } = double.MaxValue;

        /// <summary>
        /// Updates the current time to the provided time. For drawables this is a no-op
        /// as they obtain their time via their <see cref="Clock"/>.
        /// </summary>
        public void UpdateTime(FrameTimeInfo time)
        {
        }

        /// <summary>
        /// Whether this drawable is alive.
        /// </summary>
        public virtual bool IsAlive
        {
            get
            {
                //we have been loaded but our parent has since been nullified
                if (Parent == null && IsLoaded) return false;

                if (LifetimeStart == double.MinValue && LifetimeEnd == double.MaxValue)
                    return true;

                return Time.Current >= LifetimeStart && Time.Current < LifetimeEnd;
            }
        }

        /// <summary>
        /// Whether to remove the drawable from its parent's children when it's not alive.
        /// </summary>
        public virtual bool RemoveWhenNotAlive => Parent == null || Time.Current > LifetimeStart;

        #endregion

        #region Parenting (scene graph operations, including ProxyDrawable)

        /// <summary>
        /// Whether this drawable has been added to a parent <see cref="IContainer"/>. Note that this does NOT imply that
        /// <see cref="Parent"/> has been set.
        /// This is primarily used to block properties such as <see cref="Depth"/> that strictly rely on the value of <see cref="Parent"/>
        /// to alert the user of an invalid operation.
        /// </summary>
        internal bool AddedToParentContainer;

        private IContainer parent;

        /// <summary>
        /// The parent of this drawable in the scene graph.
        /// </summary>
        public IContainer Parent
        {
            get { return parent; }
            internal set
            {
                if (isDisposed)
                    throw new ObjectDisposedException(ToString(), "Disposed Drawables may never get a parent and return to the scene graph.");

                AddedToParentContainer = value != null;

                if (parent == value) return;

                if (value != null && parent != null)
                    throw new InvalidOperationException("May not add a drawable to multiple containers.");

                parent = value;
                Invalidate(InvalidationFromParentSize | Invalidation.Colour);

                if (parent != null)
                {
                    //we should already have a clock at this point (from our LoadRequested invocation)
                    //this just ensures we have the most recent parent clock.
                    //we may want to consider enforcing that parent.Clock == clock here.
                    UpdateClock(parent.Clock);
                }
            }
        }

        /// <summary>
        /// Refers to the original if this drawable was created via
        /// <see cref="CreateProxy"/>. Otherwise refers to this.
        /// </summary>
        internal virtual Drawable Original => this;

        /// <summary>
        /// True iff <see cref="CreateProxy"/> has been called before.
        /// </summary>
        internal bool HasProxy => proxy != null;

        private ProxyDrawable proxy;

        /// <summary>
        /// Creates a proxy drawable which can be inserted elsewhere in the scene graph.
        /// Will cause the original instance to not render itself.
        /// Creating multiple proxies is not supported and will result in an
        /// <see cref="InvalidOperationException"/>.
        /// </summary>
        public ProxyDrawable CreateProxy()
        {
            if (proxy != null)
                throw new InvalidOperationException("Multiple proxies are not supported.");
            return proxy = new ProxyDrawable(this);
        }

        #endregion

        #region Caching & invalidation (for things too expensive to compute every frame)

        /// <summary>
        /// Was this Drawable masked away completely during the last frame?
        /// This is measured conservatively, i.e. it is only true when the Drawable was
        /// actually masked away, but it may be false, even if the Drawable was masked away.
        /// </summary>
        internal bool IsMaskedAway;

        private Cached<Quad> screenSpaceDrawQuadBacking = new Cached<Quad>();

        protected virtual Quad ComputeScreenSpaceDrawQuad() => ToScreenSpace(DrawRectangle);

        /// <summary>
        /// The screen-space quad this drawable occupies.
        /// </summary>
        public virtual Quad ScreenSpaceDrawQuad => screenSpaceDrawQuadBacking.EnsureValid()
            ? screenSpaceDrawQuadBacking.Value
            : screenSpaceDrawQuadBacking.Refresh(ComputeScreenSpaceDrawQuad);


        private Cached<DrawInfo> drawInfoBacking = new Cached<DrawInfo>();

        /// <summary>
        /// Contains a linear transformation, colour information, and blending information
        /// of this drawable.
        /// </summary>
        public virtual DrawInfo DrawInfo => drawInfoBacking.EnsureValid()
            ? drawInfoBacking.Value
            : drawInfoBacking.Refresh(delegate
            {
                DrawInfo di = Parent?.DrawInfo ?? new DrawInfo(null);

                Vector2 pos = DrawPosition + AnchorPosition;
                Vector2 drawScale = DrawScale;
                BlendingMode localBlendingMode = BlendingMode;

                if (Parent != null)
                {
                    pos += Parent.ChildOffset;

                    if (localBlendingMode == BlendingMode.Inherit)
                        localBlendingMode = Parent.BlendingMode;
                }

                di.ApplyTransform(pos, drawScale, Rotation, Shear, OriginPosition);
                di.Blending = new BlendingInfo(localBlendingMode);

                // We need an additional parent null check here, since the following block
                // requires up-to-date matrices.
                if (Parent == null)
                    di.Colour = ColourInfo;
                else if (di.Colour.HasSingleColour)
                    di.Colour.ApplyChild(ColourInfo.MultiplyAlpha(alpha));
                else
                {
                    // Cannot use ToParentSpace here, because ToParentSpace depends on DrawInfo to be completed
                    Quad interp = Quad.FromRectangle(DrawRectangle) * (di.Matrix * Parent.DrawInfo.MatrixInverse);
                    Vector2 parentSize = Parent.DrawSize;

                    interp.TopLeft = Vector2.Divide(interp.TopLeft, parentSize);
                    interp.TopRight = Vector2.Divide(interp.TopRight, parentSize);
                    interp.BottomLeft = Vector2.Divide(interp.BottomLeft, parentSize);
                    interp.BottomRight = Vector2.Divide(interp.BottomRight, parentSize);

                    di.Colour.ApplyChild(ColourInfo.MultiplyAlpha(alpha), interp);
                }

                return di;
            });

        private Cached<Vector2> requiredParentSizeToFitBacking = new Cached<Vector2>();

        /// <summary>
        /// Returns the size of the smallest axis aligned box in parent space which
        /// encompasses this drawable while preserving this drawable's
        /// <see cref="RelativeAnchorPosition"/>.
        /// If a component of <see cref="RelativeAnchorPosition"/> is smaller than zero
        /// or larger than one, then it is impossible to preserve <see cref="RelativeAnchorPosition"/>
        /// while fitting into the parent, and thus <see cref="RelativeAnchorPosition"/> returns
        /// zero in that dimension; i.e. we no longer fit into the parent.
        /// This behavior is prominent with non-centre and non-custom <see cref="Anchor"/> values.
        /// </summary>
        internal Vector2 RequiredParentSizeToFit => requiredParentSizeToFitBacking.EnsureValid()
            ? requiredParentSizeToFitBacking.Value
            : requiredParentSizeToFitBacking.Refresh(() =>
            {
                // Auxilary variables required for the computation
                Vector2 ap = AnchorPosition;
                Vector2 rap = RelativeAnchorPosition;

                Vector2 ratio1 = new Vector2(
                    rap.X <= 0 ? 0 : 1 / rap.X,
                    rap.Y <= 0 ? 0 : 1 / rap.Y);

                Vector2 ratio2 = new Vector2(
                    rap.X >= 1 ? 0 : 1 / (1 - rap.X),
                    rap.Y >= 1 ? 0 : 1 / (1 - rap.Y));

                RectangleF bbox = BoundingBox;

                // Compute the required size of the parent such that we fit in snugly when positioned
                // at our relative anchor in the parent.
                Vector2 topLeftOffset = ap - bbox.TopLeft;
                Vector2 topLeftSize1 = topLeftOffset * ratio1;
                Vector2 topLeftSize2 = -topLeftOffset * ratio2;

                Vector2 bottomRightOffset = ap - bbox.BottomRight;
                Vector2 bottomRightSize1 = bottomRightOffset * ratio1;
                Vector2 bottomRightSize2 = -bottomRightOffset * ratio2;

                // Expand bounds according to clipped offset
                return Vector2.ComponentMax(
                    Vector2.ComponentMax(topLeftSize1, topLeftSize2),
                    Vector2.ComponentMax(bottomRightSize1, bottomRightSize2));
            });

        private static readonly AtomicCounter invalidation_counter = new AtomicCounter();
        private long invalidationID;

        /// <summary>
        /// Invalidates draw matrix and autosize caches.
        /// </summary>
        /// <returns>If the invalidate was actually necessary.</returns>
        public virtual bool Invalidate(Invalidation invalidation = Invalidation.All, Drawable source = null, bool shallPropagate = true)
        {
            if (invalidation == Invalidation.None)
                return false;

            if (shallPropagate && Parent != null && source != Parent)
                Parent.InvalidateFromChild(invalidation);

            bool alreadyInvalidated = true;

            // Either ScreenSize OR ScreenPosition OR Colour
            if ((invalidation & (Invalidation.DrawInfo | Invalidation.RequiredParentSizeToFit | Invalidation.Colour)) > 0)
            {
                if ((invalidation & Invalidation.RequiredParentSizeToFit) > 0)
                    alreadyInvalidated &= !requiredParentSizeToFitBacking.Invalidate();

                alreadyInvalidated &= !screenSpaceDrawQuadBacking.Invalidate();
                alreadyInvalidated &= !drawInfoBacking.Invalidate();
                alreadyInvalidated &= !drawSizeBacking.Invalidate();
            }

            if (!alreadyInvalidated || (invalidation & Invalidation.DrawNode) > 0)
                invalidationID = invalidation_counter.Increment();

            OnInvalidate?.Invoke(this);

            return !alreadyInvalidated;
        }

        public Invalidation InvalidationFromParentSize
        {
            get
            {
                Invalidation result = Invalidation.DrawInfo;
                if (RelativeSizeAxes != Axes.None)
                    result |= Invalidation.DrawSize;
                if (RelativePositionAxes != Axes.None)
                    result |= Invalidation.MiscGeometry;
                return result;
            }
        }

        #endregion

        #region DrawNode

        private readonly DrawNode[] drawNodes = new DrawNode[3];

        /// <summary>
        /// Generates the DrawNode for ourselves.
        /// </summary>
        /// <returns>A complete and updated DrawNode, or null if the DrawNode would be invisible.</returns>
        internal virtual DrawNode GenerateDrawNodeSubtree(int treeIndex, RectangleF bounds)
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

        /// <summary>
        /// Fills a given draw node with all information required to draw this drawable.
        /// </summary>
        /// <param name="node">The node to fill with information.</param>
        protected virtual void ApplyDrawNode(DrawNode node)
        {
            node.DrawInfo = DrawInfo;
            node.InvalidationID = invalidationID;
        }

        /// <summary>
        /// Creates a draw node capable of containing all information required to draw this drawable.
        /// </summary>
        /// <returns>The created draw node.</returns>
        protected virtual DrawNode CreateDrawNode() => new DrawNode();

        #endregion

        #region DrawInfo-based coordinate system conversions

        /// <summary>
        /// Accepts a vector in local coordinates and converts it to coordinates in another Drawable's space.
        /// </summary>
        /// <param name="input">A vector in local coordinates.</param>
        /// <param name="other">The drawable in which space we want to transform the vector to.</param>
        /// <returns>The vector in other's coordinates.</returns>
        public Vector2 ToSpaceOfOtherDrawable(Vector2 input, IDrawable other)
        {
            if (other == this)
                return input;

            return input * DrawInfo.Matrix * other.DrawInfo.MatrixInverse;
        }

        /// <summary>
        /// Accepts a rectangle in local coordinates and converts it to coordinates in another Drawable's space.
        /// </summary>
        /// <param name="input">A rectangle in local coordinates.</param>
        /// <param name="other">The drawable in which space we want to transform the rectangle to.</param>
        /// <returns>The rectangle in other's coordinates.</returns>
        public Quad ToSpaceOfOtherDrawable(RectangleF input, IDrawable other)
        {
            if (other == this)
                return input;

            return Quad.FromRectangle(input) * (DrawInfo.Matrix * other.DrawInfo.MatrixInverse);
        }

        /// <summary>
        /// Accepts a vector in local coordinates and converts it to coordinates in Parent's space.
        /// </summary>
        /// <param name="input">A vector in local coordinates.</param>
        /// <returns>The vector in Parent's coordinates.</returns>
        public Vector2 ToParentSpace(Vector2 input) => ToSpaceOfOtherDrawable(input, Parent);

        /// <summary>
        /// Accepts a rectangle in local coordinates and converts it to a quad in Parent's space.
        /// </summary>
        /// <param name="input">A rectangle in local coordinates.</param>
        /// <returns>The quad in Parent's coordinates.</returns>
        public Quad ToParentSpace(RectangleF input) => ToSpaceOfOtherDrawable(input, Parent);

        /// <summary>
        /// Accepts a vector in local coordinates and converts it to coordinates in screen space.
        /// </summary>
        /// <param name="input">A vector in local coordinates.</param>
        /// <returns>The vector in screen coordinates.</returns>
        public Vector2 ToScreenSpace(Vector2 input)
        {
            return input * DrawInfo.Matrix;
        }

        /// <summary>
        /// Accepts a rectangle in local coordinates and converts it to a quad in screen space.
        /// </summary>
        /// <param name="input">A rectangle in local coordinates.</param>
        /// <returns>The quad in screen coordinates.</returns>
        public Quad ToScreenSpace(RectangleF input)
        {
            return Quad.FromRectangle(input) * DrawInfo.Matrix;
        }

        /// <summary>
        /// Accepts a vector in screen coordinates and converts it to coordinates in local space.
        /// </summary>
        /// <param name="screenSpacePos">A vector in screen coordinates.</param>
        /// <returns>The vector in local coordinates.</returns>
        public Vector2 ToLocalSpace(Vector2 screenSpacePos)
        {
            return screenSpacePos * DrawInfo.MatrixInverse;
        }

        /// <summary>
        /// Accepts a quad in screen coordinates and converts it to coordinates in local space.
        /// </summary>
        /// <param name="screenSpaceQuad">A quad in screen coordinates.</param>
        /// <returns>The quad in local coordinates.</returns>
        public Quad ToLocalSpace(Quad screenSpaceQuad)
        {
            return screenSpaceQuad * DrawInfo.MatrixInverse;
        }

        #endregion

        #region Interaction / Input

        /// <summary>
        /// Triggers <see cref="OnHover(InputState)"/> with a local version of the given <see cref="InputState"/>.
        /// </summary>
        public bool TriggerOnHover(InputState screenSpaceState) => OnHover(createCloneInParentSpace(screenSpaceState));

        /// <summary>
        /// Triggered once when this Drawable becomes hovered.
        /// </summary>
        /// <param name="state">The state at which the Drawable becomes hovered.</param>
        /// <returns>True if this Drawable would like to handle the hover. If so, then
        /// no further Drawables up the scene graph will receive hovering events. If
        /// false, however, then <see cref="OnHoverLost(InputState)"/> will still be
        /// received once hover is lost.</returns>
        protected virtual bool OnHover(InputState state) => false;

        /// <summary>
        /// Triggers <see cref="OnHoverLost(InputState)"/> with a local version of the given <see cref="InputState"/>.
        /// </summary>
        public void TriggerOnHoverLost(InputState screenSpaceState) => OnHoverLost(createCloneInParentSpace(screenSpaceState));

        /// <summary>
        /// Triggered whenever this drawable is no longer hovered.
        /// </summary>
        /// <param name="state">The state at which hover is lost.</param>
        protected virtual void OnHoverLost(InputState state)
        {
        }

        /// <summary>
        /// Triggers <see cref="OnMouseDown(InputState, MouseDownEventArgs)"/> with a local version of the given <see cref="InputState"/>.
        /// </summary>
        public bool TriggerOnMouseDown(InputState screenSpaceState = null, MouseDownEventArgs args = null) => OnMouseDown(createCloneInParentSpace(screenSpaceState), args);

        /// <summary>
        /// Triggered whenever a mouse button is pressed on top of this Drawable.
        /// </summary>
        /// <param name="state">The state after the press.</param>
        /// <param name="args">Specific arguments for mouse down event.</param>
        /// <returns>True if this Drawable handled the event. If false, then the event
        /// is propagated up the scene graph to the next eligible Drawable.</returns>
        protected virtual bool OnMouseDown(InputState state, MouseDownEventArgs args) => false;

        /// <summary>
        /// Triggers <see cref="OnMouseUp(InputState, MouseUpEventArgs)"/> with a local version of the given <see cref="InputState"/>.
        /// </summary>
        public bool TriggerOnMouseUp(InputState screenSpaceState = null, MouseUpEventArgs args = null) => OnMouseUp(createCloneInParentSpace(screenSpaceState), args);

        /// <summary>
        /// Triggered whenever a mouse button is released on top of this Drawable.
        /// </summary>
        /// <param name="state">The state after the release.</param>
        /// <param name="args">Specific arguments for mouse up event.</param>
        /// <returns>True if this Drawable handled the event. If false, then the event
        /// is propagated up the scene graph to the next eligible Drawable.</returns>
        protected virtual bool OnMouseUp(InputState state, MouseUpEventArgs args) => false;

        /// <summary>
        /// Triggers <see cref="OnClick(InputState)"/> with a local version of the given <see cref="InputState"/>.
        /// </summary>
        public bool TriggerOnClick(InputState screenSpaceState = null) => OnClick(createCloneInParentSpace(screenSpaceState));

        /// <summary>
        /// Triggered whenever a mouse click occurs on top of this Drawable.
        /// </summary>
        /// <param name="state">The state after the click.</param>
        /// <returns>True if this Drawable handled the event. If false, then the event
        /// is propagated up the scene graph to the next eligible Drawable.</returns>
        protected virtual bool OnClick(InputState state) => false;

        /// <summary>
        /// Triggers <see cref="OnMouseDown(InputState, MouseDownEventArgs)"/> with a local version of the given <see cref="InputState"/>.
        /// </summary>
        public bool TriggerOnDoubleClick(InputState screenSpaceState) => OnDoubleClick(createCloneInParentSpace(screenSpaceState));

        /// <summary>
        /// Triggered whenever a mouse double click occurs on top of this Drawable.
        /// </summary>
        /// <param name="state">The state after the double click.</param>
        /// <returns>True if this Drawable handled the event. If false, then the event
        /// is propagated up the scene graph to the next eligible Drawable.</returns>
        protected virtual bool OnDoubleClick(InputState state) => false;

        /// <summary>
        /// Triggers <see cref="OnDragStart(InputState)"/> with a local version of the given <see cref="InputState"/>.
        /// </summary>
        public bool TriggerOnDragStart(InputState screenSpaceState) => OnDragStart(createCloneInParentSpace(screenSpaceState));

        /// <summary>
        /// Triggered whenever this Drawable is initially dragged by a held mouse click
        /// and subsequent movement.
        /// </summary>
        /// <param name="state">The state after the mouse was moved.</param>
        /// <returns>True if this Drawable accepts being dragged. If so, then future
        /// <see cref="OnDrag(InputState)"/> and <see cref="OnDragEnd(InputState)"/>
        /// events will be reveiced. Otherwise, the event is propagated up the scene
        /// graph to the next eligible Drawable.</returns>
        protected virtual bool OnDragStart(InputState state) => false;

        /// <summary>
        /// Triggers <see cref="OnDrag(InputState)"/> with a local version of the given <see cref="InputState"/>.
        /// </summary>
        public bool TriggerOnDrag(InputState screenSpaceState) => OnDrag(createCloneInParentSpace(screenSpaceState));

        /// <summary>
        /// Triggered whenever the mouse is moved while dragging.
        /// Only is received if a drag was previously initiated by returning true
        /// from <see cref="OnDragStart(InputState)"/>.
        /// </summary>
        /// <param name="state">The state after the mouse was moved.</param>
        /// <returns>Currently unused.</returns>
        protected virtual bool OnDrag(InputState state) => false;

        /// <summary>
        /// Triggers <see cref="OnDragEnd(InputState)"/> with a local version of the given <see cref="InputState"/>.
        /// </summary>
        public bool TriggerOnDragEnd(InputState screenSpaceState) => OnDragEnd(createCloneInParentSpace(screenSpaceState));

        /// <summary>
        /// Triggered whenever a drag ended. Only is received if a drag was previously
        /// initiated by returning true from <see cref="OnDragStart(InputState)"/>.
        /// </summary>
        /// <param name="state">The state after the drag ended.</param>
        /// <returns>Currently unused.</returns>
        protected virtual bool OnDragEnd(InputState state) => false;

        /// <summary>
        /// Triggers <see cref="OnWheel(InputState)"/> with a local version of the given <see cref="InputState"/>.
        /// </summary>
        public bool TriggerOnWheel(InputState screenSpaceState) => OnWheel(createCloneInParentSpace(screenSpaceState));

        /// <summary>
        /// Triggered whenever the mouse wheel was turned over this Drawable.
        /// </summary>
        /// <param name="state">The state after the wheel was turned.</param>
        /// <returns>True if this Drawable handled the event. If false, then the event
        /// is propagated up the scene graph to the next eligible Drawable.</returns>
        protected virtual bool OnWheel(InputState state) => false;

        /// <summary>
        /// Triggers <see cref="OnFocus(InputState)"/> with a local version of the given <see cref="InputState"/>
        /// </summary>
        /// <param name="screenSpaceState">The input state.</param>
        public void TriggerOnFocus(InputState screenSpaceState = null) => OnFocus(createCloneInParentSpace(screenSpaceState));

        /// <summary>
        /// Triggered whenever this Drawable gains focus.
        /// Focused Drawables receive keyboard input before all other Drawables,
        /// and thus handle it first.
        /// </summary>
        /// <param name="state">The state after focus when focus can be gained.</param>
        protected virtual void OnFocus(InputState state)
        {
        }

        /// <summary>
        /// Triggers <see cref="OnFocusLost(InputState)"/> with a local version of the given <see cref="InputState"/>
        /// </summary>
        /// <param name="screenSpaceState">The input state.</param>
        public void TriggerOnFocusLost(InputState screenSpaceState = null) => OnFocusLost(createCloneInParentSpace(screenSpaceState));

        /// <summary>
        /// Triggered whenever this Drawable lost focus.
        /// </summary>
        /// <param name="state">The state after focus was lost.</param>
        protected virtual void OnFocusLost(InputState state)
        {
        }

        /// <summary>
        /// Triggers <see cref="OnKeyDown(InputState, KeyDownEventArgs)"/> with a local version of the given <see cref="InputState"/>.
        /// </summary>
        public bool TriggerOnKeyDown(InputState screenSpaceState, KeyDownEventArgs args) => OnKeyDown(createCloneInParentSpace(screenSpaceState), args);

        /// <summary>
        /// Triggered whenever a key was pressed.
        /// </summary>
        /// <param name="state">The state after the key was pressed.</param>
        /// <param name="args">Specific arguments for key down event.</param>
        /// <returns>True if this Drawable handled the event. If false, then the event
        /// is propagated up the scene graph to the next eligible Drawable.</returns>
        protected virtual bool OnKeyDown(InputState state, KeyDownEventArgs args) => false;

        /// <summary>
        /// Triggers <see cref="OnKeyUp(InputState, KeyUpEventArgs)"/> with a local version of the given <see cref="InputState"/>.
        /// </summary>
        public bool TriggerOnKeyUp(InputState screenSpaceState, KeyUpEventArgs args) => OnKeyUp(createCloneInParentSpace(screenSpaceState), args);

        /// <summary>
        /// Triggered whenever a key was released.
        /// </summary>
        /// <param name="state">The state after the key was released.</param>
        /// <param name="args">Specific arguments for key up event.</param>
        /// <returns>True if this Drawable handled the event. If false, then the event
        /// is propagated up the scene graph to the next eligible Drawable.</returns>
        protected virtual bool OnKeyUp(InputState state, KeyUpEventArgs args) => false;

        /// <summary>
        /// Triggers <see cref="OnMouseMove(InputState)"/> with a local version of the given <see cref="InputState"/>.
        /// </summary>
        public bool TriggerOnMouseMove(InputState screenSpaceState) => OnMouseMove(createCloneInParentSpace(screenSpaceState));

        /// <summary>
        /// Triggered whenever the mouse moved over this Drawable.
        /// </summary>
        /// <param name="state">The state after the mouse moved.</param>
        /// <returns>True if this Drawable handled the event. If false, then the event
        /// is propagated up the scene graph to the next eligible Drawable.</returns>
        protected virtual bool OnMouseMove(InputState state) => false;

        /// <summary>
        /// This drawable only receives input events if HandleInput is true.
        /// </summary>
        public virtual bool HandleInput => false;

        /// <summary>
        /// Check whether we have active focus.
        /// </summary>
        public bool HasFocus { get; internal set; }

        /// <summary>
        /// If true, we are eagerly requesting focus. If nothing else above us has (or is requesting focus) we will get it.
        /// </summary>
        public virtual bool RequestsFocus => false;

        /// <summary>
        /// If true, we will gain focus (receiving priority on keybaord input) (and receive an <see cref="OnFocus"/> event) on returning true in <see cref="OnClick(InputState)"/>.
        /// </summary>
        public virtual bool AcceptsFocus => false;

        /// <summary>
        /// Whether this Drawable is currently hovered over.
        /// </summary>
        public bool Hovering { get; internal set; }

        /// <summary>
        /// Determines whether this drawable receives mouse input when the mouse is at the
        /// given screen-space position.
        /// </summary>
        /// <param name="screenSpacePos">The screen-space position where input could be received.</param>
        /// <returns>True iff input is received at the given screen-space position.</returns>
        public virtual bool ReceiveMouseInputAt(Vector2 screenSpacePos) => Contains(screenSpacePos);

        /// <summary>
        /// Computes whether a given screen-space position is contained within this drawable.
        /// Mouse input events are only received when this function is true, or when the drawable
        /// is in focus.
        /// </summary>
        /// <param name="screenSpacePos">The screen space position to be checked against this drawable.</param>
        public virtual bool Contains(Vector2 screenSpacePos) => DrawRectangle.Contains(ToLocalSpace(screenSpacePos));

        /// <summary>
        /// Whether this Drawable can receive input, taking into account all optimizations and masking.
        /// </summary>
        public bool CanReceiveInput => HandleInput && IsPresent && !IsMaskedAway;

        /// <summary>
        /// Whether this Drawable is hovered by the given screen space mouse position,
        /// taking into account whether this Drawable can receive input.
        /// </summary>
        /// <param name="screenSpaceMousePos">The mouse position to be checked.</param>
        internal bool IsHovered(Vector2 screenSpaceMousePos) => CanReceiveInput && ReceiveMouseInputAt(screenSpaceMousePos);

        /// <summary>
        /// Creates a new InputState with mouse coodinates converted to the coordinate space of our parent.
        /// </summary>
        /// <param name="screenSpaceState">The screen-space input state to be cloned and transformed.</param>
        /// <returns>The cloned and transformed state.</returns>
        private InputState createCloneInParentSpace(InputState screenSpaceState)
        {
            if (screenSpaceState == null) return null;

            return new InputState
            {
                Keyboard = screenSpaceState.Keyboard,
                Mouse = new LocalMouseState(screenSpaceState.Mouse.NativeState, this),
                Last = screenSpaceState.Last
            };
        }

        /// <summary>
        /// This method is responsible for building a queue of Drawables to receive keyboard input
        /// in reverse order. This method is overridden by <see cref="T:Container"/> to be called on all
        /// children such that the entire scene graph is covered.
        /// </summary>
        /// <param name="queue">The input queue to be built.</param>
        /// <returns>Whether we have added ourself to the queue.</returns>
        internal virtual bool BuildKeyboardInputQueue(List<Drawable> queue)
        {
            if (!CanReceiveInput)
                return false;

            queue.Add(this);
            return true;
        }

        /// <summary>
        /// This method is responsible for building a queue of Drawables to receive mouse input
        /// in reverse order. This method is overridden by <see cref="T:Container"/> to be called on all
        /// children such that the entire scene graph is covered.
        /// </summary>
        /// <param name="screenSpaceMousePos">The current position of the mouse cursor in screen space.</param>
        /// <param name="queue">The input queue to be built.</param>
        /// <returns>Whether we have added ourself to the queue.</returns>
        internal virtual bool BuildMouseInputQueue(Vector2 screenSpaceMousePos, List<Drawable> queue)
        {
            if (!IsHovered(screenSpaceMousePos))
                return false;

            queue.Add(this);
            return true;
        }

        private struct LocalMouseState : IMouseState
        {
            public IMouseState NativeState { get; }

            public IMouseState LastState { get; set; }

            private readonly Drawable us;

            public LocalMouseState(IMouseState state, Drawable us)
            {
                NativeState = state;
                LastState = null;
                this.us = us;
            }

            public Vector2 Delta => Position - LastPosition;

            public Vector2 Position => us.Parent?.ToLocalSpace(NativeState.Position) ?? NativeState.Position;

            public Vector2 LastPosition => us.Parent?.ToLocalSpace(NativeState.LastPosition) ?? NativeState.LastPosition;

            public Vector2? PositionMouseDown
            {
                get { return NativeState.PositionMouseDown == null ? null : us.Parent?.ToLocalSpace(NativeState.PositionMouseDown.Value) ?? NativeState.PositionMouseDown; }
                set { throw new NotImplementedException(); }
            }

            public bool HasMainButtonPressed => NativeState.HasMainButtonPressed;

            public bool HasAnyButtonPressed => NativeState.HasAnyButtonPressed;

            public int Wheel => NativeState.Wheel;
            public int WheelDelta => NativeState.WheelDelta;

            public bool IsPressed(MouseButton button) => NativeState.IsPressed(button);

            public void SetPressed(MouseButton button, bool pressed) => NativeState.SetPressed(button, pressed);

            public IMouseState Clone()
            {
                throw new NotSupportedException();
            }
        }

        #endregion

        #region Transforms

        protected ScheduledDelegate Schedule(Action action) => Scheduler.AddDelayed(action, TransformDelay);

        /// <summary>
        /// Make this drawable automatically clean itself up after all transforms have finished playing.
        /// Can be delayed using Delay().
        /// </summary>
        public void Expire(bool calculateLifetimeStart = false)
        {
            if (clock == null)
            {
                LifetimeEnd = double.MinValue;
                return;
            }

            //expiry should happen either at the end of the last transform or using the current sequence delay (whichever is highest).
            double max = TransformStartTime;
            foreach (ITransform<Drawable> t in Transforms)
                if (t.EndTime > max) max = t.EndTime + 1; //adding 1ms here ensures we can expire on the current frame without issue.
            LifetimeEnd = max;

            if (calculateLifetimeStart)
            {
                double min = double.MaxValue;
                foreach (ITransform<Drawable> t in Transforms)
                    if (t.StartTime < min) min = t.StartTime;
                LifetimeStart = min < int.MaxValue ? min : int.MinValue;
            }
        }

        /// <summary>
        /// Hide sprite instantly.
        /// </summary>
        /// <returns></returns>
        public virtual void Hide()
        {
            FadeOut();
        }

        /// <summary>
        /// Show sprite instantly.
        /// </summary>
        public virtual void Show()
        {
            FadeIn();
        }

        #region Helpers

        public void FadeIn(double duration = 0, EasingTypes easing = EasingTypes.None)
        {
            FadeTo(1, duration, easing);
        }

        public void FadeInFromZero(double duration = 0, EasingTypes easing = EasingTypes.None)
        {
            FadeTo(0);
            FadeIn(duration, easing);
        }

        public void FadeOut(double duration = 0, EasingTypes easing = EasingTypes.None)
        {
            FadeTo(0, duration, easing);
        }

        public void FadeOutFromOne(double duration = 0, EasingTypes easing = EasingTypes.None)
        {
            FadeTo(1);
            FadeOut(duration, easing);
        }

        public void FadeTo(float newAlpha, double duration = 0, EasingTypes easing = EasingTypes.None)
        {
            TransformTo(() => Alpha, newAlpha, duration, easing, new TransformAlpha());
        }

        public void RotateTo(float newRotation, double duration = 0, EasingTypes easing = EasingTypes.None)
        {
            TransformTo(() => Rotation, newRotation, duration, easing, new TransformRotation());
        }

        public void MoveTo(Direction direction, float destination, double duration = 0, EasingTypes easing = EasingTypes.None)
        {
            switch (direction)
            {
                case Direction.Horizontal:
                    MoveToX(destination, duration, easing);
                    break;
                case Direction.Vertical:
                    MoveToY(destination, duration, easing);
                    break;
            }
        }

        public void MoveToX(float destination, double duration = 0, EasingTypes easing = EasingTypes.None)
        {
            TransformTo(() => Position.X, destination, duration, easing, new TransformPositionX());
        }

        public void MoveToY(float destination, double duration = 0, EasingTypes easing = EasingTypes.None)
        {
            TransformTo(() => Position.Y, destination, duration, easing, new TransformPositionY());
        }

        public void ScaleTo(float newScale, double duration = 0, EasingTypes easing = EasingTypes.None)
        {
            TransformTo(() => Scale, new Vector2(newScale), duration, easing, new TransformScale());
        }

        public void ScaleTo(Vector2 newScale, double duration = 0, EasingTypes easing = EasingTypes.None)
        {
            TransformTo(() => Scale, newScale, duration, easing, new TransformScale());
        }

        public void ResizeTo(float newSize, double duration = 0, EasingTypes easing = EasingTypes.None)
        {
            TransformTo(() => Size, new Vector2(newSize), duration, easing, new TransformSize());
        }

        public void ResizeTo(Vector2 newSize, double duration = 0, EasingTypes easing = EasingTypes.None)
        {
            TransformTo(() => Size, newSize, duration, easing, new TransformSize());
        }

        public void ResizeWidthTo(float newWidth, double duration = 0, EasingTypes easing = EasingTypes.None)
        {
            TransformTo(() => Width, newWidth, duration, easing, new TransformWidth());
        }

        public void ResizeHeightTo(float newHeight, double duration = 0, EasingTypes easing = EasingTypes.None)
        {
            TransformTo(() => Height, newHeight, duration, easing, new TransformHeight());
        }

        public void MoveTo(Vector2 newPosition, double duration = 0, EasingTypes easing = EasingTypes.None)
        {
            TransformTo(() => Position, newPosition, duration, easing, new TransformPosition());
        }

        public void MoveToOffset(Vector2 offset, double duration = 0, EasingTypes easing = EasingTypes.None)
        {
            MoveTo((Transforms.FindLast(t => t is TransformPosition) as TransformPosition)?.EndValue ?? Position + offset, duration, easing);
        }

        public void FadeColour(Color4 newColour, double duration = 0, EasingTypes easing = EasingTypes.None)
        {
            TransformTo(() => Colour, newColour, duration, easing, new TransformColour());
        }

        public void FlashColour(Color4 flashColour, double duration, EasingTypes easing = EasingTypes.None)
        {
            Color4 endValue = (Transforms.FindLast(t => t is TransformColour) as TransformColour)?.EndValue ?? Colour;

            Flush(false, typeof(TransformColour));

            FadeColour(flashColour);
            FadeColour(endValue, duration, easing);
        }

        #endregion

        #endregion

        #region Effects

        /// <summary>
        /// Returns the drawable created by applying the given effect to this drawable. This method may add this drawable to a container.
        /// If this drawable should be the child of another container, make sure to add the created drawable to the container instead of this drawable.
        /// </summary>
        /// <typeparam name="T">The type of the drawable that results from applying the given effect.</typeparam>
        /// <param name="effect">The effect to apply to this drawable.</param>
        /// <param name="initializationAction">The action that should get called to initialize the created drawable before it is returned.</param>
        /// <returns>The drawable created by applying the given effect to this drawable.</returns>
        public T WithEffect<T>(IEffect<T> effect, Action<T> initializationAction = null) where T : Drawable
        {
            var result = effect.ApplyTo(this);
            initializationAction?.Invoke(result);
            return result;
        }

        #endregion

        /// <summary>
        /// A name used to identify this Drawable internally.
        /// </summary>
        public string Name = string.Empty;

        public override string ToString()
        {
            string shortClass = GetType().ReadableName();

            if (!string.IsNullOrEmpty(Name))
                shortClass = $@"{Name} ({shortClass})";

            return $@"{shortClass} ({DrawPosition.X:#,0},{DrawPosition.Y:#,0}) {DrawSize.X:#,0}x{DrawSize.Y:#,0}";
        }
    }

    /// <summary>
    /// Specifies which type of properties are being invalidated.
    /// </summary>
    [Flags]
    public enum Invalidation
    {
        /// <summary>
        /// <see cref="Drawable.DrawInfo"/> has changed. No change to <see cref="Drawable.RequiredParentSizeToFit"/> or <see cref="Drawable.DrawSize"/>
        /// is assumed unless indicated by additional flags.
        /// </summary>
        DrawInfo = 1 << 0,
        /// <summary>
        /// <see cref="Drawable.DrawSize"/> has changed.
        /// </summary>
        DrawSize = 1 << 1,
        /// <summary>
        /// Captures all other geometry changes than <see cref="Drawable.DrawSize"/>, such as
        /// <see cref="Drawable.Rotation"/>, <see cref="Drawable.Shear"/>, and <see cref="Drawable.DrawPosition"/>.
        /// </summary>
        MiscGeometry = 1 << 2,
        /// <summary>
        /// Our colour changed.
        /// </summary>
        Colour = 1 << 3,
        /// <summary>
        /// <see cref="Drawable.ApplyDrawNode(Graphics.DrawNode)"/> has to be invoked on all old draw nodes.
        /// </summary>
        DrawNode = 1 << 4,

        /// <summary>
        /// No invalidation.
        /// </summary>
        None = 0,
        /// <summary>
        /// <see cref="Drawable.RequiredParentSizeToFit"/> has to be recomputed.
        /// </summary>
        RequiredParentSizeToFit = MiscGeometry | DrawSize,
        /// <summary>
        /// All possible things are affected.
        /// </summary>
        All = DrawNode | RequiredParentSizeToFit | Colour | DrawInfo,
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

        Both = X | Y,
    }

    public enum Direction
    {
        Horizontal = 0,
        Vertical = 1,
    }

    public enum BlendingMode
    {
        /// <summary>
        /// Inherits from parent.
        /// </summary>
        Inherit = 0,
        /// <summary>
        /// Mixes with existing colour by a factor of the colour's alpha.
        /// </summary>
        Mixture,
        /// <summary>
        /// Purely additive (by a factor of the colour's alpha) blending.
        /// </summary>
        Additive,
        /// <summary>
        /// No alpha blending whatsoever.
        /// </summary>
        None,
    }

    /// <summary>
    /// Possible states of a <see cref="Drawable"/> within the loading pipeline.
    /// </summary>
    public enum LoadState
    {
        /// <summary>
        /// Not loaded, and no load has been initiated yet.
        /// </summary>
        NotLoaded,
        /// <summary>
        /// Currently loading (possibly and usually on a background
        /// thread via <see cref="Drawable.LoadAsync{T}(Game, Drawable, Action{T})"/>).
        /// </summary>
        Loading,
        /// <summary>
        /// Loading is complete, but has not yet been finalized on the update thread
        /// (<see cref="Drawable.LoadComplete"/> has not been called yet, which
        /// always runs on the update thread and requires <see cref="Drawable.IsLoaded"/>).
        /// </summary>
        Loaded,
        /// <summary>
        /// Loading is fully completed and the Drawable is now part of the scene graph.
        /// </summary>
        Alive
    }

    public enum FillMode
    {
        /// <summary>
        /// This drawable shouldn't automatically fill its parent space.
        /// </summary>
        None,
        /// <summary>
        /// This drawable should be scaled to fill its parent space while maintaining aspect ratio.
        /// </summary>
        Fill,
        /// <summary>
        /// This drawable should be scaled to fit inside the dimensions of its parent space while maintaining aspect ratio.
        /// </summary>
        Fit,
        /// <summary>
        /// This drawable should stretch to fill its parent space.
        /// </summary>
        Stretch
    }
}
