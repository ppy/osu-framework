﻿// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Collections.Generic;
using System.Diagnostics;
using osu.Framework.DebugUtils;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Transforms;
using osu.Framework.Lists;
using osu.Framework.Timing;
using OpenTK;
using OpenTK.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Threading;
using System.Threading;
using System.Threading.Tasks;
using osu.Framework.Caching;
using osu.Framework.Extensions;
using osu.Framework.Logging;
using osu.Framework.Statistics;
using osu.Framework.Graphics.Colour;
using osu.Framework.Input;

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
    public abstract class Drawable : IDisposable, IHasLifetime, IDrawable
    {
        #region Construction and disposal

        protected Drawable()
        {
            creationID = creationCounter.Increment();
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
        /// Override to add delayed load abilities (ie. using IsAlive)
        /// </summary>
        public virtual bool IsLoaded => loadState >= LoadState.Loaded;

        private volatile LoadState loadState;
        public LoadState LoadState => loadState;

        private Task loadTask;
        private object loadLock = new object();

        /// <summary>
        /// Loads this Drawable asynchronously.
        /// </summary>
        /// <param name="game">The game to load this Drawable on.</param>
        /// <param name="onLoaded">
        /// Callback to be invoked asynchronously
        /// after loading is complete.
        /// </param>
        /// <returns>The task which is used for loading and callbacks.</returns>
        public async Task LoadAsync(Game game, Action<Drawable> onLoaded = null)
        {
            if (loadState != LoadState.NotLoaded)
                throw new InvalidOperationException($@"{nameof(LoadAsync)} may not be called more than once on the same Drawable.");

            loadState = LoadState.Loading;

            loadTask = Task.Run(() => Load(game)).ContinueWith(task => game.Schedule(() =>
            {
                task.ThrowIfFaulted();
                onLoaded?.Invoke(this);
            }));

            await loadTask;

            loadTask = null;
        }

        private static StopwatchClock perf = new StopwatchClock(true);

        internal void Load(Game game)
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

                double t1 = perf.CurrentTime;
                game.Dependencies.Initialize(this);
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

            mainThread = Thread.CurrentThread;
            scheduler?.SetCurrentThread(mainThread);

            LifetimeStart = Time.Current;
            Invalidate();
            loadState = LoadState.Alive;
            LoadComplete();
            return true;
        }

        /// <summary>
        /// Play initial animation etc.
        /// </summary>
        protected virtual void LoadComplete() { }

        #endregion

        #region Sorting (CreationID / Depth)

        /// <summary>
        /// Captures the order in which Drawables were created. Each Drawable
        /// is assigned a unique, monotonically increasing ID upon creation in a thread-safe manner.
        /// The primary use case of this ID is stable sorting of Drawables with equal
        /// <see cref="Depth"/>.
        /// </summary>
        private long creationID { get; }
        private static AtomicCounter creationCounter = new AtomicCounter();

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
                if (Parent != null)
                    throw new InvalidOperationException("May not change depth while inside a parent container.");
                depth = value;
            }
        }

        public class CreationOrderDepthComparer : IComparer<Drawable>
        {
            public int Compare(Drawable x, Drawable y)
            {
                int i = y.Depth.CompareTo(x.Depth);
                if (i != 0) return i;
                return x.creationID.CompareTo(y.creationID);
            }
        }

        public class ReverseCreationOrderDepthComparer : IComparer<Drawable>
        {
            public int Compare(Drawable x, Drawable y)
            {
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
        public event Action OnUpdate;

        /// <summary>
        /// THIS EVENT PURELY EXISTS FOR THE SCENE GRAPH VISUALIZER. DO NOT USE.
        /// This event is fired after the <see cref="Invalidate(Invalidation, Drawable, bool)"/> method is called.
        /// </summary>
        internal event Action OnInvalidate;

        private Scheduler scheduler;
        private Thread mainThread;

        /// <summary>
        /// A lazily-initialized scheduler used to schedule tasks to be invoked in future <see cref="Update"/>s calls.
        /// The tasks are invoked at the beginning of the <see cref="Update"/> method before anything else.
        /// </summary>
        protected Scheduler Scheduler
        {
            get
            {
                if (scheduler == null)
                    // mainThread could be null at this point.
                    // If so, then it will be set upon LoadComplete.
                    scheduler = new Scheduler(mainThread);

                return scheduler;
            }
        }

        private LifetimeList<ITransform> transforms;

        /// <summary>
        /// A lazily-initialized list of <see cref="ITransform"/>s applied to this Drawable.
        /// <see cref="ITransform"/>s are applied right before the <see cref="Update"/> method is called.
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

        protected void UpdateTransformsOfType(Type specificType)
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

        /// <summary>
        /// Updates this Drawable and all Drawables further down the scene graph.
        /// Called once every frame.
        /// </summary>
        /// <returns>False if the drawable should not be updated.</returns>
        internal virtual bool UpdateSubTree()
        {
            if (isDisposed)
                throw new ObjectDisposedException(ToString(), "Disposed Drawables may never be in the scene graph.");

            if (Parent != null) //we don't want to update our clock if we are at the top of the stack. it's handled elsewhere for us.
                customClock?.ProcessFrame();

            if (loadState < LoadState.Alive)
                if (!loadComplete()) return false;

            transformDelay = 0;

            //todo: this should be moved to after the IsVisible condition once we have TOL for transforms (and some better logic).
            updateTransforms();

            if (!IsPresent)
                return true;

            if (scheduler != null)
            {
                int amountScheduledTasks = scheduler.Update();
                FrameStatistics.Increment(StatisticsCounterType.ScheduleInvk, amountScheduledTasks);
            }

            Update();
            OnUpdate?.Invoke();
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
            set { x = value.X; y = value.Y; }
        }

        /// <summary>
        /// Positional offset of <see cref="Origin"/> to <see cref="AnchorPosition"/> in the
        /// <see cref="Parent"/>'s coordinate system. May be in absolute or relative units
        /// (controlled by <see cref="RelativePositionAxes"/>).
        /// </summary>
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

        float x;
        float y;

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

                Invalidate(Invalidation.Geometry);
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

                Invalidate(Invalidation.Geometry);
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
                    X = conversion.X == 0 ? 0 : (X / conversion.X);
                else if ((relativePositionAxes & Axes.X) > (value & Axes.X))
                    X *= conversion.X;

                if ((value & Axes.Y) > (relativePositionAxes & Axes.Y))
                    Y = conversion.Y == 0 ? 0 : (Y / conversion.Y);
                else if ((relativePositionAxes & Axes.X) > (value & Axes.X))
                    Y *= conversion.Y;

                relativePositionAxes = value;

                // No invalidation necessary as DrawPosition remains invariant.
            }
        }

        /// <summary>
        /// Absolute positional offset of <see cref="Origin"/> to <see cref="AnchorPosition"/>
        /// in the <see cref="Parent"/>'s coordinate system.
        /// </summary>
        public Vector2 DrawPosition => applyRelativeAxes(RelativePositionAxes, Position);

        private Vector2 size
        {
            get { return new Vector2(width, height); }
            set { width = value.X; height = value.Y; }
        }

        /// <summary>
        /// Size of this Drawable in the <see cref="Parent"/>'s coordinate system.
        /// May be in absolute or relative units (controlled by <see cref="RelativeSizeAxes"/>).
        /// </summary>
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

                Invalidate(Invalidation.Geometry);
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

                Invalidate(Invalidation.Geometry);
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
                    Width = conversion.X == 0 ? 0 : (Width / conversion.X);
                else if ((relativeSizeAxes & Axes.X) > (value & Axes.X))
                    Width *= conversion.X;

                if ((value & Axes.Y) > (relativeSizeAxes & Axes.Y))
                    Height = conversion.Y == 0 ? 0 : (Height / conversion.Y);
                else if ((relativeSizeAxes & Axes.X) > (value & Axes.X))
                    Height *= conversion.Y;

                relativeSizeAxes = value;

                if ((relativeSizeAxes & Axes.X) > 0 && Width == 0) Width = 1;
                if ((relativeSizeAxes & Axes.Y) > 0 && Height == 0) Height = 1;

                // No invalidation necessary as DrawSize remains invariant.
            }
        }

        private Cached<Vector2> drawSizeBacking = new Cached<Vector2>();

        /// <summary>
        /// Absolute size of this Drawable in the <see cref="Parent"/>'s coordinate system.
        /// </summary>
        public Vector2 DrawSize => drawSizeBacking.EnsureValid() ?
            drawSizeBacking.Value :
            drawSizeBacking.Refresh(() => applyRelativeAxes(RelativeSizeAxes, Size));

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

                Invalidate(Invalidation.Geometry);
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

        private Vector2 relativeToAbsoluteFactor => Parent?.ChildSize ?? Vector2.One;

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
                Parent?.InvalidateFromChild(Invalidation.Geometry, this);
            }
        }

        /// <summary>
        /// Computes the bounding box of this drawable in its parent's space.
        /// </summary>
        public virtual RectangleF BoundingBox => ToParentSpace(LayoutRectangle).AABBFloat;

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

                Invalidate(Invalidation.Geometry);
            }
        }

        /// <summary>
        /// Relative scaling factor around <see cref="OriginPosition"/>.
        /// </summary>
        protected virtual Vector2 DrawScale => Scale;

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

                Invalidate(Invalidation.Geometry);
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

                Invalidate(Invalidation.Geometry);
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
                Invalidate(Invalidation.Geometry);
            }
        }


        private Vector2 customOrigin;

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
        /// or a fixed absolute position via <see cref="AnchorPosition"/>.
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
                Invalidate(Invalidation.Geometry);
            }
        }


        private Vector2 customAnchor;

        /// <summary>
        /// Specifies in absolute coordinates where <see cref="Origin"/> is attached
        /// to the <see cref="Parent"/> in the coordinate system with origin at the top
        /// left corner of the <see cref="Parent"/>'s <see cref="DrawRectangle"/>.
        /// </summary>
        public virtual Vector2 AnchorPosition
        {
            get
            {
                if (Anchor == Anchor.Custom)
                    return customAnchor;

                if (Anchor == Anchor.TopLeft || Parent == null)
                    return Vector2.Zero;

                return computeAnchorPosition(Parent.ChildSize, Anchor);
            }

            set
            {
                customAnchor = value;
                Anchor = Anchor.Custom;
            }
        }

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
            get
            {
                return colourInfo.Colour;
            }

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

        const float visibility_cutoff = 0.0001f;

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
        public IFrameBasedClock Clock
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
        internal virtual void UpdateClock(IFrameBasedClock clock)
        {
            this.clock = customClock ?? clock;
        }

        /// <summary>
        /// The current frame's time as observed by this drawable's <see cref="Clock"/>.
        /// </summary>
        public FrameTimeInfo Time => Clock.TimeInfo;

        /// <summary>
        /// The time at which this drawable becomes valid (and is considered for drawing).
        /// </summary>
        public double LifetimeStart { get; set; } = double.MinValue;

        /// <summary>
        /// The time at which this drawable is no longer valid (and is considered for disposal).
        /// </summary>
        public double LifetimeEnd { get; set; } = double.MaxValue;

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

        private IContainer parent;

        /// <summary>
        /// The parent of this drawable in the scene graph.
        /// </summary>
        public IContainer Parent
        {
            get { return parent; }
            set
            {
                if (isDisposed)
                    throw new ObjectDisposedException(ToString(), "Disposed Drawables may never get a parent and return to the scene graph.");

                if (parent == value) return;

                if (value != null && parent != null)
                    throw new InvalidOperationException("May not add a drawable to multiple containers.");

                parent = value;
                if (parent != null)
                    UpdateClock(parent.Clock);
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
        /// Creates a proxy drawable which can be inserted elsewhere in the draw hierarchy.
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
        public virtual DrawInfo DrawInfo => drawInfoBacking.EnsureValid() ? drawInfoBacking.Value : drawInfoBacking.Refresh(delegate
            {
                DrawInfo di = Parent?.DrawInfo ?? new DrawInfo(null);

                Vector2 position = DrawPosition + AnchorPosition;
                Vector2 scale = DrawScale;
                BlendingMode blendingMode = BlendingMode;

                if (Parent != null)
                {
                    position += Parent.ChildOffset;

                    if (blendingMode == BlendingMode.Inherit)
                        blendingMode = Parent.BlendingMode;
                }

                di.ApplyTransform(position, scale, Rotation, Shear, OriginPosition);
                di.Blending = new BlendingInfo(blendingMode);

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

        private Cached<Vector2> boundingSizeWithOriginBacking = new Cached<Vector2>();

        /// <summary>
        /// Returns the size of the smallest axis aligned box in parent space which
        /// encompasses this drawable and the parent's origin. Note, that negative
        /// sizes are clamped to zero (i.e. can never be negative).
        /// </summary>
        internal Vector2 BoundingSizeWithOrigin => boundingSizeWithOriginBacking.EnsureValid()
            ? boundingSizeWithOriginBacking.Value
            : boundingSizeWithOriginBacking.Refresh(() =>
            {
                //field will be none when the drawable isn't requesting auto-sizing
                RectangleF bbox = BoundingBox;

                Vector2 bounds = new Vector2(0, 0);

                // Without this, 0x0 objects (like FontText with no string) produce weird results.
                // When all vertices of the quad are at the same location, then the object is effectively invisible.
                // Thus we don't need its actual bounding box, but can just assume a size of 0.
                if (bbox.Width <= 0 && bbox.Height <= 0)
                    return bounds;

                Vector2 a = AnchorPosition;

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
                    // Centre or custom
                    else
                        offset.X = Math.Abs(p.X - a.X);

                    // Bottom
                    if ((Anchor & Anchor.y2) > 0)
                        offset.Y = a.Y - p.Y;
                    // Top
                    else if ((Anchor & Anchor.y0) > 0)
                        offset.Y = p.Y - a.Y;
                    // Centre or custom
                    else
                        offset.Y = Math.Abs(p.Y - a.Y);

                    // Expand bounds according to clipped offset
                    bounds.X = Math.Max(bounds.X, offset.X);
                    bounds.Y = Math.Max(bounds.Y, offset.Y);
                }

                // When anchoring an object at the center of the parent, then the parent's size needs to be twice as big
                // as the child's size.
                if ((Anchor & Anchor.x1) > 0)
                    bounds.X *= 2;

                if ((Anchor & Anchor.y1) > 0)
                    bounds.Y *= 2;

                return bounds;
            });

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

            if (shallPropagate && Parent != null && source != Parent)
                Parent.InvalidateFromChild(invalidation, this);

            bool alreadyInvalidated = true;

            // Either ScreenSize OR ScreenPosition OR Colour
            if ((invalidation & (Invalidation.Geometry | Invalidation.Colour)) > 0)
            {
                if ((invalidation & Invalidation.SizeInParentSpace) > 0)
                    alreadyInvalidated &= !boundingSizeWithOriginBacking.Invalidate();

                alreadyInvalidated &= !screenSpaceDrawQuadBacking.Invalidate();
                alreadyInvalidated &= !drawInfoBacking.Invalidate();
                alreadyInvalidated &= !drawSizeBacking.Invalidate();
            }

            if (!alreadyInvalidated || (invalidation & Invalidation.DrawNode) > 0)
                invalidationID = invalidationCounter.Increment();

            OnInvalidate?.Invoke();

            return !alreadyInvalidated;
        }

        #endregion

        #region DrawNode

        private DrawNode[] drawNodes = new DrawNode[3];

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

        protected virtual void ApplyDrawNode(DrawNode node)
        {
            node.DrawInfo = DrawInfo;
            node.InvalidationID = invalidationID;
        }

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
        /// Accepts a vector in local coordinates and converts it to coordinates in Parent's space.
        /// </summary>
        /// <param name="input">A vector in local coordinates.</param>
        /// <returns>The vector in Parent's coordinates.</returns>
        public Vector2 ToParentSpace(Vector2 input)
        {
            return ToSpaceOfOtherDrawable(input, Parent);
        }

        /// <summary>
        /// Accepts a rectangle in local coordinates and converts it to a quad in Parent's space.
        /// </summary>
        /// <param name="input">A rectangle in local coordinates.</param>
        /// <returns>The quad in Parent's coordinates.</returns>
        public Quad ToParentSpace(RectangleF input)
        {
            return Quad.FromRectangle(input) * (DrawInfo.Matrix * Parent.DrawInfo.MatrixInverse);
        }

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
        /// Convert a position to the local coordinate system from either native or local to another drawable.
        /// This is *not* the same space as the Position member variable (use Parent.GetLocalPosition() in this case).
        /// </summary>
        /// <param name="screenSpacePos">The input position.</param>
        /// <returns>The output position.</returns>
        public Vector2 ToLocalSpace(Vector2 screenSpacePos)
        {
            return screenSpacePos * DrawInfo.MatrixInverse;
        }

        #endregion

        #region Interaction / Input

        /// <summary>
        /// Find the first parent InputManager which this drawable is contained by.
        /// </summary>
        private InputManager ourInputManager => this as InputManager ?? (Parent as Drawable)?.ourInputManager;

        public bool TriggerHover(InputState screenSpaceState) => OnHover(toParentSpace(screenSpaceState));

        protected virtual bool OnHover(InputState state) => false;

        public void TriggerHoverLost(InputState screenSpaceState) => OnHoverLost(toParentSpace(screenSpaceState));

        protected virtual void OnHoverLost(InputState state)
        {
        }

        public bool TriggerMouseDown(InputState screenSpaceState = null, MouseDownEventArgs args = null) => OnMouseDown(toParentSpace(screenSpaceState), args);

        protected virtual bool OnMouseDown(InputState state, MouseDownEventArgs args) => false;

        public bool TriggerMouseUp(InputState screenSpaceState = null, MouseUpEventArgs args = null) => OnMouseUp(toParentSpace(screenSpaceState), args);

        protected virtual bool OnMouseUp(InputState state, MouseUpEventArgs args) => false;

        public bool TriggerClick(InputState screenSpaceState = null) => OnClick(toParentSpace(screenSpaceState));

        protected virtual bool OnClick(InputState state) => false;

        public bool TriggerDoubleClick(InputState screenSpaceState) => OnDoubleClick(toParentSpace(screenSpaceState));

        protected virtual bool OnDoubleClick(InputState state) => false;

        public bool TriggerDragStart(InputState screenSpaceState) => OnDragStart(toParentSpace(screenSpaceState));

        protected virtual bool OnDragStart(InputState state) => false;

        public bool TriggerDrag(InputState screenSpaceState) => OnDrag(toParentSpace(screenSpaceState));

        protected virtual bool OnDrag(InputState state) => false;

        public bool TriggerDragEnd(InputState screenSpaceState) => OnDragEnd(toParentSpace(screenSpaceState));

        protected virtual bool OnDragEnd(InputState state) => false;

        public bool TriggerWheel(InputState screenSpaceState) => OnWheel(toParentSpace(screenSpaceState));

        protected virtual bool OnWheel(InputState state) => false;

        /// <summary>
        /// Focuses this drawable.
        /// </summary>
        /// <param name="screenSpaceState">The input state.</param>
        /// <param name="checkCanFocus">Whether we should check this Drawable's OnFocus returns true before actually providing focus.</param>
        public bool TriggerFocus(InputState screenSpaceState = null, bool checkCanFocus = false)
        {
            if (HasFocus)
                return true;

            if (!IsPresent)
                return false;

            if (checkCanFocus & !OnFocus(toParentSpace(screenSpaceState)))
                return false;

            ourInputManager?.ChangeFocus(this);

            return true;
        }

        /// <summary>
        /// If we are not the current focus, this will force our parent InputManager to reconsider what to focus.
        /// Useful in combination with <see cref="RequestingFocus"/>
        /// Make sure you are already Present (ie. you've run Update at least once after becoming visible). Schedule recommended.
        /// </summary>
        protected void TriggerFocusContention()
        {
            if (!IsPresent)
                throw new InvalidOperationException("Can not obtain focus without being present.");

            if (ourInputManager.FocusedDrawable != this)
                ourInputManager.ChangeFocus(null);
        }

        protected virtual bool OnFocus(InputState state) => false;

        /// <summary>
        /// Unfocuses this drawable.
        /// </summary>
        /// <param name="screenSpaceState">The input state.</param>
        /// <param name="isCallback">Used to aavoid cyclid recursion.</param>
        public void TriggerFocusLost(InputState screenSpaceState = null, bool isCallback = false)
        {
            if (!HasFocus)
                return;

            if (screenSpaceState == null)
                screenSpaceState = new InputState { Keyboard = new KeyboardState(), Mouse = new MouseState() };

            if (!isCallback) ourInputManager.ChangeFocus(null);
            OnFocusLost(toParentSpace(screenSpaceState));
        }

        protected virtual void OnFocusLost(InputState state)
        {
        }

        public bool TriggerKeyDown(InputState screenSpaceState, KeyDownEventArgs args) => OnKeyDown(toParentSpace(screenSpaceState), args);

        protected virtual bool OnKeyDown(InputState state, KeyDownEventArgs args) => false;

        public bool TriggerKeyUp(InputState screenSpaceState, KeyUpEventArgs args) => OnKeyUp(toParentSpace(screenSpaceState), args);

        protected virtual bool OnKeyUp(InputState state, KeyUpEventArgs args) => false;

        public bool TriggerMouseMove(InputState screenSpaceState) => OnMouseMove(toParentSpace(screenSpaceState));

        protected virtual bool OnMouseMove(InputState state) => false;

        /// <summary>
        /// This drawable only receives input events if HandleInput is true.
        /// </summary>
        public virtual bool HandleInput => false;

        /// <summary>
        /// Check whether we have active focus. Walks up the drawable tree; use sparingly.
        /// </summary>
        public bool HasFocus => ourInputManager?.FocusedDrawable == this;

        /// <summary>
        /// If true, we are eagerly requesting focus. If nothing else above us has (or is requesting focus) we will get it.
        /// </summary>
        public virtual bool RequestingFocus => false;

        /// <summary>
        /// Whether this Drawable is currently hovered over.
        /// </summary>
        public bool Hovering { get; internal set; }

        /// <summary>
        /// Computes whether a given screen-space position is contained within this drawable.
        /// Mouse input events are only received when this function is true, or when the drawable
        /// is in focus.
        /// </summary>
        /// <param name="screenSpacePos">The screen space position to be checked against this drawable.</param>
        public virtual bool Contains(Vector2 screenSpacePos) => DrawRectangle.Contains(ToLocalSpace(screenSpacePos));

        /// <summary>
        /// Whether this Drawable can receive, taking into account all optimizations and masking.
        /// </summary>
        public bool CanReceiveInput => HandleInput && IsPresent && !IsMaskedAway;

        /// <summary>
        /// Whether this Drawable is hovered by the given screen space mouse position,
        /// taking into account whether this Drawable can receive input.
        /// </summary>
        /// <param name="screenSpaceMousePos">The mouse position to be checked.</param>
        public bool IsHovered(Vector2 screenSpaceMousePos) => CanReceiveInput && Contains(screenSpaceMousePos);

        /// <summary>
        /// Transforms a screen-space input state to the parent's space of this Drawable.
        /// </summary>
        /// <param name="screenSpaceState">The screen-space input state to be transformed.</param>
        /// <returns>The transformed state in parent space.</returns>
        private InputState toParentSpace(InputState screenSpaceState)
        {
            if (screenSpaceState == null) return null;

            return new InputState
            {
                Keyboard = screenSpaceState.Keyboard,
                Mouse = new LocalMouseState(screenSpaceState.Mouse, this)
            };
        }

        /// <summary>
        /// This method is responsible for building a queue of Drawables to receive keyboard input
        /// in-order. This method is overridden by <see cref="T:Container"/> to be called on all
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
        /// in-order. This method is overridden by <see cref="T:Container"/> to be called on all
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

            private readonly Drawable us;

            public LocalMouseState(IMouseState state, Drawable us)
            {
                NativeState = state;
                this.us = us;
            }

            public bool BackButton => NativeState.BackButton;
            public bool ForwardButton => NativeState.ForwardButton;

            public Vector2 Delta => Position - LastPosition;

            public Vector2 Position => us.Parent?.ToLocalSpace(NativeState.Position) ?? NativeState.Position;

            public Vector2 LastPosition => us.Parent?.ToLocalSpace(NativeState.LastPosition) ?? NativeState.LastPosition;

            public Vector2? PositionMouseDown => NativeState.PositionMouseDown == null ? null : us.Parent?.ToLocalSpace(NativeState.PositionMouseDown.Value) ?? NativeState.PositionMouseDown;
            public bool HasMainButtonPressed => NativeState.HasMainButtonPressed;
            public bool LeftButton => NativeState.LeftButton;
            public bool MiddleButton => NativeState.MiddleButton;
            public bool RightButton => NativeState.RightButton;
            public int Wheel => NativeState.Wheel;
            public int WheelDelta => NativeState.WheelDelta;
        }

        #endregion

        #region Transforms

        private double transformDelay;

        public virtual void ClearTransforms(bool propagateChildren = false)
        {
            DelayReset();
            transforms?.Clear();
        }

        public virtual Drawable Delay(double duration, bool propagateChildren = false)
        {
            if (duration == 0) return this;

            transformDelay += duration;
            return this;
        }

        public ScheduledDelegate Schedule(Action action) => Scheduler.AddDelayed(action, transformDelay);

        /// <summary>
        /// Flush specified transforms, using the last available values (ignoring current clock time).
        /// </summary>
        /// <param name="propagateChildren">Whether we also flush down the child tree.</param>
        /// <param name="flushType">An optional type of transform to flush. Null for all types.</param>
        public virtual void Flush(bool propagateChildren = false, Type flushType = null)
        {
            var operateTransforms = flushType == null ? Transforms : Transforms.FindAll(t => t.GetType() == flushType);

            double maxTime = double.MinValue;
            foreach (ITransform t in operateTransforms)
                if (t.EndTime > maxTime)
                    maxTime = t.EndTime;

            FrameTimeInfo maxTimeInfo = new FrameTimeInfo { Current = maxTime };

            foreach (ITransform t in operateTransforms)
            {
                t.UpdateTime(maxTimeInfo);
                t.Apply(this);
            }

            if (flushType == null)
                ClearTransforms();
            else
                Transforms.RemoveAll(t => t.GetType() == flushType);
        }

        public virtual Drawable DelayReset()
        {
            Delay(-transformDelay);
            return this;
        }

        public void Loop(float delay = 0)
        {
            foreach (var t in Transforms)
                t.Loop(Math.Max(0, transformDelay + delay - t.Duration));
        }

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
            double max = Time.Current + transformDelay;
            foreach (ITransform t in Transforms)
                if (t.EndTime > max) max = t.EndTime + 1; //adding 1ms here ensures we can expire on the current frame without issue.
            LifetimeEnd = max;

            if (calculateLifetimeStart)
            {
                double min = double.MaxValue;
                foreach (ITransform t in Transforms)
                    if (t.StartTime < min) min = t.StartTime;
                LifetimeStart = min < int.MaxValue ? min : int.MinValue;
            }
        }

        public void TimeWarp(double change)
        {
            if (change == 0)
                return;

            foreach (ITransform t in Transforms)
            {
                t.StartTime += change;
                t.EndTime += change;
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

        public void FadeIn(double duration = 0, EasingTypes easing = EasingTypes.None)
        {
            FadeTo(1, duration, easing);
        }

        public void FadeInFromZero(double duration)
        {
            if (transformDelay == 0)
            {
                Alpha = 0;
                Transforms.RemoveAll(t => t is TransformAlpha);
            }

            double startTime = Time.Current + transformDelay;

            Transforms.Add(new TransformAlpha
            {
                StartTime = startTime,
                EndTime = startTime + duration,
                StartValue = 0,
                EndValue = 1,
            });
        }

        public void FadeOut(double duration = 0, EasingTypes easing = EasingTypes.None)
        {
            FadeTo(0, duration, easing);
        }

        public void FadeOutFromOne(double duration)
        {
            if (transformDelay == 0)
            {
                Alpha = 1;
                Transforms.RemoveAll(t => t is TransformAlpha);
            }

            double startTime = Time.Current + transformDelay;

            TransformAlpha tr = new TransformAlpha
            {
                StartTime = startTime,
                EndTime = startTime + duration,
                StartValue = 1,
                EndValue = 0,
            };

            Transforms.Add(tr);
        }

        #region Float-based helpers

        protected void TransformFloatTo(float startValue, float newValue, double duration, EasingTypes easing, TransformFloat transform)
        {
            Type type = transform.GetType();
            if (transformDelay == 0)
            {
                Transforms.RemoveAll(t => t.GetType() == type);
                if (startValue == newValue)
                    return;
            }
            else
                startValue = (Transforms.FindLast(t => t.GetType() == type) as TransformFloat)?.EndValue ?? startValue;

            double startTime = Clock != null ? Time.Current + transformDelay : 0;

            transform.StartTime = startTime;
            transform.EndTime = startTime + duration;
            transform.StartValue = startValue;
            transform.EndValue = newValue;
            transform.Easing = easing;

            addTransform(transform);
        }

        public void FadeTo(float newAlpha, double duration = 0, EasingTypes easing = EasingTypes.None)
        {
            UpdateTransformsOfType(typeof(TransformAlpha));
            TransformFloatTo(Alpha, newAlpha, duration, easing, new TransformAlpha());
        }

        public void RotateTo(float newRotation, double duration = 0, EasingTypes easing = EasingTypes.None)
        {
            UpdateTransformsOfType(typeof(TransformRotation));
            TransformFloatTo(Rotation, newRotation, duration, easing, new TransformRotation());
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
            UpdateTransformsOfType(typeof(TransformPositionX));
            TransformFloatTo(Position.X, destination, duration, easing, new TransformPositionX());
        }

        public void MoveToY(float destination, double duration = 0, EasingTypes easing = EasingTypes.None)
        {
            UpdateTransformsOfType(typeof(TransformPositionY));
            TransformFloatTo(Position.Y, destination, duration, easing, new TransformPositionY());
        }

        #endregion

        #region Vector2-based helpers

        protected void TransformVectorTo(Vector2 startValue, Vector2 newValue, double duration, EasingTypes easing, TransformVector transform)
        {
            Type type = transform.GetType();
            if (transformDelay == 0)
            {
                Transforms.RemoveAll(t => t.GetType() == type);

                if (startValue == newValue)
                    return;
            }
            else
                startValue = (Transforms.FindLast(t => t.GetType() == type) as TransformVector)?.EndValue ?? startValue;

            double startTime = Clock != null ? Time.Current + transformDelay : 0;

            transform.StartTime = startTime;
            transform.EndTime = startTime + duration;
            transform.StartValue = startValue;
            transform.EndValue = newValue;
            transform.Easing = easing;

            addTransform(transform);
        }

        public void ScaleTo(float newScale, double duration = 0, EasingTypes easing = EasingTypes.None)
        {
            UpdateTransformsOfType(typeof(TransformScale));
            TransformVectorTo(Scale, new Vector2(newScale), duration, easing, new TransformScale());
        }

        public void ScaleTo(Vector2 newScale, double duration = 0, EasingTypes easing = EasingTypes.None)
        {
            UpdateTransformsOfType(typeof(TransformScale));
            TransformVectorTo(Scale, newScale, duration, easing, new TransformScale());
        }

        public void ResizeTo(float newSize, double duration = 0, EasingTypes easing = EasingTypes.None)
        {
            UpdateTransformsOfType(typeof(TransformSize));
            TransformVectorTo(Size, new Vector2(newSize), duration, easing, new TransformSize());
        }

        public void ResizeTo(Vector2 newSize, double duration = 0, EasingTypes easing = EasingTypes.None)
        {
            UpdateTransformsOfType(typeof(TransformSize));
            TransformVectorTo(Size, newSize, duration, easing, new TransformSize());
        }

        public void MoveTo(Vector2 newPosition, double duration = 0, EasingTypes easing = EasingTypes.None)
        {
            UpdateTransformsOfType(typeof(TransformPosition));
            TransformVectorTo(Position, newPosition, duration, easing, new TransformPosition());
        }

        public void MoveToOffset(Vector2 offset, int duration = 0, EasingTypes easing = EasingTypes.None)
        {
            UpdateTransformsOfType(typeof(TransformPosition));
            MoveTo((Transforms.FindLast(t => t is TransformPosition) as TransformPosition)?.EndValue ?? Position + offset, duration, easing);
        }

        #endregion

        #region Color4-based helpers

        public void FadeColour(SRGBColour newColour, int duration = 0, EasingTypes easing = EasingTypes.None)
        {
            UpdateTransformsOfType(typeof(TransformColour));

            Color4 startValue = Colour.Linear;
            if (transformDelay == 0)
            {
                Transforms.RemoveAll(t => t is TransformColour);

                if (startValue == newColour)
                    return;
            }
            else
                startValue = (Transforms.FindLast(t => t is TransformColour) as TransformColour)?.EndValue ?? startValue;

            double startTime = Clock != null ? Time.Current + transformDelay : 0;

            addTransform(new TransformColour
            {
                StartTime = startTime,
                EndTime = startTime + duration,
                StartValue = startValue,
                EndValue = newColour.Linear,
                Easing = easing
            });
        }

        public void FlashColour(SRGBColour flashColour, int duration, EasingTypes easing = EasingTypes.None)
        {
            if (transformDelay != 0)
                throw new NotImplementedException("FlashColour doesn't support Delay() currently");

            Color4 startValue = (Transforms.FindLast(t => t is TransformColour) as TransformColour)?.EndValue ?? Colour.Linear;
            Transforms.RemoveAll(t => t is TransformColour);

            double startTime = Clock != null ? Time.Current + transformDelay : 0;

            addTransform(new TransformColour
            {
                StartTime = startTime,
                EndTime = startTime + duration,
                StartValue = flashColour.Linear,
                EndValue = startValue,
                Easing = easing
            });
        }

        private void addTransform(ITransform transform)
        {
            if (Clock == null)
            {
                transform.UpdateTime(new FrameTimeInfo { Current = transform.EndTime });
                transform.Apply(this);
                return;
            }

            //we have no duration and do not need to be delayed, so we can just apply ourselves and be gone.
            bool canApplyInstant = transform.Duration == 0 && transformDelay == 0;

            //we should also immediately apply any transforms that have already started to avoid potentially applying them one frame too late.
            if (canApplyInstant || transform.StartTime < Time.Current)
            {
                transform.UpdateTime(Time);
                transform.Apply(this);
                if (canApplyInstant)
                    return;
            }

            Transforms.Add(transform);
        }

        #endregion

        #endregion

        /// <summary>
        /// A name used to identify this Drawable internally.
        /// </summary>
        public virtual string Name => string.Empty;

        public override string ToString()
        {
            string shortClass = base.ToString();
            shortClass = shortClass.Substring(shortClass.LastIndexOf('.') + 1);

            if (!string.IsNullOrEmpty(Name))
                shortClass = $@"{Name} ({shortClass})";

            return $@"{shortClass} ({DrawPosition.X:#,0},{DrawPosition.Y:#,0}) @ {DrawSize.X:#,0}x{DrawSize.Y:#,0}";
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
        Colour = 1 << 2,
        DrawNode = 1 << 3,

        // Meta
        None = 0,
        Geometry = Position | SizeInParentSpace,
        All = DrawNode | Geometry | Colour,
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
        Inherit = 0,
        Mixture,
        Additive,
        None,
    }

    public enum LoadState
    {
        NotLoaded,
        Loading,
        Loaded,
        Alive
    }
}
