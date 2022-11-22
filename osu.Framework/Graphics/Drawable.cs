// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using osuTK;
using osuTK.Graphics;
using osu.Framework.Allocation;
using osu.Framework.Extensions.TypeExtensions;
using osu.Framework.Graphics.Colour;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Effects;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Transforms;
using osu.Framework.Input;
using osu.Framework.Logging;
using osu.Framework.Statistics;
using osu.Framework.Threading;
using osu.Framework.Timing;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;
using JetBrains.Annotations;
using osu.Framework.Bindables;
using osu.Framework.Development;
using osu.Framework.Extensions.EnumExtensions;
using osu.Framework.Graphics.Cursor;
using osu.Framework.Graphics.Rendering;
using osu.Framework.Input.Bindings;
using osu.Framework.Input.Events;
using osu.Framework.Input.States;
using osu.Framework.Layout;
using osu.Framework.Testing;
using osu.Framework.Utils;
using osuTK.Input;
using Container = osu.Framework.Graphics.Containers.Container;

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
    [ExcludeFromDynamicCompile]
    public abstract partial class Drawable : Transformable, IDisposable, IDrawable
    {
        #region Construction and disposal

        [Resolved(CanBeNull = true)]
        private IRenderer renderer { get; set; }

        protected Drawable()
        {
            total_count.Value++;

            AddLayout(drawInfoBacking);
            AddLayout(drawSizeBacking);
            AddLayout(screenSpaceDrawQuadBacking);
            AddLayout(drawColourInfoBacking);
            AddLayout(requiredParentSizeToFitBacking);
        }

        private static readonly GlobalStatistic<int> total_count = GlobalStatistics.Get<int>(nameof(Drawable), "Total constructed");

        internal bool IsLongRunning => GetType().GetCustomAttribute<LongRunningLoadAttribute>() != null;

        /// <summary>
        /// Disposes this drawable.
        /// </summary>
        public void Dispose()
        {
            //we can't dispose if we are mid-load, else our children may get in a bad state.
            lock (LoadLock) Dispose(true);

            GC.SuppressFinalize(this);
        }

        protected internal bool IsDisposed { get; private set; }

        /// <summary>
        /// Disposes this drawable.
        /// </summary>
        protected virtual void Dispose(bool isDisposing)
        {
            if (IsDisposed)
                return;

            UnbindAllBindables();

            // Bypass expensive operations as a result of setting the Parent property, by setting the field directly.
            parent = null;
            ChildID = 0;

            OnUpdate = null;
            Invalidated = null;

            OnDispose?.Invoke();
            OnDispose = null;

            renderer?.ScheduleDisposal(d =>
            {
                for (int i = 0; i < d.drawNodes.Length; i++)
                    d.drawNodes[i]?.Dispose();
            }, this);

            IsDisposed = true;
        }

        /// <summary>
        /// Whether this Drawable should be disposed when it is automatically removed from
        /// its <see cref="Parent"/> due to <see cref="ShouldBeAlive"/> being false.
        /// </summary>
        public virtual bool DisposeOnDeathRemoval => RemoveCompletedTransforms;

        private static readonly ConcurrentDictionary<Type, Action<object>> unbind_action_cache = new ConcurrentDictionary<Type, Action<object>>();

        /// <summary>
        /// Recursively invokes <see cref="UnbindAllBindables"/> on this <see cref="Drawable"/> and all <see cref="Drawable"/>s further down the scene graph.
        /// </summary>
        internal virtual void UnbindAllBindablesSubTree() => UnbindAllBindables();

        private Action<object> getUnbindAction()
        {
            Type ourType = GetType();
            return unbind_action_cache.TryGetValue(ourType, out var action) ? action : cacheUnbindAction(ourType);

            // Extracted to a separate method to prevent .NET from pre-allocating some objects (saves ~150B per call to this method, even if already cached).
            static Action<object> cacheUnbindAction(Type ourType)
            {
                List<Action<object>> actions = new List<Action<object>>();

                foreach (var type in ourType.EnumerateBaseTypes())
                {
                    // Generate delegates to unbind fields
                    actions.AddRange(type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                                         .Where(f => typeof(IUnbindable).IsAssignableFrom(f.FieldType))
                                         .Select(f => new Action<object>(target => ((IUnbindable)f.GetValue(target))?.UnbindAll())));
                }

                // Delegates to unbind properties are intentionally not generated.
                // Properties with backing fields (including automatic properties) will be picked up by the field unbind delegate generation,
                // while ones without backing fields (like get-only properties that delegate to another drawable's bindable) should not be unbound here.

                return unbind_action_cache[ourType] = target =>
                {
                    foreach (var a in actions)
                    {
                        try
                        {
                            a(target);
                        }
                        catch (Exception e)
                        {
                            Logger.Error(e, $"Failed to unbind a local bindable in {ourType.ReadableName()}");
                        }
                    }
                };
            }
        }

        private bool unbindComplete;

        /// <summary>
        /// Unbinds all <see cref="Bindable{T}"/>s stored as fields or properties in this <see cref="Drawable"/>.
        /// </summary>
        internal virtual void UnbindAllBindables()
        {
            if (unbindComplete)
                return;

            unbindComplete = true;

            getUnbindAction().Invoke(this);

            OnUnbindAllBindables?.Invoke();
        }

        #endregion

        #region Loading

        /// <summary>
        /// Whether this Drawable is fully loaded.
        /// This is true iff <see cref="UpdateSubTree"/> has run once on this <see cref="Drawable"/>.
        /// </summary>
        public bool IsLoaded => loadState >= LoadState.Loaded;

        private volatile LoadState loadState;

        /// <summary>
        /// Describes the current state of this Drawable within the loading pipeline.
        /// </summary>
        public LoadState LoadState => loadState;

        /// <summary>
        /// The thread on which the <see cref="Load"/> operation started, or null if <see cref="Drawable"/> has not started loading.
        /// </summary>
        internal Thread LoadThread { get; private set; }

        internal readonly object LoadLock = new object();

        private static readonly StopwatchClock perf_clock = new StopwatchClock(true);

        /// <summary>
        /// Load this drawable from an async context.
        /// Because we can't be sure of the disposal state, it is returned as a bool rather than thrown as in <see cref="Load"/>.
        /// </summary>
        /// <param name="clock">The clock we should use by default.</param>
        /// <param name="dependencies">The dependency tree we will inherit by default. May be extended via <see cref="CompositeDrawable.CreateChildDependencies"/></param>
        /// <param name="isDirectAsyncContext">Whether this call is being executed from a directly async context (not a parent).</param>
        /// <returns>Whether the load was successful.</returns>
        internal bool LoadFromAsync(IFrameBasedClock clock, IReadOnlyDependencyContainer dependencies, bool isDirectAsyncContext = false)
        {
            lock (LoadLock)
            {
                if (IsDisposed)
                    return false;

                Load(clock, dependencies, isDirectAsyncContext);
                return true;
            }
        }

        /// <summary>
        /// Loads this drawable, including the gathering of dependencies and initialisation of required resources.
        /// </summary>
        /// <param name="clock">The clock we should use by default.</param>
        /// <param name="dependencies">The dependency tree we will inherit by default. May be extended via <see cref="CompositeDrawable.CreateChildDependencies"/></param>
        /// <param name="isDirectAsyncContext">Whether this call is being executed from a directly async context (not a parent).</param>
        internal void Load(IFrameBasedClock clock, IReadOnlyDependencyContainer dependencies, bool isDirectAsyncContext = false)
        {
            lock (LoadLock)
            {
                if (!isDirectAsyncContext && IsLongRunning)
                    throw new InvalidOperationException("Tried to load a long-running drawable in a non-direct async context. See https://git.io/Je1YF for more details.");

                if (IsDisposed)
                    throw new ObjectDisposedException(ToString(), "Attempting to load an already disposed drawable.");

                if (loadState == LoadState.NotLoaded)
                {
                    Trace.Assert(loadState == LoadState.NotLoaded);

                    loadState = LoadState.Loading;

                    load(clock, dependencies);

                    loadState = LoadState.Ready;
                }
            }
        }

        private void load(IFrameBasedClock clock, IReadOnlyDependencyContainer dependencies)
        {
            LoadThread = Thread.CurrentThread;

            // Cache eagerly during load to hopefully defer the reflection overhead to an async pathway.
            getUnbindAction();

            UpdateClock(clock);

            double timeBefore = DebugUtils.LogPerformanceIssues ? perf_clock.CurrentTime : 0;

            RequestsNonPositionalInput = HandleInputCache.RequestsNonPositionalInput(this);
            RequestsPositionalInput = HandleInputCache.RequestsPositionalInput(this);

            RequestsNonPositionalInputSubTree = RequestsNonPositionalInput;
            RequestsPositionalInputSubTree = RequestsPositionalInput;

            InjectDependencies(dependencies);

            LoadAsyncComplete();

            if (timeBefore > 1000)
            {
                double loadDuration = perf_clock.CurrentTime - timeBefore;

                bool blocking = ThreadSafety.IsUpdateThread;

                double allowedDuration = blocking ? 16 : 100;

                if (loadDuration > allowedDuration)
                {
                    Logger.Log($@"{ToString()} took {loadDuration:0.00}ms to load" + (blocking ? " (and blocked the update thread)" : " (async)"), LoggingTarget.Performance,
                        blocking ? LogLevel.Important : LogLevel.Verbose);
                }
            }
        }

        /// <summary>
        /// Injects dependencies from an <see cref="IReadOnlyDependencyContainer"/> into this <see cref="Drawable"/>.
        /// </summary>
        /// <param name="dependencies">The dependencies to inject.</param>
        protected virtual void InjectDependencies(IReadOnlyDependencyContainer dependencies) => dependencies.Inject(this);

        /// <summary>
        /// Runs once on the update thread after loading has finished.
        /// </summary>
        private bool loadComplete()
        {
            if (loadState < LoadState.Ready) return false;

            loadState = LoadState.Loaded;

            // From a synchronous point of view, this is the first time the Drawable receives a parent.
            // If this Drawable calculated properties such as DrawInfo that depend on the parent state before this point, they must be re-validated in the now-correct state.
            // A "parent" source is faked since Child+Self states are always assumed valid if they only access local Drawable states (e.g. Colour but not DrawInfo).
            // Only layout flags are required, as non-layout flags are always propagated by the parent.
            Invalidate(Invalidation.Layout, InvalidationSource.Parent);

            LoadComplete();

            OnLoadComplete?.Invoke(this);
            OnLoadComplete = null;
            return true;
        }

        /// <summary>
        /// Invoked after dependency injection has completed for this <see cref="Drawable"/> and all
        /// children if this is a <see cref="CompositeDrawable"/>.
        /// </summary>
        /// <remarks>
        /// This method is invoked in the potentially asynchronous context of <see cref="Load"/> prior to
        /// this <see cref="Drawable"/> becoming <see cref="IsLoaded"/> = true.
        /// </remarks>
        protected virtual void LoadAsyncComplete()
        {
        }

        /// <summary>
        /// Invoked after this <see cref="Drawable"/> has finished loading.
        /// </summary>
        /// <remarks>
        /// This method is invoked on the update thread inside this <see cref="Drawable"/>'s <see cref="UpdateSubTree"/>.
        /// </remarks>
        protected virtual void LoadComplete()
        {
        }

        #endregion

        #region Sorting (CreationID / Depth)

        /// <summary>
        /// Captures the order in which Drawables were added to a <see cref="CompositeDrawable"/>. Each Drawable
        /// is assigned a monotonically increasing ID upon being added to a <see cref="CompositeDrawable"/>. This
        /// ID is unique within the <see cref="Parent"/> <see cref="CompositeDrawable"/>.
        /// The primary use case of this ID is stable sorting of Drawables with equal <see cref="Depth"/>.
        /// </summary>
        internal ulong ChildID { get; set; }

        /// <summary>
        /// Whether this drawable has been added to a parent <see cref="CompositeDrawable"/>. Note that this does NOT imply that
        /// <see cref="Parent"/> has been set.
        /// This is primarily used to block properties such as <see cref="Depth"/> that strictly rely on the value of <see cref="Parent"/>
        /// to alert the user of an invalid operation.
        /// </summary>
        internal bool IsPartOfComposite => ChildID != 0;

        /// <summary>
        /// Whether this drawable is part of its parent's <see cref="CompositeDrawable.AliveInternalChildren"/>.
        /// </summary>
        public bool IsAlive { get; internal set; }

        private float depth;

        /// <summary>
        /// Controls which Drawables are behind or in front of other Drawables.
        /// This amounts to sorting Drawables by their <see cref="Depth"/>.
        /// A Drawable with higher <see cref="Depth"/> than another Drawable is
        /// drawn behind the other Drawable.
        /// </summary>
        public float Depth
        {
            get => depth;
            set
            {
                if (IsPartOfComposite)
                {
                    throw new InvalidOperationException(
                        $"May not change {nameof(Depth)} while inside a parent {nameof(CompositeDrawable)}." +
                        $"Use the parent's {nameof(CompositeDrawable.ChangeInternalChildDepth)} or {nameof(Container.ChangeChildDepth)} instead.");
                }

                depth = value;
            }
        }

        #endregion

        #region Periodic tasks (events, Scheduler, Transforms, Update)

        /// <summary>
        /// This event is fired after the <see cref="Update"/> method is called at the end of
        /// <see cref="UpdateSubTree"/>. It should be used when a simple action should be performed
        /// at the end of every update call which does not warrant overriding the Drawable.
        /// </summary>
        public event Action<Drawable> OnUpdate;

        /// <summary>
        /// This event is fired after the <see cref="LoadComplete"/> method is called.
        /// It should be used when a simple action should be performed
        /// when the Drawable is loaded which does not warrant overriding the Drawable.
        /// This event is automatically cleared after being invoked.
        /// </summary>
        public event Action<Drawable> OnLoadComplete;

        /// <summary>.
        /// Fired after the <see cref="Invalidate"/> method is called.
        /// </summary>
        internal event Action<Drawable> Invalidated;

        /// <summary>
        /// Fired after the <see cref="Dispose(bool)"/> method is called.
        /// </summary>
        internal event Action OnDispose;

        /// <summary>
        /// Fired after the <see cref="UnbindAllBindables"/> method is called.
        /// </summary>
        internal event Action OnUnbindAllBindables;

        /// <summary>
        /// A lock exclusively used for initial acquisition/construction of the <see cref="Scheduler"/>.
        /// </summary>
        private static readonly object scheduler_acquisition_lock = new object();

        private volatile Scheduler scheduler;

        /// <summary>
        /// A lazily-initialized scheduler used to schedule tasks to be invoked in future <see cref="Update"/>s calls.
        /// The tasks are invoked at the beginning of the <see cref="Update"/> method before anything else.
        /// </summary>
        protected internal Scheduler Scheduler
        {
            get
            {
                if (scheduler != null)
                    return scheduler;

                lock (scheduler_acquisition_lock)
                {
                    if (scheduler != null)
                        return scheduler;

                    scheduler = new Scheduler(() => ThreadSafety.IsUpdateThread, Clock);
                }

                return scheduler;
            }
        }

        /// <summary>
        /// Updates this Drawable and all Drawables further down the scene graph.
        /// Called once every frame.
        /// </summary>
        /// <returns>False if the drawable should not be updated.</returns>
        public virtual bool UpdateSubTree()
        {
            if (IsDisposed)
                throw new ObjectDisposedException(ToString(), "Disposed Drawables may never be in the scene graph.");

            if (ProcessCustomClock)
                customClock?.ProcessFrame();

            if (loadState < LoadState.Ready)
                return false;

            if (loadState == LoadState.Ready)
                loadComplete();

            Debug.Assert(loadState == LoadState.Loaded);

            UpdateTransforms();

            if (!IsPresent)
                return true;

            if (scheduler != null)
            {
                int amountScheduledTasks = scheduler.Update();
                FrameStatistics.Add(StatisticsCounterType.ScheduleInvk, amountScheduledTasks);
            }

            Update();
            OnUpdate?.Invoke(this);
            return true;
        }

        /// <summary>
        /// Updates all masking calculations for this <see cref="Drawable"/>.
        /// This occurs post-<see cref="UpdateSubTree"/> to ensure that all <see cref="Drawable"/> updates have taken place.
        /// </summary>
        /// <param name="source">The parent that triggered this update on this <see cref="Drawable"/>.</param>
        /// <param name="maskingBounds">The <see cref="RectangleF"/> that defines the masking bounds.</param>
        /// <returns>Whether masking calculations have taken place.</returns>
        public virtual bool UpdateSubTreeMasking(Drawable source, RectangleF maskingBounds)
        {
            if (!IsPresent)
                return false;

            if (HasProxy && source != proxy)
                return false;

            IsMaskedAway = ComputeIsMaskedAway(maskingBounds);
            return true;
        }

        /// <summary>
        /// Computes whether this <see cref="Drawable"/> is masked away.
        /// </summary>
        /// <param name="maskingBounds">The <see cref="RectangleF"/> that defines the masking bounds.</param>
        /// <returns>Whether this <see cref="Drawable"/> is currently masked away.</returns>
        protected virtual bool ComputeIsMaskedAway(RectangleF maskingBounds) => !Precision.AlmostIntersects(maskingBounds, ScreenSpaceDrawQuad.AABBFloat);

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
            get => new Vector2(x, y);
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
            get => position;
            set
            {
                if (position == value) return;

                if (!Validation.IsFinite(value)) throw new ArgumentException($@"{nameof(Position)} must be finite, but is {value}.");

                Axes changedAxes = Axes.None;

                if (position.X != value.X)
                    changedAxes |= Axes.X;

                if (position.Y != value.Y)
                    changedAxes |= Axes.Y;

                position = value;

                invalidateParentSizeDependencies(Invalidation.MiscGeometry, changedAxes);
            }
        }

        private float x;
        private float y;

        /// <summary>
        /// X component of <see cref="Position"/>.
        /// </summary>
        public float X
        {
            get => x;
            set
            {
                if (x == value) return;

                if (!float.IsFinite(value)) throw new ArgumentException($@"{nameof(X)} must be finite, but is {value}.");

                x = value;

                invalidateParentSizeDependencies(Invalidation.MiscGeometry, Axes.X);
            }
        }

        /// <summary>
        /// Y component of <see cref="Position"/>.
        /// </summary>
        public float Y
        {
            get => y;
            set
            {
                if (y == value) return;

                if (!float.IsFinite(value)) throw new ArgumentException($@"{nameof(Y)} must be finite, but is {value}.");

                y = value;

                invalidateParentSizeDependencies(Invalidation.MiscGeometry, Axes.Y);
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
            get => relativePositionAxes;
            set
            {
                if (value == relativePositionAxes)
                    return;

                // Convert coordinates from relative to absolute or vice versa
                Vector2 conversion = relativeToAbsoluteFactor;
                if ((value & Axes.X) > (relativePositionAxes & Axes.X))
                    X = Precision.AlmostEquals(conversion.X, 0) ? 0 : X / conversion.X;
                else if ((relativePositionAxes & Axes.X) > (value & Axes.X))
                    X *= conversion.X;

                if ((value & Axes.Y) > (relativePositionAxes & Axes.Y))
                    Y = Precision.AlmostEquals(conversion.Y, 0) ? 0 : Y / conversion.Y;
                else if ((relativePositionAxes & Axes.Y) > (value & Axes.Y))
                    Y *= conversion.Y;

                relativePositionAxes = value;

                updateBypassAutoSizeAxes();
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

                    if (!RelativePositionAxes.HasFlagFast(Axes.X))
                        offset.X = 0;

                    if (!RelativePositionAxes.HasFlagFast(Axes.Y))
                        offset.Y = 0;
                }

                return ApplyRelativeAxes(RelativePositionAxes, Position - offset, FillMode.Stretch);
            }
        }

        private Vector2 size
        {
            get => new Vector2(width, height);
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
            get => size;
            set
            {
                if (size == value) return;

                if (!Validation.IsFinite(value)) throw new ArgumentException($@"{nameof(Size)} must be finite, but is {value}.");

                Axes changedAxes = Axes.None;

                if (size.X != value.X)
                    changedAxes |= Axes.X;

                if (size.Y != value.Y)
                    changedAxes |= Axes.Y;

                size = value;

                invalidateParentSizeDependencies(Invalidation.DrawSize, changedAxes);
            }
        }

        private float width;
        private float height;

        /// <summary>
        /// X component of <see cref="Size"/>.
        /// </summary>
        public virtual float Width
        {
            get => width;
            set
            {
                if (width == value) return;

                if (!float.IsFinite(value)) throw new ArgumentException($@"{nameof(Width)} must be finite, but is {value}.");

                width = value;

                invalidateParentSizeDependencies(Invalidation.DrawSize, Axes.X);
            }
        }

        /// <summary>
        /// Y component of <see cref="Size"/>.
        /// </summary>
        public virtual float Height
        {
            get => height;
            set
            {
                if (height == value) return;

                if (!float.IsFinite(value)) throw new ArgumentException($@"{nameof(Height)} must be finite, but is {value}.");

                height = value;

                invalidateParentSizeDependencies(Invalidation.DrawSize, Axes.Y);
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
            get => relativeSizeAxes;
            set
            {
                if (value == relativeSizeAxes)
                    return;

                // In some cases we cannot easily preserve our size, and so we simply invalidate and
                // leave correct sizing to the user.
                if (fillMode != FillMode.Stretch && (value == Axes.Both || relativeSizeAxes == Axes.Both))
                    Invalidate(Invalidation.DrawSize);
                else
                {
                    // Convert coordinates from relative to absolute or vice versa
                    Vector2 conversion = relativeToAbsoluteFactor;
                    if ((value & Axes.X) > (relativeSizeAxes & Axes.X))
                        Width = Precision.AlmostEquals(conversion.X, 0) ? 0 : Width / conversion.X;
                    else if ((relativeSizeAxes & Axes.X) > (value & Axes.X))
                        Width *= conversion.X;

                    if ((value & Axes.Y) > (relativeSizeAxes & Axes.Y))
                        Height = Precision.AlmostEquals(conversion.Y, 0) ? 0 : Height / conversion.Y;
                    else if ((relativeSizeAxes & Axes.Y) > (value & Axes.Y))
                        Height *= conversion.Y;
                }

                relativeSizeAxes = value;

                if (relativeSizeAxes.HasFlagFast(Axes.X) && Width == 0) Width = 1;
                if (relativeSizeAxes.HasFlagFast(Axes.Y) && Height == 0) Height = 1;

                updateBypassAutoSizeAxes();

                OnSizingChanged();
            }
        }

        private readonly LayoutValue<Vector2> drawSizeBacking = new LayoutValue<Vector2>(Invalidation.DrawInfo | Invalidation.RequiredParentSizeToFit | Invalidation.Presence);

        /// <summary>
        /// Absolute size of this Drawable in the <see cref="Parent"/>'s coordinate system.
        /// </summary>
        public Vector2 DrawSize => drawSizeBacking.IsValid ? drawSizeBacking : drawSizeBacking.Value = ApplyRelativeAxes(RelativeSizeAxes, Size, FillMode);

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
            get => margin;
            set
            {
                if (margin.Equals(value)) return;

                if (!Validation.IsFinite(value)) throw new ArgumentException($@"{nameof(Margin)} must be finite, but is {value}.");

                margin = value;

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
        /// <param name="fillMode">The <see cref="FillMode"/> to be used.</param>
        /// <returns>Absolute coordinates in <see cref="Parent"/>'s space.</returns>
        protected Vector2 ApplyRelativeAxes(Axes relativeAxes, Vector2 v, FillMode fillMode)
        {
            if (relativeAxes != Axes.None)
            {
                Vector2 conversion = relativeToAbsoluteFactor;

                if (relativeAxes.HasFlagFast(Axes.X))
                    v.X *= conversion.X;
                if (relativeAxes.HasFlagFast(Axes.Y))
                    v.Y *= conversion.Y;

                // FillMode only makes sense if both axes are relatively sized as the general rule
                // for n-dimensional aspect preservation is to simply take the minimum or the maximum
                // scale among all active axes. For single axes the minimum / maximum is just the
                // value itself.
                if (relativeAxes == Axes.Both && fillMode != FillMode.Stretch)
                {
                    if (fillMode == FillMode.Fill)
                        v = new Vector2(Math.Max(v.X, v.Y * fillAspectRatio));
                    else if (fillMode == FillMode.Fit)
                        v = new Vector2(Math.Min(v.X, v.Y * fillAspectRatio));
                    v.Y /= fillAspectRatio;
                }
            }

            return v;
        }

        /// <summary>
        /// Conversion factor from relative to absolute coordinates in the <see cref="Parent"/>'s space.
        /// </summary>
        private Vector2 relativeToAbsoluteFactor => Parent?.RelativeToAbsoluteFactor ?? Vector2.One;

        private Axes bypassAutoSizeAxes;

        private void updateBypassAutoSizeAxes()
        {
            var value = RelativePositionAxes | RelativeSizeAxes | bypassAutoSizeAdditionalAxes;

            if (bypassAutoSizeAxes != value)
            {
                var changedAxes = bypassAutoSizeAxes ^ value;
                bypassAutoSizeAxes = value;
                if (((Parent?.AutoSizeAxes ?? 0) & changedAxes) != 0)
                    Parent?.Invalidate(Invalidation.RequiredParentSizeToFit, InvalidationSource.Child);
            }
        }

        private Axes bypassAutoSizeAdditionalAxes;

        /// <summary>
        /// Controls which <see cref="Axes"/> are ignored by parent <see cref="Parent"/> automatic sizing.
        /// Most notably, <see cref="RelativePositionAxes"/> and <see cref="RelativeSizeAxes"/> do not affect
        /// automatic sizing to avoid circular size dependencies.
        /// </summary>
        public Axes BypassAutoSizeAxes
        {
            get => bypassAutoSizeAxes;
            set
            {
                bypassAutoSizeAdditionalAxes = value;
                updateBypassAutoSizeAxes();
            }
        }

        /// <summary>
        /// Computes the bounding box of this drawable in its parent's space.
        /// </summary>
        public virtual RectangleF BoundingBox => ToParentSpace(LayoutRectangle).AABBFloat;

        /// <summary>
        /// Called whenever the <see cref="RelativeSizeAxes"/> of this drawable is changed, or when the <see cref="Container{T}.AutoSizeAxes"/> are changed if this drawable is a <see cref="Container{T}"/>.
        /// </summary>
        protected virtual void OnSizingChanged()
        {
        }

        #endregion

        #region Scale / Shear / Rotation

        private Vector2 scale = Vector2.One;

        /// <summary>
        /// Base relative scaling factor around <see cref="OriginPosition"/>.
        /// </summary>
        public Vector2 Scale
        {
            get => scale;
            set
            {
                if (scale == value)
                    return;

                if (!Validation.IsFinite(value)) throw new ArgumentException($@"{nameof(Scale)} must be finite, but is {value}.");

                bool wasPresent = IsPresent;

                scale = value;

                if (IsPresent != wasPresent)
                    Invalidate(Invalidation.MiscGeometry | Invalidation.Presence);
                else
                    Invalidate(Invalidation.MiscGeometry);
            }
        }

        private float fillAspectRatio = 1;

        /// <summary>
        /// The desired ratio of width to height when under the effect of a non-stretching <see cref="FillMode"/>
        /// and <see cref="RelativeSizeAxes"/> being <see cref="Axes.Both"/>.
        /// </summary>
        public float FillAspectRatio
        {
            get => fillAspectRatio;
            set
            {
                if (fillAspectRatio == value) return;

                if (!float.IsFinite(value)) throw new ArgumentException($@"{nameof(FillAspectRatio)} must be finite, but is {value}.");
                if (value == 0) throw new ArgumentException($@"{nameof(FillAspectRatio)} must be non-zero.");

                fillAspectRatio = value;

                if (fillMode != FillMode.Stretch && RelativeSizeAxes == Axes.Both)
                    Invalidate(Invalidation.DrawSize);
            }
        }

        private FillMode fillMode;

        /// <summary>
        /// Controls the behavior of <see cref="RelativeSizeAxes"/> when it is set to <see cref="Axes.Both"/>.
        /// Otherwise, this member has no effect. By default, stretching is used, which simply scales
        /// this drawable's <see cref="Size"/> according to <see cref="Parent"/>'s <see cref="CompositeDrawable.RelativeToAbsoluteFactor"/>
        /// disregarding this drawable's <see cref="FillAspectRatio"/>. Other values of <see cref="FillMode"/> preserve <see cref="FillAspectRatio"/>.
        /// </summary>
        public FillMode FillMode
        {
            get => fillMode;
            set
            {
                if (fillMode == value) return;

                fillMode = value;

                Invalidate(Invalidation.DrawSize);
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
            get => shear;
            set
            {
                if (shear == value) return;

                if (!Validation.IsFinite(value)) throw new ArgumentException($@"{nameof(Shear)} must be finite, but is {value}.");

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
            get => rotation;
            set
            {
                if (value == rotation) return;

                if (!float.IsFinite(value)) throw new ArgumentException($@"{nameof(Rotation)} must be finite, but is {value}.");

                rotation = value;

                Invalidate(Invalidation.MiscGeometry);
            }
        }

        #endregion

        #region Origin / Anchor

        private Anchor origin = Anchor.TopLeft;

        /// <summary>
        /// The origin of this <see cref="Drawable"/>.
        /// </summary>
        /// <exception cref="ArgumentException">If the provided value does not exist in the <see cref="Graphics.Anchor"/> enumeration.</exception>
        public virtual Anchor Origin
        {
            get => origin;
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
        /// The origin of this <see cref="Drawable"/> expressed in relative coordinates from the top-left corner of <see cref="DrawRectangle"/>.
        /// </summary>
        /// <exception cref="InvalidOperationException">If <see cref="Origin"/> is <see cref="osu.Framework.Graphics.Anchor.Custom"/>.</exception>
        public Vector2 RelativeOriginPosition
        {
            get
            {
                if (Origin == Anchor.Custom)
                    throw new InvalidOperationException(@"Can not obtain relative origin position for custom origins.");

                Vector2 result = Vector2.Zero;
                if (origin.HasFlagFast(Anchor.x1))
                    result.X = 0.5f;
                else if (origin.HasFlagFast(Anchor.x2))
                    result.X = 1;

                if (origin.HasFlagFast(Anchor.y1))
                    result.Y = 0.5f;
                else if (origin.HasFlagFast(Anchor.y2))
                    result.Y = 1;

                return result;
            }
        }

        /// <summary>
        /// The origin of this <see cref="Drawable"/> expressed in absolute coordinates from the top-left corner of <see cref="DrawRectangle"/>.
        /// </summary>
        /// <exception cref="ArgumentException">If the provided value is not finite.</exception>
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
                if (customOrigin == value && Origin == Anchor.Custom)
                    return;

                if (!Validation.IsFinite(value)) throw new ArgumentException($@"{nameof(OriginPosition)} must be finite, but is {value}.");

                customOrigin = value;
                Origin = Anchor.Custom;

                Invalidate(Invalidation.MiscGeometry);
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
            get => anchor;
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
                if (anchor.HasFlagFast(Anchor.x1))
                    result.X = 0.5f;
                else if (anchor.HasFlagFast(Anchor.x2))
                    result.X = 1;

                if (anchor.HasFlagFast(Anchor.y1))
                    result.Y = 0.5f;
                else if (anchor.HasFlagFast(Anchor.y2))
                    result.Y = 1;

                return result;
            }

            set
            {
                if (customRelativeAnchorPosition == value && Anchor == Anchor.Custom)
                    return;

                if (!Validation.IsFinite(value)) throw new ArgumentException($@"{nameof(RelativeAnchorPosition)} must be finite, but is {value}.");

                customRelativeAnchorPosition = value;
                Anchor = Anchor.Custom;

                Invalidate(Invalidation.MiscGeometry);
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

            if (anchor.HasFlagFast(Anchor.x1))
                result.X = size.X / 2f;
            else if (anchor.HasFlagFast(Anchor.x2))
                result.X = size.X;

            if (anchor.HasFlagFast(Anchor.y1))
                result.Y = size.Y / 2f;
            else if (anchor.HasFlagFast(Anchor.y2))
                result.Y = size.Y;

            return result;
        }

        #endregion

        #region Colour / Alpha / Blending

        private ColourInfo colour = Color4.White;

        /// <summary>
        /// Colour of this <see cref="Drawable"/> in sRGB space. Can contain individual colours for all four
        /// corners of this <see cref="Drawable"/>, which are then interpolated, but can also be assigned
        /// just a single colour. Implicit casts from <see cref="SRGBColour"/> and from <see cref="Color4"/> exist.
        /// </summary>
        public ColourInfo Colour
        {
            get => colour;
            set
            {
                if (colour.Equals(value)) return;

                colour = value;

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
            get => alpha;
            set
            {
                if (alpha == value)
                    return;

                bool wasPresent = IsPresent;

                alpha = value;

                if (IsPresent != wasPresent)
                    Invalidate(Invalidation.Colour | Invalidation.Presence);
                else
                    Invalidate(Invalidation.Colour);
            }
        }

        private const float visibility_cutoff = 0.0001f;

        /// <summary>
        /// Determines whether this Drawable is present based on its <see cref="Alpha"/> value.
        /// Can be forced always on with <see cref="AlwaysPresent"/>.
        /// </summary>
        public virtual bool IsPresent => AlwaysPresent || (Alpha > visibility_cutoff && DrawScale.X != 0 && DrawScale.Y != 0);

        private bool alwaysPresent;

        /// <summary>
        /// If true, forces <see cref="IsPresent"/> to always be true. In other words,
        /// this drawable is always considered for layout, input, and drawing, regardless
        /// of alpha value.
        /// </summary>
        public bool AlwaysPresent
        {
            get => alwaysPresent;
            set
            {
                if (alwaysPresent == value)
                    return;

                bool wasPresent = IsPresent;

                alwaysPresent = value;

                if (IsPresent != wasPresent)
                    Invalidate(Invalidation.Presence);
            }
        }

        private BlendingParameters blending;

        /// <summary>
        /// Determines how this Drawable is blended with other already drawn Drawables.
        /// Inherits the <see cref="Parent"/>'s <see cref="Blending"/> by default.
        /// </summary>
        public BlendingParameters Blending
        {
            get => blending;
            set
            {
                if (blending == value)
                    return;

                blending = value;

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
            get => clock;
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
            scheduler?.UpdateClock(this.clock);
        }

        /// <summary>
        /// Whether <see cref="IFrameBasedClock.ProcessFrame"/> should be automatically invoked on this <see cref="Drawable"/>'s <see cref="Clock"/>
        /// in <see cref="UpdateSubTree"/>. This should only be set to false in scenarios where the clock is updated elsewhere.
        /// </summary>
        public bool ProcessCustomClock = true;

        private double lifetimeStart = double.MinValue;
        private double lifetimeEnd = double.MaxValue;

        /// <summary>
        /// Invoked after <see cref="lifetimeStart"/> or <see cref="LifetimeEnd"/> has changed.
        /// </summary>
        internal event Action<Drawable> LifetimeChanged;

        /// <summary>
        /// The time at which this drawable becomes valid (and is considered for drawing).
        /// </summary>
        public virtual double LifetimeStart
        {
            get => lifetimeStart;
            set
            {
                if (lifetimeStart == value) return;

                lifetimeStart = value;
                LifetimeChanged?.Invoke(this);
            }
        }

        /// <summary>
        /// The time at which this drawable is no longer valid (and is considered for disposal).
        /// </summary>
        public virtual double LifetimeEnd
        {
            get => lifetimeEnd;
            set
            {
                if (lifetimeEnd == value) return;

                lifetimeEnd = value;
                LifetimeChanged?.Invoke(this);
            }
        }

        /// <summary>
        /// Whether this drawable should currently be alive.
        /// This is queried by the framework to decide the <see cref="IsAlive"/> state of this drawable for the next frame.
        /// </summary>
        protected internal virtual bool ShouldBeAlive
        {
            get
            {
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
        /// Retrieve the first parent in the tree which derives from <see cref="InputManager"/>.
        /// As this is performing an upward tree traversal, avoid calling every frame.
        /// </summary>
        /// <returns>The first parent <see cref="InputManager"/>.</returns>
        protected InputManager GetContainingInputManager() => this.FindClosestParent<InputManager>();

        private CompositeDrawable parent;

        /// <summary>
        /// The parent of this drawable in the scene graph.
        /// </summary>
        public CompositeDrawable Parent
        {
            get => parent;
            internal set
            {
                if (IsDisposed)
                    throw new ObjectDisposedException(ToString(), "Disposed Drawables may never get a parent and return to the scene graph.");

                if (value == null)
                    ChildID = 0;

                if (parent == value) return;

                if (value != null && parent != null)
                    throw new InvalidOperationException("May not add a drawable to multiple containers.");

                parent = value;
                Invalidate(InvalidationFromParentSize | Invalidation.Colour | Invalidation.Presence | Invalidation.Parent);

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
        public bool HasProxy => proxy != null;

        /// <summary>
        /// True iff this <see cref="Drawable"/> is not a proxy of any <see cref="Drawable"/>.
        /// </summary>
        public bool IsProxy => Original != this;

        private Drawable proxy;

        /// <summary>
        /// Creates a proxy drawable which can be inserted elsewhere in the scene graph.
        /// Will cause the original instance to not render itself.
        /// Creating multiple proxies is not supported and will result in an
        /// <see cref="InvalidOperationException"/>.
        /// </summary>
        public Drawable CreateProxy()
        {
            if (proxy != null)
                throw new InvalidOperationException("Multiple proxies are not supported.");

            return proxy = new ProxyDrawable(this);
        }

        /// <summary>
        /// Validates a <see cref="DrawNode"/> for use by the proxy of this <see cref="Drawable"/>.
        /// This is used exclusively by <see cref="CompositeDrawable.addFromComposite"/>, and should not be used otherwise.
        /// </summary>
        /// <param name="treeIndex">The index of the <see cref="DrawNode"/> in <see cref="drawNodes"/> which the proxy should use.</param>
        /// <param name="frame">The frame for which the <see cref="DrawNode"/> was created. This is the parameter used for validation.</param>
        internal virtual void ValidateProxyDrawNode(int treeIndex, ulong frame) => proxy.ValidateProxyDrawNode(treeIndex, frame);

        #endregion

        #region Caching & invalidation (for things too expensive to compute every frame)

        /// <summary>
        /// Was this Drawable masked away completely during the last frame?
        /// This is measured conservatively, i.e. it is only true when the Drawable was
        /// actually masked away, but it may be false, even if the Drawable was masked away.
        /// </summary>
        internal bool IsMaskedAway { get; private set; }

        private readonly LayoutValue<Quad> screenSpaceDrawQuadBacking = new LayoutValue<Quad>(Invalidation.DrawInfo | Invalidation.RequiredParentSizeToFit | Invalidation.Presence);

        protected virtual Quad ComputeScreenSpaceDrawQuad() => ToScreenSpace(DrawRectangle);

        /// <summary>
        /// The screen-space quad this drawable occupies.
        /// </summary>
        public virtual Quad ScreenSpaceDrawQuad => screenSpaceDrawQuadBacking.IsValid ? screenSpaceDrawQuadBacking : screenSpaceDrawQuadBacking.Value = ComputeScreenSpaceDrawQuad();

        private readonly LayoutValue<DrawInfo> drawInfoBacking = new LayoutValue<DrawInfo>(Invalidation.DrawInfo | Invalidation.RequiredParentSizeToFit | Invalidation.Presence);

        private DrawInfo computeDrawInfo()
        {
            DrawInfo di = Parent?.DrawInfo ?? new DrawInfo(null);

            Vector2 pos = DrawPosition + AnchorPosition;
            Vector2 drawScale = DrawScale;

            if (Parent != null)
                pos += Parent.ChildOffset;

            di.ApplyTransform(pos, drawScale, Rotation, Shear, OriginPosition);

            return di;
        }

        /// <summary>
        /// Contains the linear transformation of this <see cref="Drawable"/> that is used during draw.
        /// </summary>
        public virtual DrawInfo DrawInfo => drawInfoBacking.IsValid ? drawInfoBacking : drawInfoBacking.Value = computeDrawInfo();

        private readonly LayoutValue<DrawColourInfo> drawColourInfoBacking = new LayoutValue<DrawColourInfo>(
            Invalidation.Colour | Invalidation.DrawInfo | Invalidation.RequiredParentSizeToFit | Invalidation.Presence,
            conditions: (s, i) =>
            {
                if ((i & Invalidation.Colour) > 0)
                    return true;

                return !s.Colour.HasSingleColour || (s.drawColourInfoBacking.IsValid && !s.drawColourInfoBacking.Value.Colour.HasSingleColour);
            });

        /// <summary>
        /// Contains the colour and blending information of this <see cref="Drawable"/> that are used during draw.
        /// </summary>
        public virtual DrawColourInfo DrawColourInfo => drawColourInfoBacking.IsValid ? drawColourInfoBacking : drawColourInfoBacking.Value = computeDrawColourInfo();

        private DrawColourInfo computeDrawColourInfo()
        {
            DrawColourInfo ci = Parent?.DrawColourInfo ?? new DrawColourInfo(null);

            BlendingParameters localBlending = Blending;

            if (Parent != null)
                localBlending.CopyFromParent(ci.Blending);

            localBlending.ApplyDefaultToInherited();

            ci.Blending = localBlending;

            ColourInfo ourColour = alpha != 1 ? colour.MultiplyAlpha(alpha) : colour;

            if (ci.Colour.HasSingleColour)
                ci.Colour.ApplyChild(ourColour);
            else
            {
                Debug.Assert(Parent != null,
                    $"The {nameof(ci)} of null parents should always have the single colour white, and therefore this branch should never be hit.");

                // Cannot use ToParentSpace here, because ToParentSpace depends on DrawInfo to be completed
                // ReSharper disable once PossibleNullReferenceException
                Quad interp = Quad.FromRectangle(DrawRectangle) * (DrawInfo.Matrix * Parent.DrawInfo.MatrixInverse);
                Vector2 parentSize = Parent.DrawSize;

                ci.Colour.ApplyChild(ourColour,
                    new Quad(
                        Vector2.Divide(interp.TopLeft, parentSize),
                        Vector2.Divide(interp.TopRight, parentSize),
                        Vector2.Divide(interp.BottomLeft, parentSize),
                        Vector2.Divide(interp.BottomRight, parentSize)));
            }

            return ci;
        }

        private readonly LayoutValue<Vector2> requiredParentSizeToFitBacking = new LayoutValue<Vector2>(Invalidation.RequiredParentSizeToFit);

        private Vector2 computeRequiredParentSizeToFit()
        {
            // Auxiliary variables required for the computation
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
        }

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
        internal Vector2 RequiredParentSizeToFit => requiredParentSizeToFitBacking.IsValid ? requiredParentSizeToFitBacking : requiredParentSizeToFitBacking.Value = computeRequiredParentSizeToFit();

        /// <summary>
        /// The flags which this <see cref="Drawable"/> has been invalidated with, grouped by <see cref="InvalidationSource"/>.
        /// </summary>
        private InvalidationList invalidationList = new InvalidationList(Invalidation.All);

        /// <summary>
        /// Represents the most-recently added <see cref="LayoutMember"/> (via <see cref="AddLayout"/>).
        /// It is also used as a linked-list during invalidation by traversing through <see cref="LayoutMember.Next"/>.
        /// </summary>
        private LayoutMember layoutList;

        /// <summary>
        /// Adds a layout member that will be invalidated when its <see cref="LayoutMember.Invalidation"/> is invalidated.
        /// </summary>
        /// <param name="member">The layout member to add.</param>
        protected void AddLayout(LayoutMember member)
        {
            if (LoadState > LoadState.NotLoaded)
                throw new InvalidOperationException($"{nameof(LayoutMember)}s cannot be added after {nameof(Drawable)}s have started loading. Consider adding in the constructor.");

            if (layoutList == null)
                layoutList = member;
            else
            {
                member.Next = layoutList;
                layoutList = member;
            }

            member.Parent = this;
        }

        /// <summary>
        /// Validates the super-tree of this <see cref="Drawable"/> for the given <see cref="Invalidation"/> flags.
        /// </summary>
        /// <remarks>
        /// This is internally invoked by <see cref="LayoutMember"/>, and should not be invoked manually.
        /// </remarks>
        /// <param name="validationType">The <see cref="Invalidation"/> flags to validate with.</param>
        internal void ValidateSuperTree(Invalidation validationType)
        {
            if (invalidationList.Validate(validationType))
                Parent?.ValidateSuperTree(validationType);
        }

        // Make sure we start out with a value of 1 such that ApplyDrawNode is always called at least once
        public long InvalidationID { get; private set; } = 1;

        /// <summary>
        /// Invalidates the layout of this <see cref="Drawable"/>.
        /// </summary>
        /// <param name="invalidation">The flags to invalidate with.</param>
        /// <param name="source">The source that triggered the invalidation.</param>
        /// <returns>If any layout was invalidated.</returns>
        public bool Invalidate(Invalidation invalidation = Invalidation.All, InvalidationSource source = InvalidationSource.Self) => invalidate(invalidation, source);

        /// <summary>
        /// Invalidates the layout of this <see cref="Drawable"/>.
        /// </summary>
        /// <param name="invalidation">The flags to invalidate with.</param>
        /// <param name="source">The source that triggered the invalidation.</param>
        /// <param name="propagateToParent">Whether to propagate the invalidation to the parent of this <see cref="Drawable"/>.
        /// Only has an effect if <paramref name="source"/> is <see cref="InvalidationSource.Self"/>.</param>
        /// <returns>If any layout was invalidated.</returns>
        private bool invalidate(Invalidation invalidation = Invalidation.All, InvalidationSource source = InvalidationSource.Self, bool propagateToParent = true)
        {
            if (source != InvalidationSource.Child && source != InvalidationSource.Parent && source != InvalidationSource.Self)
                throw new InvalidOperationException($"A {nameof(Drawable)} can only be invalidated with a singular {nameof(source)} (child, parent, or self).");

            if (LoadState < LoadState.Ready)
                return false;

            // Changes in the colour of children don't affect parents.
            if (source == InvalidationSource.Child)
                invalidation &= ~Invalidation.Colour;

            if (invalidation == Invalidation.None)
                return false;

            // If the invalidation originated locally, propagate to the immediate parent.
            // Note: This is done _before_ invalidation is blocked below, since the parent always needs to be aware of changes even if the Drawable's invalidation state hasn't changed.
            // This is for only propagating once, otherwise it would propagate all the way to the root Drawable.
            if (propagateToParent && source == InvalidationSource.Self)
                Parent?.Invalidate(invalidation, InvalidationSource.Child);

            // Perform the invalidation.
            if (!invalidationList.Invalidate(source, invalidation))
                return false;

            // A DrawNode invalidation always invalidates.
            bool anyInvalidated = (invalidation & Invalidation.DrawNode) > 0;

            // Invalidate all layout members
            LayoutMember nextLayout = layoutList;

            while (nextLayout != null)
            {
                // Only invalidate layout members that accept the given source.
                if ((nextLayout.Source & source) == 0)
                    goto NextLayoutIteration;

                // Remove invalidation flags that don't refer to the layout member.
                Invalidation memberInvalidation = invalidation & nextLayout.Invalidation;
                if (memberInvalidation == 0)
                    goto NextLayoutIteration;

                if (nextLayout.Conditions?.Invoke(this, memberInvalidation) != false)
                    anyInvalidated |= nextLayout.Invalidate();

                NextLayoutIteration:
                nextLayout = nextLayout.Next;
            }

            // Allow any custom invalidation to take place.
            anyInvalidated |= OnInvalidate(invalidation, source);

            if (anyInvalidated)
                InvalidationID++;

            Invalidated?.Invoke(this);

            return anyInvalidated;
        }

        /// <summary>
        /// Invoked when the layout of this <see cref="Drawable"/> was invalidated.
        /// </summary>
        /// <remarks>
        /// This should be used to perform any custom invalidation logic that cannot be described as a layout.
        /// </remarks>
        /// <remarks>
        /// This does not ensure that the parent containers have been updated before us, thus operations involving
        /// parent states (e.g. <see cref="DrawInfo"/>) should not be executed in an overridden implementation.
        /// </remarks>
        /// <param name="invalidation">The flags that the this <see cref="Drawable"/> was invalidated with.</param>
        /// <param name="source">The source that triggered the invalidation.</param>
        /// <returns>Whether any custom invalidation was performed. Must be true if the <see cref="DrawNode"/> for this <see cref="Drawable"/> is to be invalidated.</returns>
        protected virtual bool OnInvalidate(Invalidation invalidation, InvalidationSource source) => false;

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

        /// <summary>
        /// A fast path for invalidating ourselves and our parent's children size dependencies whenever a size or position change occurs.
        /// </summary>
        /// <param name="invalidation">The <see cref="Invalidation"/> to invalidate with.</param>
        /// <param name="changedAxes">The <see cref="Axes"/> that were affected.</param>
        private void invalidateParentSizeDependencies(Invalidation invalidation, Axes changedAxes)
        {
            // We're invalidating the parent manually, so we should not propagate it upwards.
            invalidate(invalidation, InvalidationSource.Self, false);

            // The fast path, which performs an invalidation on the parent along with optimisations for bypassed sizing axes.
            Parent?.InvalidateChildrenSizeDependencies(invalidation, changedAxes, this);
        }

        #endregion

        #region DrawNode

        private readonly DrawNode[] drawNodes = new DrawNode[IRenderer.MAX_DRAW_NODES];

        /// <summary>
        /// Generates the <see cref="DrawNode"/> for ourselves.
        /// </summary>
        /// <param name="frame">The frame which the <see cref="DrawNode"/> sub-tree should be generated for.</param>
        /// <param name="treeIndex">The index of the <see cref="DrawNode"/> to use.</param>
        /// <param name="forceNewDrawNode">Whether the creation of a new <see cref="DrawNode"/> should be forced, rather than re-using an existing <see cref="DrawNode"/>.</param>
        /// <returns>A complete and updated <see cref="DrawNode"/>, or null if the <see cref="DrawNode"/> would be invisible.</returns>
        internal virtual DrawNode GenerateDrawNodeSubtree(ulong frame, int treeIndex, bool forceNewDrawNode)
        {
            DrawNode node = drawNodes[treeIndex];

            if (node == null || forceNewDrawNode)
            {
                drawNodes[treeIndex] = node = CreateDrawNode();
                FrameStatistics.Increment(StatisticsCounterType.DrawNodeCtor);
            }

            if (InvalidationID != node.InvalidationID)
            {
                node.ApplyState();
                FrameStatistics.Increment(StatisticsCounterType.DrawNodeAppl);
            }

            return node;
        }

        /// <summary>
        /// Creates a draw node capable of containing all information required to draw this drawable.
        /// </summary>
        /// <returns>The created draw node.</returns>
        protected virtual DrawNode CreateDrawNode() => new DrawNode(this);

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

            return Vector2Extensions.Transform(Vector2Extensions.Transform(input, DrawInfo.Matrix), other.DrawInfo.MatrixInverse);
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
        public Vector2 ToScreenSpace(Vector2 input) => Vector2Extensions.Transform(input, DrawInfo.Matrix);

        /// <summary>
        /// Accepts a rectangle in local coordinates and converts it to a quad in screen space.
        /// </summary>
        /// <param name="input">A rectangle in local coordinates.</param>
        /// <returns>The quad in screen coordinates.</returns>
        public Quad ToScreenSpace(RectangleF input) => Quad.FromRectangle(input) * DrawInfo.Matrix;

        /// <summary>
        /// Accepts a vector in screen coordinates and converts it to coordinates in local space.
        /// </summary>
        /// <param name="screenSpacePos">A vector in screen coordinates.</param>
        /// <returns>The vector in local coordinates.</returns>
        public Vector2 ToLocalSpace(Vector2 screenSpacePos) => Vector2Extensions.Transform(screenSpacePos, DrawInfo.MatrixInverse);

        /// <summary>
        /// Accepts a quad in screen coordinates and converts it to coordinates in local space.
        /// </summary>
        /// <param name="screenSpaceQuad">A quad in screen coordinates.</param>
        /// <returns>The quad in local coordinates.</returns>
        public Quad ToLocalSpace(Quad screenSpaceQuad) => screenSpaceQuad * DrawInfo.MatrixInverse;

        #endregion

        #region Interaction / Input

        /// <summary>
        /// Handle a UI event.
        /// </summary>
        /// <param name="e">The event to be handled.</param>
        /// <returns>If the event supports blocking, returning true will make the event to not propagating further.</returns>
        protected virtual bool Handle(UIEvent e) => false;

        /// <summary>
        /// Trigger a UI event with <see cref="UIEvent.Target"/> set to this <see cref="Drawable"/>.
        /// </summary>
        /// <param name="e">The event. Its <see cref="UIEvent.Target"/> will be modified.</param>
        /// <returns>The result of event handler.</returns>
        public bool TriggerEvent(UIEvent e)
        {
            e.Target = this;

            switch (e)
            {
                case MouseMoveEvent mouseMove:
                    return OnMouseMove(mouseMove);

                case HoverEvent hover:
                    return OnHover(hover);

                case HoverLostEvent hoverLost:
                    OnHoverLost(hoverLost);
                    return false;

                case MouseDownEvent mouseDown:
                    return OnMouseDown(mouseDown);

                case MouseUpEvent mouseUp:
                    OnMouseUp(mouseUp);
                    return false;

                case ClickEvent click:
                    return OnClick(click);

                case DoubleClickEvent doubleClick:
                    return OnDoubleClick(doubleClick);

                case DragStartEvent dragStart:
                    return OnDragStart(dragStart);

                case DragEvent drag:
                    OnDrag(drag);
                    return false;

                case DragEndEvent dragEnd:
                    OnDragEnd(dragEnd);
                    return false;

                case ScrollEvent scroll:
                    return OnScroll(scroll);

                case FocusEvent focus:
                    OnFocus(focus);
                    return false;

                case FocusLostEvent focusLost:
                    OnFocusLost(focusLost);
                    return false;

                case KeyDownEvent keyDown:
                    return OnKeyDown(keyDown);

                case KeyUpEvent keyUp:
                    OnKeyUp(keyUp);
                    return false;

                case TouchDownEvent touchDown:
                    return OnTouchDown(touchDown);

                case TouchMoveEvent touchMove:
                    OnTouchMove(touchMove);
                    return false;

                case TouchUpEvent touchUp:
                    OnTouchUp(touchUp);
                    return false;

                case JoystickPressEvent joystickPress:
                    return OnJoystickPress(joystickPress);

                case JoystickReleaseEvent joystickRelease:
                    OnJoystickRelease(joystickRelease);
                    return false;

                case JoystickAxisMoveEvent joystickAxisMove:
                    return OnJoystickAxisMove(joystickAxisMove);

                case MidiDownEvent midiDown:
                    return OnMidiDown(midiDown);

                case MidiUpEvent midiUp:
                    OnMidiUp(midiUp);
                    return false;

                case TabletPenButtonPressEvent tabletPenButtonPress:
                    return OnTabletPenButtonPress(tabletPenButtonPress);

                case TabletPenButtonReleaseEvent tabletPenButtonRelease:
                    OnTabletPenButtonRelease(tabletPenButtonRelease);
                    return false;

                case TabletAuxiliaryButtonPressEvent tabletAuxiliaryButtonPress:
                    return OnTabletAuxiliaryButtonPress(tabletAuxiliaryButtonPress);

                case TabletAuxiliaryButtonReleaseEvent tabletAuxiliaryButtonRelease:
                    OnTabletAuxiliaryButtonRelease(tabletAuxiliaryButtonRelease);
                    return false;

                default:
                    return Handle(e);
            }
        }

        /// <summary>
        /// Triggers a left click event for this <see cref="Drawable"/>.
        /// </summary>
        /// <returns>Whether the click event is handled.</returns>
        public bool TriggerClick() => TriggerEvent(new ClickEvent(GetContainingInputManager()?.CurrentState ?? new InputState(), MouseButton.Left));

        #region Individual event handlers

        /// <summary>
        /// An event that occurs every time the mouse is moved while hovering this <see cref="Drawable"/>.
        /// </summary>
        /// <param name="e">The <see cref="MouseMoveEvent"/> containing information about the input event.</param>
        /// <returns>Whether to block the event from propagating to other <see cref="Drawable"/>s in the hierarchy.</returns>
        protected virtual bool OnMouseMove(MouseMoveEvent e) => Handle(e);

        /// <summary>
        /// An event that occurs when the mouse starts hovering this <see cref="Drawable"/>.
        /// </summary>
        /// <param name="e">The <see cref="HoverEvent"/> containing information about the input event.</param>
        /// <returns>Whether to block the event from propagating to other <see cref="Drawable"/>s in the hierarchy.</returns>
        protected virtual bool OnHover(HoverEvent e) => Handle(e);

        /// <summary>
        /// An event that occurs when the mouse stops hovering this <see cref="Drawable"/>.
        /// </summary>
        /// <remarks>
        /// This is guaranteed to be invoked if <see cref="OnHover"/> was invoked.
        /// </remarks>
        /// <param name="e">The <see cref="HoverLostEvent"/> containing information about the input event.</param>
        protected virtual void OnHoverLost(HoverLostEvent e) => Handle(e);

        /// <summary>
        /// An event that occurs when a <see cref="MouseButton"/> is pressed on this <see cref="Drawable"/>.
        /// </summary>
        /// <param name="e">The <see cref="MouseDownEvent"/> containing information about the input event.</param>
        /// <returns>Whether to block the event from propagating to other <see cref="Drawable"/>s in the hierarchy.</returns>
        protected virtual bool OnMouseDown(MouseDownEvent e) => Handle(e);

        /// <summary>
        /// An event that occurs when a <see cref="MouseButton"/> that was pressed on this <see cref="Drawable"/> is released.
        /// </summary>
        /// <remarks>
        /// This is guaranteed to be invoked if <see cref="OnMouseDown"/> was invoked.
        /// </remarks>
        /// <param name="e">The <see cref="MouseUpEvent"/> containing information about the input event.</param>
        protected virtual void OnMouseUp(MouseUpEvent e) => Handle(e);

        /// <summary>
        /// An event that occurs when a <see cref="MouseButton"/> is clicked on this <see cref="Drawable"/>.
        /// </summary>
        /// <remarks>
        /// This will only be invoked on the <see cref="Drawable"/>s that received a previous <see cref="OnMouseDown"/> invocation
        /// which are still present in the input queue (via <see cref="BuildPositionalInputQueue"/>) when the click occurs.<br />
        /// This will not occur if a drag was started (<see cref="OnDragStart"/> was invoked) or a double-click occurred (<see cref="OnDoubleClick"/> was invoked).
        /// </remarks>
        /// <param name="e">The <see cref="ClickEvent"/> containing information about the input event.</param>
        /// <returns>Whether to block the event from propagating to other <see cref="Drawable"/>s in the hierarchy.</returns>
        protected virtual bool OnClick(ClickEvent e) => Handle(e);

        /// <summary>
        /// An event that occurs when a <see cref="MouseButton"/> is double-clicked on this <see cref="Drawable"/>.
        /// </summary>
        /// <remarks>
        /// This will only be invoked on the <see cref="Drawable"/> that returned <code>true</code> from a previous <see cref="OnClick"/> invocation.
        /// </remarks>
        /// <param name="e">The <see cref="DoubleClickEvent"/> containing information about the input event.</param>
        /// <returns>Whether to block the next <see cref="OnClick"/> event from occurring.</returns>
        protected virtual bool OnDoubleClick(DoubleClickEvent e) => Handle(e);

        /// <summary>
        /// An event that occurs when the mouse starts dragging on this <see cref="Drawable"/>.
        /// </summary>
        /// <param name="e">The <see cref="DragEvent"/> containing information about the input event.</param>
        /// <returns>Whether to block the event from propagating to other <see cref="Drawable"/>s in the hierarchy.</returns>
        protected virtual bool OnDragStart(DragStartEvent e) => Handle(e);

        /// <summary>
        /// An event that occurs every time the mouse moves while dragging this <see cref="Drawable"/>.
        /// </summary>
        /// <remarks>
        /// This will only be invoked on the <see cref="Drawable"/> that returned <code>true</code> from a previous <see cref="OnDragStart"/> invocation.
        /// </remarks>
        /// <param name="e">The <see cref="DragEvent"/> containing information about the input event.</param>
        protected virtual void OnDrag(DragEvent e) => Handle(e);

        /// <summary>
        /// An event that occurs when the mouse stops dragging this <see cref="Drawable"/>.
        /// </summary>
        /// <remarks>
        /// This will only be invoked on the <see cref="Drawable"/> that returned <code>true</code> from a previous <see cref="OnDragStart"/> invocation.
        /// </remarks>
        /// <param name="e">The <see cref="DragEndEvent"/> containing information about the input event.</param>
        protected virtual void OnDragEnd(DragEndEvent e) => Handle(e);

        /// <summary>
        /// An event that occurs when the mouse wheel is scrolled on this <see cref="Drawable"/>.
        /// </summary>
        /// <param name="e">The <see cref="ScrollEvent"/> containing information about the input event.</param>
        /// <returns>Whether to block the event from propagating to other <see cref="Drawable"/>s in the hierarchy.</returns>
        protected virtual bool OnScroll(ScrollEvent e) => Handle(e);

        /// <summary>
        /// An event that occurs when this <see cref="Drawable"/> gains focus.
        /// </summary>
        /// <remarks>
        /// This will only be invoked on the <see cref="Drawable"/> that returned <code>true</code> from both <see cref="AcceptsFocus"/> and a previous <see cref="OnClick"/> invocation.
        /// </remarks>
        /// <param name="e">The <see cref="FocusEvent"/> containing information about the input event.</param>
        protected virtual void OnFocus(FocusEvent e) => Handle(e);

        /// <summary>
        /// An event that occurs when this <see cref="Drawable"/> loses focus.
        /// </summary>
        /// <remarks>
        /// This will only be invoked on the <see cref="Drawable"/> that previously had focus (<see cref="OnFocus"/> was invoked).
        /// </remarks>
        /// <param name="e">The <see cref="FocusLostEvent"/> containing information about the input event.</param>
        protected virtual void OnFocusLost(FocusLostEvent e) => Handle(e);

        /// <summary>
        /// An event that occurs when a <see cref="Key"/> is pressed.
        /// </summary>
        /// <remarks>
        /// Repeat events can only be invoked on the <see cref="Drawable"/>s that received a previous non-repeat <see cref="OnKeyDown"/> invocation
        /// which are still present in the input queue (via <see cref="BuildNonPositionalInputQueue"/>) when the repeat occurs.
        /// </remarks>
        /// <param name="e">The <see cref="KeyDownEvent"/> containing information about the input event.</param>
        /// <returns>Whether to block the event from propagating to other <see cref="Drawable"/>s in the hierarchy.</returns>
        protected virtual bool OnKeyDown(KeyDownEvent e) => Handle(e);

        /// <summary>
        /// An event that occurs when a <see cref="Key"/> is released.
        /// </summary>
        /// <remarks>
        /// This is guaranteed to be invoked if <see cref="OnKeyDown"/> was invoked.
        /// </remarks>
        /// <param name="e">The <see cref="KeyUpEvent"/> containing information about the input event.</param>
        protected virtual void OnKeyUp(KeyUpEvent e) => Handle(e);

        /// <summary>
        /// An event that occurs when a <see cref="Touch"/> is active.
        /// </summary>
        /// <param name="e">The <see cref="TouchDownEvent"/> containing information about the input event.</param>
        /// <returns>Whether to block the event from propagating to other <see cref="Drawable"/>s in the hierarchy.</returns>
        protected virtual bool OnTouchDown(TouchDownEvent e) => Handle(e);

        /// <summary>
        /// An event that occurs every time an active <see cref="Touch"/> has moved while hovering this <see cref="Drawable"/>.
        /// </summary>
        /// <remarks>
        /// This will only be invoked on the <see cref="Drawable"/>s that received a previous <see cref="OnTouchDown"/> invocation from the source of this touch.
        /// This will not occur if the touch has been activated then deactivated without moving from its initial position.
        /// </remarks>
        /// <param name="e">The <see cref="TouchMoveEvent"/> containing information about the input event.</param>
        protected virtual void OnTouchMove(TouchMoveEvent e) => Handle(e);

        /// <summary>
        /// An event that occurs when a <see cref="Touch"/> is not active.
        /// </summary>
        /// <remarks>
        /// This is guaranteed to be invoked if <see cref="OnTouchDown"/> was invoked.
        /// </remarks>
        /// <param name="e">The <see cref="TouchUpEvent"/> containing information about the input event.</param>
        protected virtual void OnTouchUp(TouchUpEvent e) => Handle(e);

        /// <summary>
        /// An event that occurs when a <see cref="JoystickButton"/> is pressed.
        /// </summary>
        /// <param name="e">The <see cref="JoystickPressEvent"/> containing information about the input event.</param>
        /// <returns>Whether to block the event from propagating to other <see cref="Drawable"/>s in the hierarchy.</returns>
        protected virtual bool OnJoystickPress(JoystickPressEvent e) => Handle(e);

        /// <summary>
        /// An event that occurs when a <see cref="JoystickButton"/> is released.
        /// </summary>
        /// <remarks>
        /// This is guaranteed to be invoked if <see cref="OnJoystickPress"/> was invoked.
        /// </remarks>
        /// <param name="e">The <see cref="JoystickReleaseEvent"/> containing information about the input event.</param>
        protected virtual void OnJoystickRelease(JoystickReleaseEvent e) => Handle(e);

        /// <summary>
        /// An event that occurs when a <see cref="JoystickAxis"/> is moved.
        /// </summary>
        /// <param name="e">The <see cref="JoystickAxisMoveEvent"/> containing information about the input event.</param>
        /// <returns>Whether to block the event from propagating to other <see cref="Drawable"/>s in the hierarchy.</returns>
        protected virtual bool OnJoystickAxisMove(JoystickAxisMoveEvent e) => Handle(e);

        /// <summary>
        /// An event that occurs when a <see cref="MidiKey"/> is pressed.
        /// </summary>
        /// <param name="e">The <see cref="MidiDownEvent"/> containing information about the input event.</param>
        /// <returns>Whether to block the event from propagating to other <see cref="Drawable"/>s in the hierarchy.</returns>
        protected virtual bool OnMidiDown(MidiDownEvent e) => Handle(e);

        /// <summary>
        /// An event that occurs when a <see cref="MidiKey"/> is released.
        /// </summary>
        /// <remarks>
        /// This is guaranteed to be invoked if <see cref="MidiDownEvent"/> was invoked.
        /// </remarks>
        /// <param name="e">The <see cref="MidiUpEvent"/> containing information about the input event.</param>
        protected virtual void OnMidiUp(MidiUpEvent e) => Handle(e);

        /// <summary>
        /// An event that occurs when a <see cref="TabletPenButton"/> is pressed.
        /// </summary>
        /// <param name="e">The <see cref="TabletPenButtonPressEvent"/> containing information about the input event.</param>
        /// <returns>Whether to block the event from propagating to other <see cref="Drawable"/>s in the hierarchy.</returns>
        protected virtual bool OnTabletPenButtonPress(TabletPenButtonPressEvent e) => Handle(e);

        /// <summary>
        /// An event that occurs when a <see cref="TabletPenButton"/> is released.
        /// </summary>
        /// <remarks>
        /// This is guaranteed to be invoked if <see cref="OnTabletPenButtonPress"/> was invoked.
        /// </remarks>
        /// <param name="e">The <see cref="TabletPenButtonReleaseEvent"/> containing information about the input event.</param>
        protected virtual void OnTabletPenButtonRelease(TabletPenButtonReleaseEvent e) => Handle(e);

        /// <summary>
        /// An event that occurs when a <see cref="TabletAuxiliaryButton"/> is pressed.
        /// </summary>
        /// <param name="e">The <see cref="TabletAuxiliaryButtonPressEvent"/> containing information about the input event.</param>
        /// <returns>Whether to block the event from propagating to other <see cref="Drawable"/>s in the hierarchy.</returns>
        protected virtual bool OnTabletAuxiliaryButtonPress(TabletAuxiliaryButtonPressEvent e) => Handle(e);

        /// <summary>
        /// An event that occurs when a <see cref="TabletAuxiliaryButton"/> is released.
        /// </summary>
        /// <remarks>
        /// This is guaranteed to be invoked if <see cref="OnTabletAuxiliaryButtonPress"/> was invoked.
        /// </remarks>
        /// <param name="e">The <see cref="TabletAuxiliaryButtonReleaseEvent"/> containing information about the input event.</param>
        protected virtual void OnTabletAuxiliaryButtonRelease(TabletAuxiliaryButtonReleaseEvent e) => Handle(e);

        #endregion

        /// <summary>
        /// Whether this drawable should receive non-positional input. This does not mean that the drawable will immediately handle the received input, but that it may handle it at some point.
        /// </summary>
        internal bool RequestsNonPositionalInput { get; private set; }

        /// <summary>
        /// Whether this drawable should receive positional input. This does not mean that the drawable will immediately handle the received input, but that it may handle it at some point.
        /// </summary>
        internal bool RequestsPositionalInput { get; private set; }

        /// <summary>
        /// Conservatively approximates whether there is a descendant which <see cref="RequestsNonPositionalInput"/> in the sub-tree rooted at this drawable
        /// to enable sub-tree skipping optimization for input handling.
        /// </summary>
        internal bool RequestsNonPositionalInputSubTree;

        /// <summary>
        /// Conservatively approximates whether there is a descendant which <see cref="RequestsPositionalInput"/> in the sub-tree rooted at this drawable
        /// to enable sub-tree skipping optimization for input handling.
        /// </summary>
        internal bool RequestsPositionalInputSubTree;

        /// <summary>
        /// Whether this <see cref="Drawable"/> handles non-positional input.
        /// This value is true by default if <see cref="Handle"/> or any non-positional (e.g. keyboard related) "On-" input methods are overridden.
        /// </summary>
        public virtual bool HandleNonPositionalInput => RequestsNonPositionalInput;

        /// <summary>
        /// Whether this <see cref="Drawable"/> handles positional input.
        /// This value is true by default if <see cref="Handle"/> or any positional (i.e. mouse related) "On-" input methods are overridden.
        /// </summary>
        public virtual bool HandlePositionalInput => RequestsPositionalInput;

        /// <summary>
        /// Nested class which is used for caching <see cref="HandleNonPositionalInput"/>, <see cref="HandlePositionalInput"/> values obtained via reflection.
        /// </summary>
        private static class HandleInputCache
        {
            private static readonly ConcurrentDictionary<Type, bool> positional_cached_values = new ConcurrentDictionary<Type, bool>();
            private static readonly ConcurrentDictionary<Type, bool> non_positional_cached_values = new ConcurrentDictionary<Type, bool>();

            private static readonly string[] positional_input_methods =
            {
                nameof(Handle),
                nameof(OnMouseMove),
                nameof(OnHover),
                nameof(OnHoverLost),
                nameof(OnMouseDown),
                nameof(OnMouseUp),
                nameof(OnClick),
                nameof(OnDoubleClick),
                nameof(OnDragStart),
                nameof(OnDrag),
                nameof(OnDragEnd),
                nameof(OnScroll),
                nameof(OnFocus),
                nameof(OnFocusLost),
                nameof(OnTouchDown),
                nameof(OnTouchMove),
                nameof(OnTouchUp),
                nameof(OnTabletPenButtonPress),
                nameof(OnTabletPenButtonRelease)
            };

            private static readonly string[] non_positional_input_methods =
            {
                nameof(Handle),
                nameof(OnFocus),
                nameof(OnFocusLost),
                nameof(OnKeyDown),
                nameof(OnKeyUp),
                nameof(OnJoystickPress),
                nameof(OnJoystickRelease),
                nameof(OnJoystickAxisMove),
                nameof(OnTabletAuxiliaryButtonPress),
                nameof(OnTabletAuxiliaryButtonRelease),
                nameof(OnMidiDown),
                nameof(OnMidiUp)
            };

            private static readonly Type[] positional_input_interfaces =
            {
                typeof(IHasTooltip),
                typeof(IHasCustomTooltip),
                typeof(IHasContextMenu),
                typeof(IHasPopover),
            };

            private static readonly Type[] non_positional_input_interfaces =
            {
                typeof(IKeyBindingHandler),
            };

            private static readonly string[] positional_input_properties =
            {
                nameof(HandlePositionalInput),
            };

            private static readonly string[] non_positional_input_properties =
            {
                nameof(HandleNonPositionalInput),
                nameof(AcceptsFocus),
            };

            public static bool RequestsNonPositionalInput(Drawable drawable) => get(drawable, non_positional_cached_values, false);

            public static bool RequestsPositionalInput(Drawable drawable) => get(drawable, positional_cached_values, true);

            private static bool get(Drawable drawable, ConcurrentDictionary<Type, bool> cache, bool positional)
            {
                var type = drawable.GetType();

                if (!cache.TryGetValue(type, out bool value))
                {
                    value = compute(type, positional);
                    cache.TryAdd(type, value);
                }

                return value;
            }

            private static bool compute([NotNull] Type type, bool positional)
            {
                string[] inputMethods = positional ? positional_input_methods : non_positional_input_methods;

                foreach (string inputMethod in inputMethods)
                {
                    // check for any input method overrides which are at a higher level than drawable.
                    var method = type.GetMethod(inputMethod, BindingFlags.Instance | BindingFlags.NonPublic);

                    Debug.Assert(method != null);

                    if (method.DeclaringType != typeof(Drawable))
                        return true;
                }

                var inputInterfaces = positional ? positional_input_interfaces : non_positional_input_interfaces;

                foreach (var inputInterface in inputInterfaces)
                {
                    // check if this type implements any interface which requires a drawable to handle input.
                    if (inputInterface.IsAssignableFrom(type))
                        return true;
                }

                string[] inputProperties = positional ? positional_input_properties : non_positional_input_properties;

                foreach (string inputProperty in inputProperties)
                {
                    var property = type.GetProperty(inputProperty);

                    Debug.Assert(property != null);

                    if (property.DeclaringType != typeof(Drawable))
                        return true;
                }

                return false;
            }
        }

        /// <summary>
        /// Check whether we have active focus.
        /// </summary>
        public bool HasFocus { get; internal set; }

        /// <summary>
        /// If true, we are eagerly requesting focus. If nothing else above us has (or is requesting focus) we will get it.
        /// </summary>
        /// <remarks>In order to get focused, <see cref="HandleNonPositionalInput"/> must be true.</remarks>
        public virtual bool RequestsFocus => false;

        /// <summary>
        /// If true, we will gain focus (receiving priority on keyboard input) (and receive an <see cref="OnFocus"/> event) on returning true in <see cref="OnClick"/>.
        /// </summary>
        public virtual bool AcceptsFocus => false;

        /// <summary>
        /// Whether this Drawable is currently hovered over.
        /// </summary>
        /// <remarks>This is updated only if <see cref="HandlePositionalInput"/> is true.</remarks>
        public bool IsHovered { get; internal set; }

        /// <summary>
        /// Whether this Drawable is currently being dragged.
        /// </summary>
        public bool IsDragged { get; internal set; }

        /// <summary>
        /// Determines whether this drawable receives positional input when the mouse is at the
        /// given screen-space position.
        /// </summary>
        /// <param name="screenSpacePos">The screen-space position where input could be received.</param>
        /// <returns>True if input is received at the given screen-space position.</returns>
        public virtual bool ReceivePositionalInputAt(Vector2 screenSpacePos) => Contains(screenSpacePos);

        /// <summary>
        /// Computes whether a given screen-space position is contained within this drawable.
        /// Mouse input events are only received when this function is true, or when the drawable
        /// is in focus.
        /// </summary>
        /// <param name="screenSpacePos">The screen space position to be checked against this drawable.</param>
        public virtual bool Contains(Vector2 screenSpacePos) => DrawRectangle.Contains(ToLocalSpace(screenSpacePos));

        /// <summary>
        /// Whether non-positional input should be propagated to the sub-tree rooted at this drawable.
        /// </summary>
        public virtual bool PropagateNonPositionalInputSubTree => IsPresent && RequestsNonPositionalInputSubTree;

        /// <summary>
        /// Whether positional input should be propagated to the sub-tree rooted at this drawable.
        /// </summary>
        public virtual bool PropagatePositionalInputSubTree => IsPresent && RequestsPositionalInputSubTree && !IsMaskedAway;

        /// <summary>
        /// Whether clicks should be blocked when this drawable is in a dragged state.
        /// </summary>
        /// <remarks>
        /// This is queried when a click is to be actuated.
        /// </remarks>
        public virtual bool DragBlocksClick => true;

        /// <summary>
        /// This method is responsible for building a queue of Drawables to receive non-positional input in reverse order.
        /// </summary>
        /// <param name="queue">The input queue to be built.</param>
        /// <param name="allowBlocking">Whether blocking at <see cref="PassThroughInputManager"/>s should be allowed.</param>
        /// <returns>Returns false if we should skip this sub-tree.</returns>
        internal virtual bool BuildNonPositionalInputQueue(List<Drawable> queue, bool allowBlocking = true)
        {
            if (!PropagateNonPositionalInputSubTree)
                return false;

            if (HandleNonPositionalInput)
                queue.Add(this);

            return true;
        }

        /// <summary>
        /// This method is responsible for building a queue of Drawables to receive positional input in reverse order.
        /// </summary>
        /// <param name="screenSpacePos">The screen space position of the positional input.</param>
        /// <param name="queue">The input queue to be built.</param>
        /// <returns>Returns false if we should skip this sub-tree.</returns>
        internal virtual bool BuildPositionalInputQueue(Vector2 screenSpacePos, List<Drawable> queue)
        {
            if (!PropagatePositionalInputSubTree)
                return false;

            if (HandlePositionalInput && ReceivePositionalInputAt(screenSpacePos))
                queue.Add(this);

            return true;
        }

        internal sealed override void EnsureTransformMutationAllowed() => EnsureMutationAllowed($"mutate the {nameof(Transforms)}");

        /// <summary>
        /// Check whether the current thread is valid for operating on thread-safe properties.
        /// </summary>
        /// <param name="action">The action to be performed, used only for describing failures in exception messages.</param>
        /// <exception cref="InvalidThreadForMutationException">If the current thread is not valid.</exception>
        internal void EnsureMutationAllowed(string action)
        {
            switch (LoadState)
            {
                case LoadState.NotLoaded:
                    break;

                case LoadState.Loading:
                    if (Thread.CurrentThread != LoadThread)
                        throw new InvalidThreadForMutationException(LoadState, action, "not on the load thread");

                    break;

                case LoadState.Ready:
                    // Allow mutating from the load thread since parenting containers may still be in the loading state
                    if (Thread.CurrentThread != LoadThread && !ThreadSafety.IsUpdateThread)
                        throw new InvalidThreadForMutationException(LoadState, action, "not on the load or update threads");

                    break;

                case LoadState.Loaded:
                    if (!ThreadSafety.IsUpdateThread)
                        throw new InvalidThreadForMutationException(LoadState, action, "not on the update thread");

                    break;
            }
        }

        #endregion

        #region Transforms

        protected internal ScheduledDelegate Schedule<T>(Action<T> action, T data)
        {
            if (TransformDelay > 0)
                return Scheduler.AddDelayed(action, data, TransformDelay);

            return Scheduler.Add(action, data);
        }

        protected internal ScheduledDelegate Schedule(Action action)
        {
            if (TransformDelay > 0)
                return Scheduler.AddDelayed(action, TransformDelay);

            return Scheduler.Add(action);
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

            LifetimeEnd = LatestTransformEndTime;

            if (calculateLifetimeStart)
            {
                double min = double.MaxValue;

                foreach (Transform t in Transforms)
                {
                    if (t.StartTime < min)
                        min = t.StartTime;
                }

                LifetimeStart = min < int.MaxValue ? min : int.MinValue;
            }
        }

        /// <summary>
        /// Hide sprite instantly.
        /// </summary>
        public virtual void Hide() => this.FadeOut();

        /// <summary>
        /// Show sprite instantly.
        /// </summary>
        public virtual void Show() => this.FadeIn();

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
                return $@"{Name}({shortClass})";
            else
                return shortClass;
        }

        /// <summary>
        /// Creates a new instance of an empty <see cref="Drawable"/>.
        /// </summary>
        public static Drawable Empty() => new EmptyDrawable();

        private class EmptyDrawable : Drawable
        {
        }

        public class InvalidThreadForMutationException : InvalidOperationException
        {
            public InvalidThreadForMutationException(LoadState loadState, string action, string invalidThreadContextDescription)
                : base($"Cannot {action} on a {loadState} {nameof(Drawable)} while {invalidThreadContextDescription}. "
                       + $"Consider using {nameof(Schedule)} to schedule the mutation operation.")
            {
            }
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
        DrawInfo = 1,

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
        /// <see cref="Drawable.Colour"/> has changed.
        /// </summary>
        Colour = 1 << 3,

        /// <summary>
        /// <see cref="Graphics.DrawNode.ApplyState"/> has to be invoked on all old draw nodes.
        /// This <see cref="Invalidation"/> flag never propagates to children.
        /// </summary>
        DrawNode = 1 << 4,

        /// <summary>
        /// <see cref="Drawable.IsPresent"/> has changed.
        /// </summary>
        Presence = 1 << 5,

        /// <summary>
        /// A <see cref="Drawable.Parent"/> has changed.
        /// Unlike other <see cref="Invalidation"/> flags, this propagates to all children regardless of their <see cref="Drawable.IsAlive"/> state.
        /// </summary>
        Parent = 1 << 6,

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
        All = DrawNode | RequiredParentSizeToFit | Colour | DrawInfo | Presence,

        /// <summary>
        /// Only the layout flags.
        /// </summary>
        Layout = All & ~(DrawNode | Parent)
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
        y0 = 1,

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

        X = 1,
        Y = 1 << 1,

        Both = X | Y,
    }

    [Flags]
    public enum Edges
    {
        None = 0,

        Top = 1,
        Left = 1 << 1,
        Bottom = 1 << 2,
        Right = 1 << 3,

        Horizontal = Left | Right,
        Vertical = Top | Bottom,

        All = Top | Left | Bottom | Right,
    }

    public enum Direction
    {
        Horizontal,
        Vertical,
    }

    public enum RotationDirection
    {
        [Description("Clockwise")]
        Clockwise,

        [Description("Counterclockwise")]
        Counterclockwise,
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
        /// Currently loading (possibly and usually on a background thread via <see cref="CompositeDrawable.LoadComponentAsync{TLoadable}"/>).
        /// </summary>
        Loading,

        /// <summary>
        /// Loading is complete, but has not yet been finalized on the update thread
        /// (<see cref="Drawable.LoadComplete"/> has not been called yet, which
        /// always runs on the update thread and requires <see cref="Drawable.IsAlive"/>).
        /// </summary>
        Ready,

        /// <summary>
        /// Loading is fully completed and the Drawable is now part of the scene graph.
        /// </summary>
        Loaded
    }

    /// <summary>
    /// Controls the behavior of <see cref="Drawable.RelativeSizeAxes"/> when it is set to <see cref="Axes.Both"/>.
    /// </summary>
    public enum FillMode
    {
        /// <summary>
        /// Completely fill the parent with a relative size of 1 at the cost of stretching the aspect ratio (default).
        /// </summary>
        Stretch,

        /// <summary>
        /// Always maintains aspect ratio while filling the portion of the parent's size denoted by the relative size.
        /// A relative size of 1 results in completely filling the parent by scaling the smaller axis of the drawable to fill the parent.
        /// </summary>
        Fill,

        /// <summary>
        /// Always maintains aspect ratio while fitting into the portion of the parent's size denoted by the relative size.
        /// A relative size of 1 results in fitting exactly into the parent by scaling the larger axis of the drawable to fit into the parent.
        /// </summary>
        Fit,
    }
}
