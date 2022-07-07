// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using osu.Framework.Lists;
using System.Collections.Generic;
using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Threading;
using osuTK;
using osuTK.Graphics;
using osu.Framework.Graphics.Shaders;
using osu.Framework.Extensions.IEnumerableExtensions;
using osu.Framework.Graphics.Colour;
using osu.Framework.Allocation;
using osu.Framework.Graphics.Transforms;
using osu.Framework.Timing;
using osu.Framework.Threading;
using osu.Framework.Statistics;
using System.Threading.Tasks;
using JetBrains.Annotations;
using osu.Framework.Development;
using osu.Framework.Extensions.EnumExtensions;
using osu.Framework.Extensions.ExceptionExtensions;
using osu.Framework.Graphics.Effects;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Layout;
using osu.Framework.Logging;
using osu.Framework.Testing;
using osu.Framework.Utils;

namespace osu.Framework.Graphics.Containers
{
    /// <summary>
    /// A drawable consisting of a composite of child drawables which are
    /// manages by the composite object itself. Transformations applied to
    /// a <see cref="CompositeDrawable"/> are also applied to its children.
    /// Additionally, <see cref="CompositeDrawable"/>s support various effects, such as masking, edge effect,
    /// padding, and automatic sizing depending on their children.
    /// </summary>
    [ExcludeFromDynamicCompile]
    public abstract partial class CompositeDrawable : Drawable
    {
        #region Construction and disposal

        /// <summary>
        /// Constructs a <see cref="CompositeDrawable"/> that stores children.
        /// </summary>
        protected CompositeDrawable()
        {
            var childComparer = new ChildComparer(this);

            internalChildren = new SortedList<Drawable>(childComparer);
            aliveInternalChildren = new SortedList<Drawable>(childComparer);

            AddLayout(childrenSizeDependencies);
        }

        [Resolved]
        private Game game { get; set; }

        /// <summary>
        /// Create a local dependency container which will be used by our nested children.
        /// If not overridden, the load-time parent's dependency tree will be used.
        /// </summary>
        /// <param name="parent">The parent <see cref="IReadOnlyDependencyContainer"/> which should be passed through if we want fallback lookups to work.</param>
        /// <returns>A new dependency container to be stored for this Drawable.</returns>
        protected virtual IReadOnlyDependencyContainer CreateChildDependencies(IReadOnlyDependencyContainer parent) => DependencyActivator.MergeDependencies(this, parent);

        /// <summary>
        /// Contains all dependencies that can be injected into this CompositeDrawable's children using <see cref="BackgroundDependencyLoaderAttribute"/>.
        /// Add or override dependencies by calling <see cref="DependencyContainer.Cache(object)"/>.
        /// </summary>
        public IReadOnlyDependencyContainer Dependencies { get; private set; }

        protected sealed override void InjectDependencies(IReadOnlyDependencyContainer dependencies)
        {
            // get our dependencies from our parent, but allow local overriding of our inherited dependency container
            Dependencies = CreateChildDependencies(dependencies);

            base.InjectDependencies(dependencies);
        }

        private CancellationTokenSource disposalCancellationSource;

        private WeakList<Drawable> loadingComponents;

        internal static readonly ThreadedTaskScheduler SCHEDULER_STANDARD = new ThreadedTaskScheduler(4, $"{nameof(LoadComponentsAsync)} (standard)");

        internal static readonly ThreadedTaskScheduler SCHEDULER_LONG_LOAD = new ThreadedTaskScheduler(4, $"{nameof(LoadComponentsAsync)} (long load)");

        /// <summary>
        /// Loads a future child or grand-child of this <see cref="CompositeDrawable"/> asynchronously. <see cref="Dependencies"/>
        /// and <see cref="Drawable.Clock"/> are inherited from this <see cref="CompositeDrawable"/>.
        ///
        /// Note that this will always use the dependencies and clock from this instance. If you must load to a nested container level,
        /// consider using <see cref="DelayedLoadWrapper"/>
        /// </summary>
        /// <typeparam name="TLoadable">The type of the future future child or grand-child to be loaded.</typeparam>
        /// <param name="component">The child or grand-child to be loaded.</param>
        /// <param name="onLoaded">Callback to be invoked on the update thread after loading is complete.</param>
        /// <param name="cancellation">An optional cancellation token.</param>
        /// <param name="scheduler">The scheduler for <paramref name="onLoaded"/> to be invoked on. If null, the local scheduler will be used.</param>
        /// <returns>The task which is used for loading and callbacks.</returns>
        protected internal Task LoadComponentAsync<TLoadable>([NotNull] TLoadable component, Action<TLoadable> onLoaded = null, CancellationToken cancellation = default, Scheduler scheduler = null)
            where TLoadable : Drawable
        {
            if (component == null) throw new ArgumentNullException(nameof(component));

            return LoadComponentsAsync(component.Yield(), l => onLoaded?.Invoke(l.Single()), cancellation, scheduler);
        }

        /// <summary>
        /// Loads a future child or grand-child of this <see cref="CompositeDrawable"/> synchronously and immediately. <see cref="Dependencies"/>
        /// and <see cref="Drawable.Clock"/> are inherited from this <see cref="CompositeDrawable"/>.
        /// <remarks>
        /// This is generally useful if already in an asynchronous context and requiring forcefully (pre)loading content without adding it to the hierarchy.
        /// </remarks>
        /// </summary>
        /// <typeparam name="TLoadable">The type of the future future child or grand-child to be loaded.</typeparam>
        /// <param name="component">The child or grand-child to be loaded.</param>
        protected void LoadComponent<TLoadable>(TLoadable component) where TLoadable : Drawable
            => LoadComponents(component.Yield());

        /// <summary>
        /// Loads several future child or grand-child of this <see cref="CompositeDrawable"/> asynchronously. <see cref="Dependencies"/>
        /// and <see cref="Drawable.Clock"/> are inherited from this <see cref="CompositeDrawable"/>.
        ///
        /// Note that this will always use the dependencies and clock from this instance. If you must load to a nested container level,
        /// consider using <see cref="DelayedLoadWrapper"/>
        /// </summary>
        /// <typeparam name="TLoadable">The type of the future future child or grand-child to be loaded.</typeparam>
        /// <param name="components">The children or grand-children to be loaded.</param>
        /// <param name="onLoaded">Callback to be invoked on the update thread after loading is complete.</param>
        /// <param name="cancellation">An optional cancellation token.</param>
        /// <param name="scheduler">The scheduler for <paramref name="onLoaded"/> to be invoked on. If null, the local scheduler will be used.</param>
        /// <returns>The task which is used for loading and callbacks.</returns>
        protected internal Task LoadComponentsAsync<TLoadable>(IEnumerable<TLoadable> components, Action<IEnumerable<TLoadable>> onLoaded = null, CancellationToken cancellation = default,
                                                               Scheduler scheduler = null)
            where TLoadable : Drawable
        {
            if (game == null)
                throw new InvalidOperationException($"May not invoke {nameof(LoadComponentAsync)} prior to this {nameof(CompositeDrawable)} being loaded.");

            EnsureMutationAllowed($"load components via {nameof(LoadComponentsAsync)}");

            if (IsDisposed)
                throw new ObjectDisposedException(ToString());

            disposalCancellationSource ??= new CancellationTokenSource();

            var linkedSource = CancellationTokenSource.CreateLinkedTokenSource(disposalCancellationSource.Token, cancellation);

            var deps = new DependencyContainer(Dependencies);
            deps.CacheValueAs(linkedSource.Token);

            loadingComponents ??= new WeakList<Drawable>();

            var loadables = components.ToList();

            foreach (var d in loadables)
            {
                loadingComponents.Add(d);
                LoadingComponentsLogger.Add(d);

                d.OnLoadComplete += _ =>
                {
                    loadingComponents.Remove(d);
                    LoadingComponentsLogger.Remove(d);
                };
            }

            var taskScheduler = loadables.Any(c => c.IsLongRunning) ? SCHEDULER_LONG_LOAD : SCHEDULER_STANDARD;

            return Task.Factory.StartNew(() => loadComponents(loadables, deps, true, linkedSource.Token), linkedSource.Token, TaskCreationOptions.HideScheduler, taskScheduler).ContinueWith(loaded =>
            {
                var exception = loaded.Exception?.AsSingular();

                if (loadables.Count == 0)
                    return;

                if (linkedSource.Token.IsCancellationRequested)
                {
                    // In the case of cancellation the final load state will not be reached, so cleanup here is required.
                    foreach (var d in loadables)
                        LoadingComponentsLogger.Remove(d);

                    linkedSource.Dispose();
                    return;
                }

                (scheduler ?? Scheduler).Add(() =>
                {
                    try
                    {
                        if (exception != null)
                            ExceptionDispatchInfo.Capture(exception).Throw();

                        if (!linkedSource.Token.IsCancellationRequested)
                            onLoaded?.Invoke(loadables);
                    }
                    finally
                    {
                        linkedSource.Dispose();
                    }
                });
            }, TaskContinuationOptions.ExecuteSynchronously);
        }

        /// <summary>
        /// Loads several future child or grand-child of this <see cref="CompositeDrawable"/> synchronously and immediately. <see cref="Dependencies"/>
        /// and <see cref="Drawable.Clock"/> are inherited from this <see cref="CompositeDrawable"/>.
        /// <remarks>
        /// This is generally useful if already in an asynchronous context and requiring forcefully (pre)loading content without adding it to the hierarchy.
        /// </remarks>
        /// </summary>
        /// <typeparam name="TLoadable">The type of the future future child or grand-child to be loaded.</typeparam>
        /// <param name="components">The children or grand-children to be loaded.</param>
        protected void LoadComponents<TLoadable>(IEnumerable<TLoadable> components) where TLoadable : Drawable
        {
            if (game == null)
                throw new InvalidOperationException($"May not invoke {nameof(LoadComponent)} prior to this {nameof(CompositeDrawable)} being loaded.");

            if (IsDisposed)
                throw new ObjectDisposedException(ToString());

            loadComponents(components.ToList(), Dependencies, false);
        }

        /// <summary>
        /// Load the provided components. Any components which could not be loaded will be removed from the provided list.
        /// </summary>
        private void loadComponents<TLoadable>(List<TLoadable> components, IReadOnlyDependencyContainer dependencies, bool isDirectAsyncContext, CancellationToken cancellation = default)
            where TLoadable : Drawable
        {
            for (int i = 0; i < components.Count; i++)
            {
                if (cancellation.IsCancellationRequested)
                    break;

                if (!components[i].LoadFromAsync(Clock, dependencies, isDirectAsyncContext))
                {
                    LoadingComponentsLogger.Remove(components[i]);
                    components.Remove(components[i--]);
                }
            }
        }

        [BackgroundDependencyLoader(true)]
        private void load(ShaderManager shaders, CancellationToken? cancellation)
        {
            hasCustomDrawNode = GetType().GetMethod(nameof(CreateDrawNode))?.DeclaringType != typeof(CompositeDrawable);

            Shader ??= shaders?.Load(VertexShaderDescriptor.TEXTURE_2, FragmentShaderDescriptor.TEXTURE_ROUNDED);

            // We are in a potentially async context, so let's aggressively load all our children
            // regardless of their alive state. this also gives children a clock so they can be checked
            // for their correct alive state in the case LifetimeStart is set to a definite value.
            foreach (var c in internalChildren)
            {
                cancellation?.ThrowIfCancellationRequested();
                loadChild(c);
            }
        }

        protected override void LoadAsyncComplete()
        {
            base.LoadAsyncComplete();

            // At this point we can assume that we are loaded although we're not in the "ready" state, because we'll be given
            // a "ready" state soon after this method terminates. Therefore we can perform an early check to add any alive children
            // while we're still in an asynchronous context and avoid putting pressure on the main thread during UpdateSubTree.
            CheckChildrenLife();
        }

        /// <summary>
        /// Loads a <see cref="Drawable"/> child. This will not throw in the event of the load being cancelled.
        /// </summary>
        /// <param name="child">The <see cref="Drawable"/> child to load.</param>
        private void loadChild(Drawable child)
        {
            try
            {
                if (IsDisposed)
                    throw new ObjectDisposedException(ToString(), "Disposed drawables may not have children added.");

                child.Load(Clock, Dependencies, false);

                child.Parent = this;
            }
            catch (OperationCanceledException)
            {
            }
            catch (AggregateException ae)
            {
                foreach (var e in ae.Flatten().InnerExceptions)
                {
                    if (e is OperationCanceledException)
                        continue;

                    ExceptionDispatchInfo.Capture(e).Throw();
                }
            }
        }

        protected override void Dispose(bool isDisposing)
        {
            if (IsDisposed)
                return;

            disposalCancellationSource?.Cancel();
            disposalCancellationSource?.Dispose();

            InternalChildren?.ForEach(c => c.Dispose());

            if (loadingComponents != null)
            {
                foreach (var d in loadingComponents)
                {
                    d.Dispose();
                    LoadingComponentsLogger.Remove(d);
                }
            }

            OnAutoSize = null;
            Dependencies = null;
            schedulerAfterChildren = null;

            base.Dispose(isDisposing);
        }

        #endregion

        #region Children management

        /// <summary>
        /// Invoked when a child has entered <see cref="AliveInternalChildren"/>.
        /// </summary>
        internal event Action<Drawable> ChildBecameAlive;

        /// <summary>
        /// Invoked when a child has left <see cref="AliveInternalChildren"/>.
        /// </summary>
        internal event Action<Drawable> ChildDied;

        /// <summary>
        /// Fired after a child's <see cref="Drawable.Depth"/> is changed.
        /// </summary>
        internal event Action<Drawable> ChildDepthChanged;

        /// <summary>
        /// Gets or sets the only child in <see cref="InternalChildren"/>.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        protected internal Drawable InternalChild
        {
            get
            {
                if (InternalChildren.Count != 1)
                {
                    throw new InvalidOperationException(
                        $"Cannot call {nameof(InternalChild)} unless there's exactly one {nameof(Drawable)} in {nameof(InternalChildren)} (currently {InternalChildren.Count})!");
                }

                return InternalChildren[0];
            }
            set
            {
                if (IsDisposed)
                    throw new ObjectDisposedException(ToString(), "Disposed drawables may not have children set.");

                ClearInternal();
                AddInternal(value);
            }
        }

        protected class ChildComparer : IComparer<Drawable>
        {
            private readonly CompositeDrawable owner;

            public ChildComparer(CompositeDrawable owner)
            {
                this.owner = owner;
            }

            public int Compare(Drawable x, Drawable y) => owner.Compare(x, y);
        }

        /// <summary>
        /// Compares two <see cref="InternalChildren"/> to determine their sorting.
        /// </summary>
        /// <param name="x">The first child to compare.</param>
        /// <param name="y">The second child to compare.</param>
        /// <returns>-1 if <paramref name="x"/> comes before <paramref name="y"/>, and 1 otherwise.</returns>
        protected virtual int Compare(Drawable x, Drawable y)
        {
            if (x == null) throw new ArgumentNullException(nameof(x));
            if (y == null) throw new ArgumentNullException(nameof(y));

            int i = y.Depth.CompareTo(x.Depth);
            if (i != 0) return i;

            return x.ChildID.CompareTo(y.ChildID);
        }

        /// <summary>
        /// Helper method comparing children by their depth first, and then by their reversed child ID.
        /// </summary>
        /// <param name="x">The first child to compare.</param>
        /// <param name="y">The second child to compare.</param>
        /// <returns>-1 if <paramref name="x"/> comes before <paramref name="y"/>, and 1 otherwise.</returns>
        protected int CompareReverseChildID(Drawable x, Drawable y)
        {
            if (x == null) throw new ArgumentNullException(nameof(x));
            if (y == null) throw new ArgumentNullException(nameof(y));

            int i = y.Depth.CompareTo(x.Depth);
            if (i != 0) return i;

            return y.ChildID.CompareTo(x.ChildID);
        }

        private readonly SortedList<Drawable> internalChildren;

        /// <summary>
        /// This <see cref="CompositeDrawable"/> list of children. Assigning to this property will dispose all existing children of this <see cref="CompositeDrawable"/>.
        /// </summary>
        protected internal IReadOnlyList<Drawable> InternalChildren
        {
            get => internalChildren;
            set => InternalChildrenEnumerable = value;
        }

        /// <summary>
        /// Replaces all internal children of this <see cref="CompositeDrawable"/> with the elements contained in the enumerable.
        /// </summary>
        protected internal IEnumerable<Drawable> InternalChildrenEnumerable
        {
            set
            {
                if (IsDisposed)
                    throw new ObjectDisposedException(ToString(), "Children cannot be mutated on a disposed drawable.");

                ClearInternal();
                AddRangeInternal(value);
            }
        }

        private readonly SortedList<Drawable> aliveInternalChildren;
        protected internal IReadOnlyList<Drawable> AliveInternalChildren => aliveInternalChildren;

        /// <summary>
        /// The index of a given child within <see cref="InternalChildren"/>.
        /// </summary>
        /// <returns>
        /// If the child is found, its index. Otherwise, the negated index it would obtain
        /// if it were added to <see cref="InternalChildren"/>.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// <list type="bullet">
        /// <item>If the supplied <paramref name="drawable"/> is already attached to another <see cref="Drawable.Parent"/>.</item>
        /// <item>If a child drawable was matched using <see cref="Compare"/>, but that child drawable was not the supplied <paramref name="drawable"/>.</item>
        /// </list>
        /// </exception>
        protected internal int IndexOfInternal(Drawable drawable)
        {
            if (drawable.Parent != null && drawable.Parent != this)
                throw new InvalidOperationException($@"Cannot call {nameof(IndexOfInternal)} for a drawable that already is a child of a different parent.");

            int index = internalChildren.IndexOf(drawable);

            if (index >= 0 && internalChildren[index].ChildID != drawable.ChildID)
                throw new InvalidOperationException($@"A non-matching {nameof(Drawable)} was returned. Please ensure {GetType()}'s {nameof(Compare)} override implements a stable sort algorithm.");

            return index;
        }

        /// <summary>
        /// Checks whether a given child is contained within <see cref="InternalChildren"/>.
        /// </summary>
        protected internal bool ContainsInternal(Drawable drawable) => IndexOfInternal(drawable) >= 0;

        /// <summary>
        /// Removes a given child from this <see cref="InternalChildren"/>.
        /// </summary>
        /// <param name="drawable">The <see cref="Drawable"/> to be removed.</param>
        /// <returns>False if <paramref name="drawable"/> was not a child of this <see cref="CompositeDrawable"/> and true otherwise.</returns>
        protected internal virtual bool RemoveInternal(Drawable drawable)
        {
            EnsureChildMutationAllowed();

            if (drawable == null)
                throw new ArgumentNullException(nameof(drawable));

            int index = IndexOfInternal(drawable);
            if (index < 0)
                return false;

            internalChildren.RemoveAt(index);

            if (drawable.IsAlive)
            {
                aliveInternalChildren.Remove(drawable);
                ChildDied?.Invoke(drawable);
            }

            if (drawable.LoadState >= LoadState.Ready && drawable.Parent != this)
                throw new InvalidOperationException($@"Removed a drawable ({drawable}) whose parent was not this ({this}), but {drawable.Parent}.");

            drawable.Parent = null;
            drawable.IsAlive = false;

            if (AutoSizeAxes != Axes.None)
                Invalidate(Invalidation.RequiredParentSizeToFit, InvalidationSource.Child);

            return true;
        }

        /// <summary>
        /// Clear all of <see cref="InternalChildren"/>.
        /// </summary>
        /// <param name="disposeChildren">
        /// Whether removed children should also get disposed.
        /// Disposal will be recursive.
        /// </param>
        protected internal virtual void ClearInternal(bool disposeChildren = true)
        {
            EnsureChildMutationAllowed();

            if (IsDisposed)
                throw new ObjectDisposedException(ToString(), "Children cannot be cleared on a disposed drawable.");

            if (internalChildren.Count == 0) return;

            foreach (Drawable t in internalChildren)
            {
                if (t.IsAlive)
                    ChildDied?.Invoke(t);

                t.IsAlive = false;
                t.Parent = null;

                if (disposeChildren)
                    DisposeChildAsync(t);

                Trace.Assert(t.Parent == null);
            }

            internalChildren.Clear();
            aliveInternalChildren.Clear();
            RequestsNonPositionalInputSubTree = RequestsNonPositionalInput;
            RequestsPositionalInputSubTree = RequestsPositionalInput;

            if (AutoSizeAxes != Axes.None)
                Invalidate(Invalidation.RequiredParentSizeToFit, InvalidationSource.Child);
        }

        /// <summary>
        /// Used to assign a monotonically increasing ID to children as they are added. This member is
        /// incremented whenever a child is added.
        /// </summary>
        private ulong currentChildID;

        /// <summary>
        /// Adds a child to <see cref="InternalChildren"/>.
        /// </summary>
        protected internal virtual void AddInternal(Drawable drawable)
        {
            EnsureChildMutationAllowed();

            if (IsDisposed)
                throw new ObjectDisposedException(ToString(), "Children cannot be mutated on a disposed drawable.");

            if (drawable == null)
                throw new ArgumentNullException(nameof(drawable), $"null {nameof(Drawable)}s may not be added to {nameof(CompositeDrawable)}.");

            if (drawable == this)
                throw new InvalidOperationException($"{nameof(CompositeDrawable)} may not be added to itself.");

            // If the drawable's ChildId is not zero, then it was added to another parent even if it wasn't loaded
            if (drawable.ChildID != 0)
                throw new InvalidOperationException("May not add a drawable to multiple containers.");

            drawable.ChildID = ++currentChildID;
            drawable.RemoveCompletedTransforms = RemoveCompletedTransforms;

            if (LoadState >= LoadState.Loading)
            {
                // If we're already loaded, we can eagerly allow children to be loaded

                if (drawable.LoadState >= LoadState.Ready)
                    drawable.Parent = this;
                else
                    loadChild(drawable);
            }

            internalChildren.Add(drawable);

            if (AutoSizeAxes != Axes.None)
                Invalidate(Invalidation.RequiredParentSizeToFit, InvalidationSource.Child);
        }

        /// <summary>
        /// Adds a range of children to <see cref="InternalChildren"/>. This is equivalent to calling
        /// <see cref="AddInternal(Drawable)"/> on each element of the range in order.
        /// </summary>
        protected internal void AddRangeInternal(IEnumerable<Drawable> range)
        {
            if (range is IContainerEnumerable<Drawable>)
            {
                throw new InvalidOperationException($"Attempting to add a {nameof(IContainer)} as a range of children to {this}."
                                                    + $"If intentional, consider using the {nameof(IContainerEnumerable<Drawable>.Children)} property instead.");
            }

            foreach (Drawable d in range)
                AddInternal(d);
        }

        /// <summary>
        /// Changes the depth of an internal child. This affects ordering of <see cref="InternalChildren"/>.
        /// </summary>
        /// <param name="child">The child whose depth is to be changed.</param>
        /// <param name="newDepth">The new depth value to be set.</param>
        protected internal void ChangeInternalChildDepth(Drawable child, float newDepth)
        {
            EnsureChildMutationAllowed();

            if (child.Depth == newDepth) return;

            int index = IndexOfInternal(child);
            if (index < 0)
                throw new InvalidOperationException($"Can not change depth of drawable which is not contained within this {nameof(CompositeDrawable)}.");

            internalChildren.RemoveAt(index);
            int aliveIndex = aliveInternalChildren.IndexOf(child);
            if (aliveIndex >= 0) // remove if found
                aliveInternalChildren.RemoveAt(aliveIndex);

            ulong chId = child.ChildID;
            child.ChildID = 0; // ensure Depth-change does not throw an exception
            child.Depth = newDepth;
            child.ChildID = chId;

            internalChildren.Add(child);
            if (aliveIndex >= 0) // re-add if it used to be in aliveInternalChildren
                aliveInternalChildren.Add(child);

            ChildDepthChanged?.Invoke(child);
        }

        /// <summary>
        /// Sorts all children of this <see cref="CompositeDrawable"/>.
        /// </summary>
        /// <remarks>
        /// This can be used to re-sort the children if the result of <see cref="Compare"/> has changed.
        /// </remarks>
        protected internal void SortInternal()
        {
            EnsureChildMutationAllowed();

            internalChildren.Sort();
            aliveInternalChildren.Sort();
        }

        #endregion

        #region Updating (per-frame periodic)

        private Scheduler schedulerAfterChildren;

        /// <summary>
        /// A lazily-initialized scheduler used to schedule tasks to be invoked in future <see cref="UpdateAfterChildren"/>s calls.
        /// The tasks are invoked at the beginning of the <see cref="UpdateAfterChildren"/> method before anything else.
        /// </summary>
        protected internal Scheduler SchedulerAfterChildren
        {
            get
            {
                if (schedulerAfterChildren != null)
                    return schedulerAfterChildren;

                lock (LoadLock)
                    return schedulerAfterChildren ??= new Scheduler(() => ThreadSafety.IsUpdateThread, Clock);
            }
        }

        /// <summary>
        /// Updates the life status of <see cref="InternalChildren"/> according to their
        /// <see cref="Drawable.ShouldBeAlive"/> property.
        /// </summary>
        /// <returns>True iff the life status of at least one child changed.</returns>
        protected virtual bool UpdateChildrenLife()
        {
            // Can not have alive children if we are not loaded.
            if (LoadState < LoadState.Ready)
                return false;

            if (!CheckChildrenLife())
                return false;

            return true;
        }

        /// <summary>
        /// Checks whether the alive state of any child has changed and processes it. This will add or remove
        /// children from <see cref="aliveInternalChildren"/> depending on their alive states.
        /// <para>Note that this does NOT check the load state of this <see cref="CompositeDrawable"/> to check if it can hold any alive children.</para>
        /// </summary>
        /// <returns>Whether any child's alive state has changed.</returns>
        protected virtual bool CheckChildrenLife()
        {
            bool anyAliveChanged = false;

            for (int i = 0; i < internalChildren.Count; i++)
            {
                var state = checkChildLife(internalChildren[i]);

                anyAliveChanged |= state.HasFlagFast(ChildLifeStateChange.MadeAlive) || state.HasFlagFast(ChildLifeStateChange.MadeDead);

                if (state.HasFlagFast(ChildLifeStateChange.Removed))
                    i--;
            }

            FrameStatistics.Add(StatisticsCounterType.CCL, internalChildren.Count);

            return anyAliveChanged;
        }

        /// <summary>
        /// Checks whether the alive state of a child has changed and processes it. This will add or remove
        /// the child from <see cref="aliveInternalChildren"/> depending on its alive state.
        ///
        /// This should only ever be called on a <see cref="CompositeDrawable"/>'s own <see cref="internalChildren"/>.
        ///
        /// <para>Note that this does NOT check the load state of this <see cref="CompositeDrawable"/> to check if it can hold any alive children.</para>
        /// </summary>
        /// <param name="child">The child to check.</param>
        /// <returns>Whether the child's alive state has changed.</returns>
        private ChildLifeStateChange checkChildLife(Drawable child)
        {
            ChildLifeStateChange state = ChildLifeStateChange.None;

            if (child.ShouldBeAlive)
            {
                if (!child.IsAlive)
                {
                    if (child.LoadState < LoadState.Ready)
                    {
                        // If we're already loaded, we can eagerly allow children to be loaded
                        loadChild(child);
                        if (child.LoadState < LoadState.Ready)
                            return ChildLifeStateChange.None;
                    }

                    MakeChildAlive(child);
                    state = ChildLifeStateChange.MadeAlive;
                }
            }
            else
            {
                if (child.IsAlive || child.RemoveWhenNotAlive)
                {
                    if (MakeChildDead(child))
                        state |= ChildLifeStateChange.Removed;

                    state |= ChildLifeStateChange.MadeDead;
                }
            }

            return state;
        }

        [Flags]
        private enum ChildLifeStateChange
        {
            None = 0,
            MadeAlive = 1,
            MadeDead = 1 << 1,
            Removed = 1 << 2,
        }

        /// <summary>
        /// Makes a child alive.
        /// </summary>
        /// <remarks>
        /// Callers have to ensure that <paramref name="child"/> is of this <see cref="CompositeDrawable"/>'s non-alive <see cref="InternalChildren"/> and <see cref="LoadState"/> of the <paramref name="child"/> is at least <see cref="LoadState.Ready"/>.
        /// </remarks>
        /// <param name="child">The child of this <see cref="CompositeDrawable"/>> to make alive.</param>
        protected void MakeChildAlive(Drawable child)
        {
            Debug.Assert(!child.IsAlive && child.LoadState >= LoadState.Ready);

            // If the new child has the flag set, we should propagate the flag towards the root.
            // We can stop at the ancestor which has the flag already set because further ancestors will also have the flag set.
            if (child.RequestsNonPositionalInputSubTree)
            {
                for (var ancestor = this; ancestor != null && !ancestor.RequestsNonPositionalInputSubTree; ancestor = ancestor.Parent)
                    ancestor.RequestsNonPositionalInputSubTree = true;
            }

            if (child.RequestsPositionalInputSubTree)
            {
                for (var ancestor = this; ancestor != null && !ancestor.RequestsPositionalInputSubTree; ancestor = ancestor.Parent)
                    ancestor.RequestsPositionalInputSubTree = true;
            }

            aliveInternalChildren.Add(child);
            child.IsAlive = true;

            ChildBecameAlive?.Invoke(child);

            // Layout invalidations on non-alive children are blocked, so they must be invalidated once when they become alive.
            child.Invalidate(Invalidation.Layout, InvalidationSource.Parent);

            // Notify ourselves that a child has become alive.
            Invalidate(Invalidation.Presence, InvalidationSource.Child);
        }

        /// <summary>
        /// Makes a child dead (not alive) and removes it if <see cref="Drawable.RemoveWhenNotAlive"/> of the <paramref name="child"/> is set.
        /// </summary>
        /// <remarks>
        /// Callers have to ensure that <paramref name="child"/> is of this <see cref="CompositeDrawable"/>'s <see cref="AliveInternalChildren"/>.
        /// </remarks>
        /// <param name="child">The child of this <see cref="CompositeDrawable"/>> to make dead.</param>
        /// <returns>Whether <paramref name="child"/> has been removed by death.</returns>
        protected bool MakeChildDead(Drawable child)
        {
            if (child.IsAlive)
            {
                aliveInternalChildren.Remove(child);
                child.IsAlive = false;

                ChildDied?.Invoke(child);
            }

            bool removed = false;

            if (child.RemoveWhenNotAlive)
            {
                RemoveInternal(child);

                if (child.DisposeOnDeathRemoval)
                    DisposeChildAsync(child);

                removed = true;
            }

            // Notify ourselves that a child has died.
            Invalidate(Invalidation.Presence, InvalidationSource.Child);

            return removed;
        }

        internal override void UnbindAllBindablesSubTree()
        {
            base.UnbindAllBindablesSubTree();

            // TODO: this code can potentially be run from an update thread while a drawable is still loading (see ScreenStack as an example).
            // while this is quite a bad issue, it is rare and generally happens in tests which have frame perfect behaviours.
            // as such, for loop is used here intentionally to avoid collection modified exceptions for this (usually) non-critical failure.
            // see https://github.com/ppy/osu-framework/issues/4054.
            for (int i = 0; i < internalChildren.Count; i++)
            {
                Drawable child = internalChildren[i];
                child.UnbindAllBindablesSubTree();
            }
        }

        /// <summary>
        /// Unbinds a child's bindings synchronously and queues an asynchronous disposal of the child.
        /// </summary>
        /// <param name="drawable">The child to dispose.</param>
        internal void DisposeChildAsync(Drawable drawable)
        {
            drawable.UnbindAllBindablesSubTree();
            AsyncDisposalQueue.Enqueue(drawable);
        }

        internal override void UpdateClock(IFrameBasedClock clock)
        {
            if (Clock == clock)
                return;

            base.UpdateClock(clock);
            foreach (Drawable child in internalChildren)
                child.UpdateClock(Clock);

            schedulerAfterChildren?.UpdateClock(Clock);
        }

        /// <summary>
        /// Specifies whether this <see cref="CompositeDrawable"/> requires an update of its children.
        /// If the return value is false, then children are not updated and
        /// <see cref="UpdateAfterChildren"/> is not called.
        /// </summary>
        protected virtual bool RequiresChildrenUpdate => !IsMaskedAway || !childrenSizeDependencies.IsValid;

        public override bool UpdateSubTree()
        {
            if (!base.UpdateSubTree()) return false;

            // We update our children's life even if we are invisible.
            // Note, that this does not propagate down and may need
            // generalization in the future.
            UpdateChildrenLife();

            // If we are not present then there is never a reason to check
            // for children, as they should never affect our present status.
            if (!IsPresent || !RequiresChildrenUpdate) return false;

            UpdateAfterChildrenLife();

            if (TypePerformanceMonitor.Active)
            {
                for (int i = 0; i < aliveInternalChildren.Count; ++i)
                {
                    Drawable c = aliveInternalChildren[i];

                    TypePerformanceMonitor.BeginCollecting(c);
                    updateChild(c);
                    TypePerformanceMonitor.EndCollecting(c);
                }
            }
            else
            {
                for (int i = 0; i < aliveInternalChildren.Count; ++i)
                    updateChild(aliveInternalChildren[i]);
            }

            if (schedulerAfterChildren != null)
            {
                int amountScheduledTasks = schedulerAfterChildren.Update();
                FrameStatistics.Add(StatisticsCounterType.ScheduleInvk, amountScheduledTasks);
            }

            UpdateAfterChildren();

            updateChildrenSizeDependencies();
            UpdateAfterAutoSize();
            return true;
        }

        private void updateChild(Drawable c)
        {
            Debug.Assert(c.LoadState >= LoadState.Ready);
            c.UpdateSubTree();
        }

        /// <summary>
        /// Updates all masking calculations for this <see cref="CompositeDrawable"/> and its <see cref="AliveInternalChildren"/>.
        /// This occurs post-<see cref="UpdateSubTree"/> to ensure that all <see cref="Drawable"/> updates have taken place.
        /// </summary>
        /// <param name="source">The parent that triggered this update on this <see cref="Drawable"/>.</param>
        /// <param name="maskingBounds">The <see cref="RectangleF"/> that defines the masking bounds.</param>
        /// <returns>Whether masking calculations have taken place.</returns>
        public override bool UpdateSubTreeMasking(Drawable source, RectangleF maskingBounds)
        {
            if (!base.UpdateSubTreeMasking(source, maskingBounds))
                return false;

            if (IsMaskedAway)
                return true;

            if (aliveInternalChildren.Count == 0)
                return true;

            if (RequiresChildrenUpdate)
            {
                var childMaskingBounds = ComputeChildMaskingBounds(maskingBounds);

                for (int i = 0; i < aliveInternalChildren.Count; i++)
                    aliveInternalChildren[i].UpdateSubTreeMasking(this, childMaskingBounds);
            }

            return true;
        }

        protected override bool ComputeIsMaskedAway(RectangleF maskingBounds)
        {
            if (!CanBeFlattened)
                return base.ComputeIsMaskedAway(maskingBounds);

            // The masking check is overly expensive (requires creation of ScreenSpaceDrawQuad)
            // when only few children exist.
            return aliveInternalChildren.Count >= amount_children_required_for_masking_check && base.ComputeIsMaskedAway(maskingBounds);
        }

        /// <summary>
        /// Computes the <see cref="RectangleF"/> to be used as the masking bounds for all <see cref="AliveInternalChildren"/>.
        /// </summary>
        /// <param name="maskingBounds">The <see cref="RectangleF"/> that defines the masking bounds for this <see cref="CompositeDrawable"/>.</param>
        /// <returns>The <see cref="RectangleF"/> to be used as the masking bounds for <see cref="AliveInternalChildren"/>.</returns>
        protected virtual RectangleF ComputeChildMaskingBounds(RectangleF maskingBounds) => Masking ? RectangleF.Intersect(maskingBounds, ScreenSpaceDrawQuad.AABBFloat) : maskingBounds;

        /// <summary>
        /// Invoked after <see cref="UpdateChildrenLife"/> and <see cref="Drawable.IsPresent"/> state checks have taken place,
        /// but before <see cref="Drawable.UpdateSubTree"/> is invoked for all <see cref="InternalChildren"/>.
        /// This occurs after <see cref="Drawable.Update"/> has been invoked on this <see cref="CompositeDrawable"/>
        /// </summary>
        protected virtual void UpdateAfterChildrenLife()
        {
        }

        /// <summary>
        /// An opportunity to update state once-per-frame after <see cref="Drawable.Update"/> has been called
        /// for all <see cref="InternalChildren"/>.
        /// This is invoked prior to any autosize calculations of this <see cref="CompositeDrawable"/>.
        /// </summary>
        protected virtual void UpdateAfterChildren()
        {
        }

        /// <summary>
        /// Invoked after all autosize calculations have taken place.
        /// </summary>
        protected virtual void UpdateAfterAutoSize()
        {
        }

        #endregion

        #region Invalidation

        protected override bool OnInvalidate(Invalidation invalidation, InvalidationSource source)
        {
            bool anyInvalidated = base.OnInvalidate(invalidation, source);

            // Child invalidations should not propagate to other children.
            if (source == InvalidationSource.Child)
                return anyInvalidated;

            // DrawNode invalidations should not propagate to children.
            invalidation &= ~Invalidation.DrawNode;
            if (invalidation == Invalidation.None)
                return anyInvalidated;

            IReadOnlyList<Drawable> targetChildren = aliveInternalChildren;

            // Non-layout flags must be propagated to all children. As such, it is simplest + quickest to propagate all other relevant flags along with them.
            if ((invalidation & ~Invalidation.Layout) > 0)
                targetChildren = internalChildren;

            for (int i = 0; i < targetChildren.Count; ++i)
            {
                Drawable c = targetChildren[i];

                Invalidation childInvalidation = invalidation;
                if ((invalidation & Invalidation.RequiredParentSizeToFit) > 0)
                    childInvalidation |= Invalidation.DrawInfo;

                // Other geometry things like rotation, shearing, etc don't affect child properties.
                childInvalidation &= ~Invalidation.MiscGeometry;

                // Relative positioning can however affect child geometry.
                if (c.RelativePositionAxes != Axes.None && (invalidation & Invalidation.DrawSize) > 0)
                    childInvalidation |= Invalidation.MiscGeometry;

                // No draw size changes if relative size axes does not propagate it downward.
                if (c.RelativeSizeAxes == Axes.None)
                    childInvalidation &= ~Invalidation.DrawSize;

                anyInvalidated |= c.Invalidate(childInvalidation, InvalidationSource.Parent);
            }

            return anyInvalidated;
        }

        /// <summary>
        /// Invalidates the children size dependencies of this <see cref="CompositeDrawable"/> when a child's position or size changes.
        /// </summary>
        /// <param name="invalidation">The <see cref="Invalidation"/> to invalidate with.</param>
        /// <param name="axes">The position or size <see cref="Axes"/> that changed.</param>
        /// <param name="source">The source <see cref="Drawable"/>.</param>
        internal void InvalidateChildrenSizeDependencies(Invalidation invalidation, Axes axes, Drawable source)
        {
            // Store the current state of the children size dependencies.
            // This state may be restored later if the invalidation proved to be unnecessary.
            bool wasValid = childrenSizeDependencies.IsValid;

            // The invalidation still needs to occur as normal, since a derived CompositeDrawable may want to respond to children size invalidations.
            Invalidate(invalidation, InvalidationSource.Child);

            // If all the changed axes were bypassed and an invalidation occurred, the children size dependencies can immediately be
            // re-validated without a recomputation, as a recomputation would not change the auto-sized size.
            if (wasValid && (axes & source.BypassAutoSizeAxes) == axes)
                childrenSizeDependencies.Validate();
        }

        #endregion

        #region DrawNode

        private bool hasCustomDrawNode;

        internal IShader Shader { get; private set; }

        protected override DrawNode CreateDrawNode() => new CompositeDrawableDrawNode(this);

        private bool forceLocalVertexBatch;

        /// <summary>
        /// Whether to use a local vertex batch for rendering. If false, a parenting vertex batch will be used.
        /// </summary>
        public bool ForceLocalVertexBatch
        {
            get => forceLocalVertexBatch;
            protected set
            {
                if (forceLocalVertexBatch == value)
                    return;

                forceLocalVertexBatch = value;

                Invalidate(Invalidation.DrawNode);
            }
        }

        /// <summary>
        /// A flattened <see cref="CompositeDrawable"/> has its <see cref="DrawNode"/> merged into its parents'.
        /// In some cases, the <see cref="DrawNode"/> must always be generated and flattening should not occur.
        /// </summary>
        protected virtual bool CanBeFlattened =>
            // Masking composite draw nodes define the masking area for their children.
            !Masking
            // Proxied drawables have their DrawNodes drawn elsewhere in the scene graph.
            && !HasProxy
            // Custom draw nodes may provide custom drawing procedures.
            && !hasCustomDrawNode
            // Composites with local vertex batches require their own draw node.
            && !ForceLocalVertexBatch;

        private const int amount_children_required_for_masking_check = 2;

        /// <summary>
        /// This function adds all children's <see cref="DrawNode"/>s to a target List, flattening the children of certain types
        /// of <see cref="CompositeDrawable"/> subtrees for optimization purposes.
        /// </summary>
        /// <param name="frame">The frame which <see cref="DrawNode"/>s should be generated for.</param>
        /// <param name="treeIndex">The index of the currently in-use <see cref="DrawNode"/> tree.</param>
        /// <param name="forceNewDrawNode">Whether the creation of a new <see cref="DrawNode"/> should be forced, rather than re-using an existing <see cref="DrawNode"/>.</param>
        /// <param name="j">The running index into the target List.</param>
        /// <param name="parentComposite">The <see cref="CompositeDrawable"/> whose children's <see cref="DrawNode"/>s to add.</param>
        /// <param name="target">The target list to fill with DrawNodes.</param>
        private static void addFromComposite(ulong frame, int treeIndex, bool forceNewDrawNode, ref int j, CompositeDrawable parentComposite, List<DrawNode> target)
        {
            SortedList<Drawable> children = parentComposite.aliveInternalChildren;

            for (int i = 0; i < children.Count; ++i)
            {
                Drawable drawable = children[i];

                if (!drawable.IsLoaded)
                    continue;

                if (!drawable.IsProxy)
                {
                    if (!drawable.IsPresent)
                        continue;

                    if (drawable.IsMaskedAway)
                        continue;

                    CompositeDrawable composite = drawable as CompositeDrawable;

                    if (composite?.CanBeFlattened == true)
                    {
                        addFromComposite(frame, treeIndex, forceNewDrawNode, ref j, composite, target);
                        continue;
                    }
                }

                DrawNode next = drawable.GenerateDrawNodeSubtree(frame, treeIndex, forceNewDrawNode);
                if (next == null)
                    continue;

                if (drawable.HasProxy)
                    drawable.ValidateProxyDrawNode(treeIndex, frame);
                else
                {
                    if (j < target.Count)
                        target[j] = next;
                    else
                        target.Add(next);
                    j++;
                }
            }
        }

        internal override DrawNode GenerateDrawNodeSubtree(ulong frame, int treeIndex, bool forceNewDrawNode)
        {
            // No need for a draw node at all if there are no children and we are not glowing.
            if (aliveInternalChildren.Count == 0 && CanBeFlattened)
                return null;

            DrawNode node = base.GenerateDrawNodeSubtree(frame, treeIndex, forceNewDrawNode);

            if (!(node is ICompositeDrawNode cNode))
                return null;

            cNode.Children ??= new List<DrawNode>(aliveInternalChildren.Count);

            if (cNode.AddChildDrawNodes)
            {
                int j = 0;
                addFromComposite(frame, treeIndex, forceNewDrawNode, ref j, this, cNode.Children);

                if (j < cNode.Children.Count)
                    cNode.Children.RemoveRange(j, cNode.Children.Count - j);
            }

            return node;
        }

        #endregion

        #region Transforms

        /// <summary>
        /// Whether to remove completed transforms from the list of applicable transforms. Setting this to false allows for rewinding transforms.
        /// <para>
        /// This value is passed down to children.
        /// </para>
        /// </summary>
        public override bool RemoveCompletedTransforms
        {
            get => base.RemoveCompletedTransforms;
            internal set
            {
                if (base.RemoveCompletedTransforms == value)
                    return;

                base.RemoveCompletedTransforms = value;

                foreach (var c in internalChildren)
                    c.RemoveCompletedTransforms = RemoveCompletedTransforms;
            }
        }

        public override void ApplyTransformsAt(double time, bool propagateChildren = false)
        {
            EnsureTransformMutationAllowed();

            base.ApplyTransformsAt(time, propagateChildren);

            if (!propagateChildren)
                return;

            foreach (var c in internalChildren)
                c.ApplyTransformsAt(time, true);
        }

        public override void ClearTransformsAfter(double time, bool propagateChildren = false, string targetMember = null)
        {
            EnsureTransformMutationAllowed();

            base.ClearTransformsAfter(time, propagateChildren, targetMember);

            if (AutoSizeAxes != Axes.None && AutoSizeDuration > 0)
                childrenSizeDependencies.Invalidate();

            if (!propagateChildren)
                return;

            foreach (var c in internalChildren)
                c.ClearTransformsAfter(time, true, targetMember);
        }

        internal override void AddDelay(double duration, bool propagateChildren = false)
        {
            if (duration == 0)
                return;

            base.AddDelay(duration, propagateChildren);

            if (propagateChildren)
            {
                foreach (var c in internalChildren)
                    c.AddDelay(duration, true);
            }
        }

        protected internal ScheduledDelegate ScheduleAfterChildren<T>(Action<T> action, T data)
        {
            if (TransformDelay > 0)
                return SchedulerAfterChildren.AddDelayed(action, data, TransformDelay);

            return SchedulerAfterChildren.Add(action, data);
        }

        protected internal ScheduledDelegate ScheduleAfterChildren(Action action)
        {
            if (TransformDelay > 0)
                return SchedulerAfterChildren.AddDelayed(action, TransformDelay);

            return SchedulerAfterChildren.Add(action);
        }

        public override IDisposable BeginAbsoluteSequence(double newTransformStartTime, bool recursive = true)
        {
            EnsureTransformMutationAllowed();

            if (!recursive || internalChildren.Count == 0)
                return base.BeginAbsoluteSequence(newTransformStartTime, false);

            List<AbsoluteSequenceSender> disposalActions = new List<AbsoluteSequenceSender>(internalChildren.Count + 1);

            base.CollectAbsoluteSequenceActionsFromSubTree(newTransformStartTime, disposalActions);

            foreach (var c in internalChildren)
                c.CollectAbsoluteSequenceActionsFromSubTree(newTransformStartTime, disposalActions);

            return new ValueInvokeOnDisposal<List<AbsoluteSequenceSender>>(disposalActions, actions =>
            {
                foreach (var a in actions)
                    a.Dispose();
            });
        }

        internal override void CollectAbsoluteSequenceActionsFromSubTree(double newTransformStartTime, List<AbsoluteSequenceSender> actions)
        {
            base.CollectAbsoluteSequenceActionsFromSubTree(newTransformStartTime, actions);

            foreach (var c in internalChildren)
                c.CollectAbsoluteSequenceActionsFromSubTree(newTransformStartTime, actions);
        }

        public override void FinishTransforms(bool propagateChildren = false, string targetMember = null)
        {
            EnsureTransformMutationAllowed();

            base.FinishTransforms(propagateChildren, targetMember);

            if (propagateChildren)
            {
                // Use for over foreach as collection may grow due to abort / completion events.
                // Note that this may mean that in the addition of elements being removed,
                // `FinishTransforms` may not be called on all items.
                for (int i = 0; i < internalChildren.Count; i++)
                    internalChildren[i].FinishTransforms(true, targetMember);
            }
        }

        /// <summary>
        /// Helper function for creating and adding a <see cref="Transform{TValue, T}"/> that fades the current <see cref="EdgeEffect"/>.
        /// </summary>
        protected TransformSequence<CompositeDrawable> FadeEdgeEffectTo(float newAlpha, double duration = 0, Easing easing = Easing.None)
        {
            Color4 targetColour = EdgeEffect.Colour;
            targetColour.A = newAlpha;
            return FadeEdgeEffectTo(targetColour, duration, easing);
        }

        /// <summary>
        /// Helper function for creating and adding a <see cref="Transform{TValue, T}"/> that fades the current <see cref="EdgeEffect"/>.
        /// </summary>
        protected TransformSequence<CompositeDrawable> FadeEdgeEffectTo(Color4 newColour, double duration = 0, Easing easing = Easing.None)
        {
            var effect = EdgeEffect;
            effect.Colour = newColour;
            return TweenEdgeEffectTo(effect, duration, easing);
        }

        /// <summary>
        /// Helper function for creating and adding a <see cref="Transform{TValue, T}"/> that tweens the current <see cref="EdgeEffect"/>.
        /// </summary>
        protected TransformSequence<CompositeDrawable> TweenEdgeEffectTo(EdgeEffectParameters newParams, double duration = 0, Easing easing = Easing.None) =>
            this.TransformTo(nameof(EdgeEffect), newParams, duration, easing);

        internal void EnsureChildMutationAllowed() => EnsureMutationAllowed($"mutate the {nameof(InternalChildren)}");

        #endregion

        #region Interaction / Input

        public override bool Contains(Vector2 screenSpacePos)
        {
            float cRadius = effectiveCornerRadius;
            float cExponent = CornerExponent;

            // Select a cheaper contains method when we don't need rounded edges.
            if (cRadius == 0.0f)
                return base.Contains(screenSpacePos);

            return DrawRectangle.Shrink(cRadius).DistanceExponentiated(ToLocalSpace(screenSpacePos), cExponent) <= Math.Pow(cRadius, cExponent);
        }

        /// <summary>
        /// Check whether a child should be considered for inclusion in <see cref="BuildNonPositionalInputQueue"/> and <see cref="BuildPositionalInputQueue"/>
        /// </summary>
        /// <param name="child">The drawable to be evaluated.</param>
        /// <returns>Whether or not the specified drawable should be considered when building input queues.</returns>
        protected virtual bool ShouldBeConsideredForInput(Drawable child) => child.LoadState == LoadState.Loaded;

        internal override bool BuildNonPositionalInputQueue(List<Drawable> queue, bool allowBlocking = true)
        {
            if (!base.BuildNonPositionalInputQueue(queue, allowBlocking))
                return false;

            for (int i = 0; i < aliveInternalChildren.Count; ++i)
            {
                if (ShouldBeConsideredForInput(aliveInternalChildren[i]))
                    aliveInternalChildren[i].BuildNonPositionalInputQueue(queue, allowBlocking);
            }

            return true;
        }

        /// <summary>
        /// Determines whether the subtree of this <see cref="CompositeDrawable"/> should receive positional input when the mouse is at the given screen-space position.
        /// </summary>
        /// <remarks>
        /// By default, the subtree of this <see cref="CompositeDrawable"/> always receives input when masking is turned off, and only receives input if this
        /// <see cref="CompositeDrawable"/> also receives input when masking is turned on.
        /// </remarks>
        /// <param name="screenSpacePos">The screen-space position where input could be received.</param>
        /// <returns>True if the subtree should receive input at the given screen-space position.</returns>
        protected virtual bool ReceivePositionalInputAtSubTree(Vector2 screenSpacePos) => !Masking || ReceivePositionalInputAt(screenSpacePos);

        internal override bool BuildPositionalInputQueue(Vector2 screenSpacePos, List<Drawable> queue)
        {
            if (!base.BuildPositionalInputQueue(screenSpacePos, queue))
                return false;

            if (!ReceivePositionalInputAtSubTree(screenSpacePos))
                return false;

            for (int i = 0; i < aliveInternalChildren.Count; ++i)
            {
                if (ShouldBeConsideredForInput(aliveInternalChildren[i]))
                    aliveInternalChildren[i].BuildPositionalInputQueue(screenSpacePos, queue);
            }

            return true;
        }

        #endregion

        #region Masking and related effects (e.g. round corners)

        private bool masking;

        /// <summary>
        /// If enabled, only the portion of children that falls within this <see cref="CompositeDrawable"/>'s
        /// shape is drawn to the screen.
        /// </summary>
        public bool Masking
        {
            get => masking;
            protected set
            {
                if (masking == value)
                    return;

                masking = value;
                Invalidate(Invalidation.DrawNode);
            }
        }

        private float maskingSmoothness = 1;

        /// <summary>
        /// Determines over how many pixels the alpha component smoothly fades out.
        /// Only has an effect when <see cref="Masking"/> is true.
        /// </summary>
        public float MaskingSmoothness
        {
            get => maskingSmoothness;
            protected set
            {
                //must be above zero to avoid a div-by-zero in the shader logic.
                value = Math.Max(0.01f, value);

                if (maskingSmoothness == value)
                    return;

                maskingSmoothness = value;
                Invalidate(Invalidation.DrawNode);
            }
        }

        private float cornerRadius;

        /// <summary>
        /// Determines how large of a radius is masked away around the corners.
        /// Only has an effect when <see cref="Masking"/> is true.
        /// </summary>
        public float CornerRadius
        {
            get => cornerRadius;
            protected set
            {
                if (cornerRadius == value)
                    return;

                cornerRadius = value;
                Invalidate(Invalidation.DrawNode);
            }
        }

        private float cornerExponent = 2f;

        /// <summary>
        /// Determines how gentle the curve of the corner straightens. A value of 2 (default) results in
        /// circular arcs, a value of 2.5 results in something closer to apple's "continuous corner".
        /// Values between 2 and 10 result in varying degrees of "continuousness", where larger values are smoother.
        /// Values between 1 and 2 result in a "flatter" appearance than round corners.
        /// Values between 0 and 1 result in a concave, round corner as opposed to a convex round corner,
        /// where a value of 0.5 is a circular concave arc.
        /// Only has an effect when <see cref="Masking"/> is true and <see cref="CornerRadius"/> is non-zero.
        /// </summary>
        public float CornerExponent
        {
            get => cornerExponent;
            protected set
            {
                if (!Precision.DefinitelyBigger(value, 0) || value > 10)
                    throw new ArgumentOutOfRangeException(nameof(CornerExponent), $"{nameof(CornerExponent)} may not be <=0 or >10 for numerical correctness.");

                if (cornerExponent == value)
                    return;

                cornerExponent = value;
                Invalidate(Invalidation.DrawNode);
            }
        }

        // This _hacky_ modification of the corner radius (obtained from playing around) ensures that the corner remains at roughly
        // equal size (perceptually) compared to the circular arc as the CornerExponent is adjusted within the range ~2-5.
        private float effectiveCornerRadius => CornerRadius * 0.8f * CornerExponent / 2 + 0.2f * CornerRadius;

        private float borderThickness;

        /// <summary>
        /// Determines how thick of a border to draw around the inside of the masked region.
        /// Only has an effect when <see cref="Masking"/> is true.
        /// The border only is drawn on top of children using a sprite shader.
        /// </summary>
        /// <remarks>
        /// Drawing borders is optimized heavily into our sprite shaders. As a consequence
        /// borders are only drawn correctly on top of quad-shaped children using our sprite
        /// shaders.
        /// </remarks>
        public float BorderThickness
        {
            get => borderThickness;
            protected set
            {
                if (borderThickness == value)
                    return;

                borderThickness = value;
                Invalidate(Invalidation.DrawNode);
            }
        }

        private ColourInfo borderColour = Color4.Black;

        /// <summary>
        /// Determines the color of the border controlled by <see cref="BorderThickness"/>.
        /// Only has an effect when <see cref="Masking"/> is true.
        /// </summary>
        public ColourInfo BorderColour
        {
            get => borderColour;
            protected set
            {
                if (borderColour.Equals(value))
                    return;

                borderColour = value;
                Invalidate(Invalidation.DrawNode);
            }
        }

        private EdgeEffectParameters edgeEffect;

        /// <summary>
        /// Determines an edge effect of this <see cref="CompositeDrawable"/>.
        /// Edge effects are e.g. glow or a shadow.
        /// Only has an effect when <see cref="Masking"/> is true.
        /// </summary>
        public EdgeEffectParameters EdgeEffect
        {
            get => edgeEffect;
            protected set
            {
                if (edgeEffect.Equals(value))
                    return;

                edgeEffect = value;
                Invalidate(Invalidation.DrawNode);
            }
        }

        #endregion

        #region Sizing

        public override RectangleF BoundingBox
        {
            get
            {
                float cRadius = CornerRadius;
                if (cRadius == 0.0f)
                    return base.BoundingBox;

                RectangleF drawRect = LayoutRectangle.Shrink(cRadius);

                // Inflate bounding box in parent space by the half-size of the bounding box of the
                // ellipse obtained by transforming the unit circle into parent space.
                Vector2 offset = ToParentSpace(Vector2.Zero);
                Vector2 u = ToParentSpace(new Vector2(cRadius, 0)) - offset;
                Vector2 v = ToParentSpace(new Vector2(0, cRadius)) - offset;
                Vector2 inflation = new Vector2(
                    MathF.Sqrt(u.X * u.X + v.X * v.X),
                    MathF.Sqrt(u.Y * u.Y + v.Y * v.Y)
                );

                RectangleF result = ToParentSpace(drawRect).AABBFloat.Inflate(inflation);
                // The above algorithm will return incorrect results if the rounded corners are not fully visible.
                // To limit bad behavior we at least enforce here, that the bounding box with rounded corners
                // is never larger than the bounding box without.
                if (DrawSize.X < CornerRadius * 2 || DrawSize.Y < CornerRadius * 2)
                    result.Intersect(base.BoundingBox);

                return result;
            }
        }

        private MarginPadding padding;

        /// <summary>
        /// Shrinks the space children may occupy within this <see cref="CompositeDrawable"/>
        /// by the specified amount on each side.
        /// </summary>
        public MarginPadding Padding
        {
            get => padding;
            protected set
            {
                if (padding.Equals(value)) return;

                if (!Validation.IsFinite(value)) throw new ArgumentException($@"{nameof(Padding)} must be finite, but is {value}.");

                padding = value;

                foreach (Drawable c in internalChildren)
                    c.Invalidate(c.InvalidationFromParentSize | Invalidation.MiscGeometry);
            }
        }

        /// <summary>
        /// The size of the coordinate space revealed to <see cref="InternalChildren"/>.
        /// Captures the effect of e.g. <see cref="Padding"/>.
        /// </summary>
        public Vector2 ChildSize => DrawSize - new Vector2(Padding.TotalHorizontal, Padding.TotalVertical);

        /// <summary>
        /// Positional offset applied to <see cref="InternalChildren"/>.
        /// Captures the effect of e.g. <see cref="Padding"/>.
        /// </summary>
        public Vector2 ChildOffset => new Vector2(Padding.Left, Padding.Top);

        private Vector2 relativeChildSize = Vector2.One;

        /// <summary>
        /// The size of the relative position/size coordinate space of children of this <see cref="CompositeDrawable"/>.
        /// Children positioned at this size will appear as if they were positioned at <see cref="Drawable.Position"/> = <see cref="Vector2.One"/> in this <see cref="CompositeDrawable"/>.
        /// </summary>
        public Vector2 RelativeChildSize
        {
            get => relativeChildSize;
            protected set
            {
                if (relativeChildSize == value)
                    return;

                if (!Validation.IsFinite(value)) throw new ArgumentException($@"{nameof(RelativeChildSize)} must be finite, but is {value}.");
                if (value.X == 0 || value.Y == 0) throw new ArgumentException($@"{nameof(RelativeChildSize)} must be non-zero, but is {value}.");

                relativeChildSize = value;

                foreach (Drawable c in internalChildren)
                    c.Invalidate(c.InvalidationFromParentSize);
            }
        }

        private Vector2 relativeChildOffset = Vector2.Zero;

        /// <summary>
        /// The offset of the relative position/size coordinate space of children of this <see cref="CompositeDrawable"/>.
        /// Children positioned at this offset will appear as if they were positioned at <see cref="Drawable.Position"/> = <see cref="Vector2.Zero"/> in this <see cref="CompositeDrawable"/>.
        /// </summary>
        public Vector2 RelativeChildOffset
        {
            get => relativeChildOffset;
            protected set
            {
                if (relativeChildOffset == value)
                    return;

                if (!Validation.IsFinite(value)) throw new ArgumentException($@"{nameof(RelativeChildOffset)} must be finite, but is {value}.");

                relativeChildOffset = value;

                foreach (Drawable c in internalChildren)
                    c.Invalidate(c.InvalidationFromParentSize & ~Invalidation.DrawSize);
            }
        }

        /// <summary>
        /// Conversion factor from relative to absolute coordinates in our space.
        /// </summary>
        public Vector2 RelativeToAbsoluteFactor => Vector2.Divide(ChildSize, RelativeChildSize);

        /// <summary>
        /// Tweens the <see cref="RelativeChildSize"/> of this <see cref="CompositeDrawable"/>.
        /// </summary>
        /// <param name="newSize">The coordinate space to tween to.</param>
        /// <param name="duration">The tween duration.</param>
        /// <param name="easing">The tween easing.</param>
        protected TransformSequence<CompositeDrawable> TransformRelativeChildSizeTo(Vector2 newSize, double duration = 0, Easing easing = Easing.None)
        {
            if (newSize.X == 0 || newSize.Y == 0)
                throw new ArgumentException($@"{nameof(newSize)} must be non-zero, but is {newSize}.", nameof(newSize));

            return this.TransformTo(nameof(RelativeChildSize), newSize, duration, easing);
        }

        /// <summary>
        /// Tweens the <see cref="RelativeChildOffset"/> of this <see cref="CompositeDrawable"/>.
        /// </summary>
        /// <param name="newOffset">The coordinate space to tween to.</param>
        /// <param name="duration">The tween duration.</param>
        /// <param name="easing">The tween easing.</param>
        protected TransformSequence<CompositeDrawable> TransformRelativeChildOffsetTo(Vector2 newOffset, double duration = 0, Easing easing = Easing.None) =>
            this.TransformTo(nameof(RelativeChildOffset), newOffset, duration, easing);

        public override Axes RelativeSizeAxes
        {
            get => base.RelativeSizeAxes;
            set
            {
                if ((AutoSizeAxes & value) != 0)
                    throw new InvalidOperationException("No axis can be relatively sized and automatically sized at the same time.");

                base.RelativeSizeAxes = value;
            }
        }

        private Axes autoSizeAxes;

        /// <summary>
        /// Controls which <see cref="Axes"/> are automatically sized w.r.t. <see cref="InternalChildren"/>.
        /// Children's <see cref="Drawable.BypassAutoSizeAxes"/> are ignored for automatic sizing.
        /// Most notably, <see cref="Drawable.RelativePositionAxes"/> and <see cref="RelativeSizeAxes"/> of children
        /// do not affect automatic sizing to avoid circular size dependencies.
        /// It is not allowed to manually set <see cref="Size"/> (or <see cref="Width"/> / <see cref="Height"/>)
        /// on any <see cref="Axes"/> which are automatically sized.
        /// </summary>
        public virtual Axes AutoSizeAxes
        {
            get => autoSizeAxes;
            protected set
            {
                if (value == autoSizeAxes)
                    return;

                if ((RelativeSizeAxes & value) != 0)
                    throw new InvalidOperationException("No axis can be relatively sized and automatically sized at the same time.");

                autoSizeAxes = value;
                childrenSizeDependencies.Invalidate();
                OnSizingChanged();
            }
        }

        /// <summary>
        /// The duration which automatic sizing should take. If zero, then it is instantaneous.
        /// Otherwise, this is equivalent to applying an automatic size via a resize transform.
        /// </summary>
        public float AutoSizeDuration { get; protected set; }

        /// <summary>
        /// The type of easing which should be used for smooth automatic sizing when <see cref="AutoSizeDuration"/>
        /// is non-zero.
        /// </summary>
        public Easing AutoSizeEasing { get; protected set; }

        /// <summary>
        /// Fired after this <see cref="CompositeDrawable"/>'s <see cref="Size"/> is updated through autosize.
        /// </summary>
        internal event Action OnAutoSize;

        private readonly LayoutValue childrenSizeDependencies = new LayoutValue(Invalidation.RequiredParentSizeToFit | Invalidation.Presence, InvalidationSource.Child);

        public override float Width
        {
            get
            {
                if (!isComputingChildrenSizeDependencies && AutoSizeAxes.HasFlagFast(Axes.X))
                    updateChildrenSizeDependencies();
                return base.Width;
            }

            set
            {
                if ((AutoSizeAxes & Axes.X) != 0)
                    throw new InvalidOperationException($"The width of a {nameof(CompositeDrawable)} with {nameof(AutoSizeAxes)} can not be set manually.");

                base.Width = value;
            }
        }

        public override float Height
        {
            get
            {
                if (!isComputingChildrenSizeDependencies && AutoSizeAxes.HasFlagFast(Axes.Y))
                    updateChildrenSizeDependencies();
                return base.Height;
            }

            set
            {
                if ((AutoSizeAxes & Axes.Y) != 0)
                    throw new InvalidOperationException($"The height of a {nameof(CompositeDrawable)} with {nameof(AutoSizeAxes)} can not be set manually.");

                base.Height = value;
            }
        }

        private bool isComputingChildrenSizeDependencies;

        public override Vector2 Size
        {
            get
            {
                if (!isComputingChildrenSizeDependencies && AutoSizeAxes != Axes.None)
                    updateChildrenSizeDependencies();
                return base.Size;
            }

            set
            {
                if ((AutoSizeAxes & Axes.Both) != 0)
                    throw new InvalidOperationException($"The Size of a {nameof(CompositeDrawable)} with {nameof(AutoSizeAxes)} can not be set manually.");

                base.Size = value;
            }
        }

        private Vector2 computeAutoSize()
        {
            MarginPadding originalPadding = Padding;
            MarginPadding originalMargin = Margin;

            try
            {
                Padding = new MarginPadding();
                Margin = new MarginPadding();

                if (AutoSizeAxes == Axes.None) return DrawSize;

                Vector2 maxBoundSize = Vector2.Zero;

                // Find the maximum width/height of children
                foreach (Drawable c in aliveInternalChildren)
                {
                    if (!c.IsPresent)
                        continue;

                    Vector2 cBound = c.RequiredParentSizeToFit;

                    if (!c.BypassAutoSizeAxes.HasFlagFast(Axes.X))
                        maxBoundSize.X = Math.Max(maxBoundSize.X, cBound.X);

                    if (!c.BypassAutoSizeAxes.HasFlagFast(Axes.Y))
                        maxBoundSize.Y = Math.Max(maxBoundSize.Y, cBound.Y);
                }

                if (!AutoSizeAxes.HasFlagFast(Axes.X))
                    maxBoundSize.X = DrawSize.X;
                if (!AutoSizeAxes.HasFlagFast(Axes.Y))
                    maxBoundSize.Y = DrawSize.Y;

                return new Vector2(maxBoundSize.X, maxBoundSize.Y);
            }
            finally
            {
                Padding = originalPadding;
                Margin = originalMargin;
            }
        }

        private void updateAutoSize()
        {
            if (AutoSizeAxes == Axes.None)
                return;

            Vector2 b = computeAutoSize() + Padding.Total;

            autoSizeResizeTo(new Vector2(
                AutoSizeAxes.HasFlagFast(Axes.X) ? b.X : base.Width,
                AutoSizeAxes.HasFlagFast(Axes.Y) ? b.Y : base.Height
            ), AutoSizeDuration, AutoSizeEasing);

            //note that this is called before autoSize becomes valid. may be something to consider down the line.
            //might work better to add an OnRefresh event in Cached<> and invoke there.
            OnAutoSize?.Invoke();
        }

        private void updateChildrenSizeDependencies()
        {
            isComputingChildrenSizeDependencies = true;

            try
            {
                if (!childrenSizeDependencies.IsValid)
                {
                    updateAutoSize();
                    childrenSizeDependencies.Validate();
                }
            }
            finally
            {
                isComputingChildrenSizeDependencies = false;
            }
        }

        private void autoSizeResizeTo(Vector2 newSize, double duration = 0, Easing easing = Easing.None)
        {
            var currentTransform = TransformsForTargetMember(nameof(baseSize)).FirstOrDefault() as AutoSizeTransform;

            if ((currentTransform?.EndValue ?? Size) != newSize)
            {
                if (duration == 0)
                {
                    if (currentTransform != null)
                        ClearTransforms(false, nameof(baseSize));
                    baseSize = newSize;
                }
                else
                    this.TransformTo(this.PopulateTransform(new AutoSizeTransform { Rewindable = false }, newSize, duration, easing));
            }
        }

        /// <summary>
        /// A helper property for <see cref="autoSizeResizeTo(Vector2, double, Easing)"/> to change the size of <see cref="CompositeDrawable"/>s with <see cref="AutoSizeAxes"/>.
        /// </summary>
        private Vector2 baseSize
        {
            get => new Vector2(base.Width, base.Height);
            set
            {
                base.Width = value.X;
                base.Height = value.Y;
            }
        }

        private class AutoSizeTransform : TransformCustom<Vector2, CompositeDrawable>
        {
            public AutoSizeTransform()
                : base(nameof(baseSize))
            {
            }
        }

        #endregion
    }
}
