// Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
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
using osu.Framework.Extensions;
using osu.Framework.Logging;
using osu.Framework.Statistics;
using osu.Framework.Graphics.Colour;

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
    public abstract partial class Drawable : IDisposable, IHasLifetime, IDrawable
    {
        #region Construction and disposal

        public Drawable()
        {
            CreationID = creationCounter.Increment();
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

        protected internal virtual bool DisposeOnRemove => false;

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

        #endregion

        #region Sorting (CreationID / Depth)

        /// <summary>
        /// Captures the order in which Drawables were created. Each Drawable
        /// is assigned a unique, monotonically increasing ID upon creation in a thread-safe manner.
        /// The primary use case of this ID is stable sorting of Drawables with equal
        /// <see cref="Depth"/>.
        /// </summary>
        internal long CreationID { get; private set; }
        private static AtomicCounter creationCounter = new AtomicCounter();

        private float depth;

        /// <summary>
        /// Controls which Drawables are behind or in front of other Drawables.
        /// This amounts to sorting Drawables by their <see cref="Depth"/>.
        /// </summary>
        public float Depth
        {
            get { return depth; }
            set
            {
                // TODO: Consider automatically resorting the parents children instead of simply forbidding this.
                Debug.Assert(Parent == null, "May not change depth while inside a parent container.");
                depth = value;
            }
        }

        protected virtual IComparer<Drawable> DepthComparer => new DepthComparer();

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
        protected internal virtual bool UpdateSubTree()
        {
            if (Parent != null) //we don't want to update our clock if we are at the top of the stack. it's handled elsewhere for us.
                customClock?.ProcessFrame();

            if (LoadState < LoadState.Alive)
                if (!loadComplete()) return false;

            transformationDelay = 0;

            //todo: this should be moved to after the IsVisible condition once we have TOL for transformations (and some better logic).
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
        /// <see cref="OnUpdate"/> when deriving from <see cref="Drawable"/>.
        /// </summary>
        protected virtual void Update()
        {
        }

        #endregion

        #region Position / Size (with margin)

        private Vector2 position;

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

        private Axes relativePositionAxes;

        /// <summary>
        /// Controls which <see cref="Axes"/> of <see cref="Position"/> are relative w.r.t.
        /// <see cref="Parent"/>'s size (from 0 to 1) rather than absolute.
        /// </summary>
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
        /// </summary>
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
                Vector2 parent = Parent?.ChildSize ?? Vector2.One;
                if ((relativeAxes & Axes.X) > 0)
                    v.X *= parent.X;
                if ((relativeAxes & Axes.Y) > 0)
                    v.Y *= parent.Y;
            }
            return v;
        }

        private Axes bypassAutoSizeAxes;

        /// <summary>
        /// Controls which <see cref="Axes"/> are ignored by parent <see cref="Parent"/>'s auto size containers.
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

                Debug.Assert(value != 0, "Cannot set anchor to 0.");
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
        public IFrameBasedClock Clock
        {
            get { return clock; }
            set
            {
                customClock = value;
                UpdateClock(customClock);
            }
        }

        internal virtual void UpdateClock(IFrameBasedClock clock)
        {
            this.clock = customClock ?? clock;
        }

        public FrameTimeInfo Time => Clock.TimeInfo;

        /// <summary>
        /// The time at which this drawable becomes valid (and is considered for drawing).
        /// </summary>
        public double LifetimeStart { get; set; } = double.MinValue;

        /// <summary>
        /// The time at which this drawable is no longer valid (and is considered for disposal).
        /// </summary>
        public double LifetimeEnd { get; set; } = double.MaxValue;

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
        public IContainer Parent
        {
            get { return parent; }
            set
            {
                if (parent == value) return;

                parent = value;
                if (parent != null)
                    UpdateClock(parent.Clock);
            }
        }

        internal void ChangeParent(IContainer parent)
        {
            if (Parent == parent) return;

            Debug.Assert(Parent == null, "May not add a drawable to multiple containers.");
            Parent = parent;
        }

        private bool isProxied;

        /// <summary>
        /// Creates a proxy drawable which can be inserted elsewhere in the draw hierarchy.
        /// Will cause the original instance to not render itself.
        /// </summary>
        public ProxyDrawable CreateProxy()
        {
            isProxied = true;
            return new ProxyDrawable(this);
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
            IDrawable currentParent = Parent;
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
            IContainer currentParent = child.Parent;
            while (currentParent != null)
            {
                if (currentParent == this)
                    return true;
                currentParent = currentParent.Parent;
            }

            return false;
        }

        #endregion

        #region Caching & invalidation (for things too expensive to compute every frame)

        /// <summary>
        /// Was this Drawable masked away completely during the last frame?
        /// This is measured conservatively, i.e. it is only true when the Drawable was
        /// actually masked away, but it may be false, even if the Drawable was masked away.
        /// </summary>
        public bool IsMaskedAway = false;

        private Cached<Quad> screenSpaceDrawQuadBacking = new Cached<Quad>();

        protected virtual Quad ComputeScreenSpaceDrawQuad() => ToScreenSpace(DrawRectangle);

        public virtual Quad ScreenSpaceDrawQuad => screenSpaceDrawQuadBacking.EnsureValid()
            ? screenSpaceDrawQuadBacking.Value
            : screenSpaceDrawQuadBacking.Refresh(ComputeScreenSpaceDrawQuad);


        private Cached<DrawInfo> drawInfoBacking = new Cached<DrawInfo>();

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

        /// <summary>
        /// Computes the bounding box of this drawable in its parent's space.
        /// </summary>
        public virtual RectangleF BoundingBox => ToParentSpace(LayoutRectangle).AABBFloat;

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
                    alreadyInvalidated &= !boundingSizeBacking.Invalidate();

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
        protected internal virtual DrawNode GenerateDrawNodeSubtree(int treeIndex, RectangleF bounds)
        {
            if (isProxied) return null;

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

            return (input * DrawInfo.Matrix) * other.DrawInfo.MatrixInverse;
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

        #region Loading

        /// <summary>
        /// Override to add delayed load abilities (ie. using IsAlive)
        /// </summary>
        public virtual bool IsLoaded => LoadState >= LoadState.Loaded;

        public volatile LoadState LoadState;

        public Task Preload(BaseGame game, Action<Drawable> onLoaded = null)
        {
            if (LoadState == LoadState.NotLoaded)
                return Task.Run(() => PerformLoad(game)).ContinueWith(task => game.Schedule(() =>
                {
                    task.ThrowIfFaulted();
                    onLoaded?.Invoke(this);
                }));

            Debug.Assert(LoadState >= LoadState.Loaded, "Preload got called twice on the same Drawable.");
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
            game.Dependencies.Initialize(this);
            double elapsed = perf.CurrentTime - t1;
            if (perf.CurrentTime > 1000 && elapsed > 50 && ThreadSafety.IsUpdateThread)
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

            LifetimeStart = Time.Current;
            Invalidate();
            LoadState = LoadState.Alive;
            LoadComplete();
            return true;
        }

        /// <summary>
        /// Play initial animation etc.
        /// </summary>
        protected virtual void LoadComplete() { }

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

        Both = X | Y
    }

    public enum BlendingMode
    {
        Inherit = 0,
        Mixture,
        Additive,
        None,
    }

    public class DepthComparer : IComparer<Drawable>
    {
        public int Compare(Drawable x, Drawable y)
        {
            int i = y.Depth.CompareTo(x.Depth);
            if (i != 0) return i;
            return x.CreationID.CompareTo(y.CreationID);
        }
    }

    public class ReverseCreationOrderDepthComparer : IComparer<Drawable>
    {
        public int Compare(Drawable x, Drawable y)
        {
            int i = y.Depth.CompareTo(x.Depth);
            if (i != 0) return i;
            return y.CreationID.CompareTo(x.CreationID);
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
