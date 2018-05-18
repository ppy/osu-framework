// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Lists;
using System.Collections.Generic;
using System;
using System.Diagnostics;
using OpenTK;
using osu.Framework.Graphics.OpenGL;
using OpenTK.Graphics;
using osu.Framework.Graphics.Shaders;
using osu.Framework.Extensions.IEnumerableExtensions;
using osu.Framework.Graphics.Colour;
using osu.Framework.Allocation;
using osu.Framework.Graphics.Transforms;
using osu.Framework.Timing;
using osu.Framework.Caching;
using osu.Framework.Threading;
using osu.Framework.Statistics;
using System.Threading.Tasks;
using osu.Framework.Graphics.Primitives;
using osu.Framework.MathUtils;

namespace osu.Framework.Graphics.Containers
{
    /// <summary>
    /// A drawable consisting of a composite of child drawables which are
    /// manages by the composite object itself. Transformations applied to
    /// a <see cref="CompositeDrawable"/> are also applied to its children.
    /// Additionally, <see cref="CompositeDrawable"/>s support various effects, such as masking, edge effect,
    /// padding, and automatic sizing depending on their children.
    /// </summary>
    public abstract class CompositeDrawable : Drawable
    {
        #region Contruction and disposal

        /// <summary>
        /// Contructs a <see cref="CompositeDrawable"/> that stores children.
        /// </summary>
        protected CompositeDrawable()
        {
            internalChildren = new SortedList<Drawable>(new ChildComparer(this));
            aliveInternalChildren = new SortedList<Drawable>(new ChildComparer(this));
        }

        private Game game;

        /// <summary>
        /// Loads a future child or grand-child of this <see cref="CompositeDrawable"/> asyncronously. <see cref="Drawable.Dependencies"/>
        /// and <see cref="Drawable.Clock"/> are inherited from this <see cref="CompositeDrawable"/>.
        ///
        /// Note that this will always use the dependencies and clock from this instance. If you must load to a nested container level,
        /// consider using <see cref="DelayedLoadWrapper"/>
        /// </summary>
        /// <typeparam name="TLoadable">The type of the future future child or grand-child to be loaded.</typeparam>
        /// <param name="component">The type of the future future child or grand-child to be loaded.</param>
        /// <param name="onLoaded">Callback to be invoked on the update thread after loading is complete.</param>
        /// <returns>The task which is used for loading and callbacks.</returns>
        protected Task LoadComponentAsync<TLoadable>(TLoadable component, Action<TLoadable> onLoaded = null) where TLoadable : Drawable
        {
            if (game == null)
                throw new InvalidOperationException($"May not invoke {nameof(LoadComponentAsync)} prior to this {nameof(CompositeDrawable)} being loaded.");

            return component.LoadAsync(game, this, () => onLoaded?.Invoke(component));
        }

        [BackgroundDependencyLoader(true)]
        private void load(Game game, ShaderManager shaders)
        {
            this.game = game;

            if (shader == null)
                shader = shaders?.Load(VertexShaderDescriptor.TEXTURE_2, FragmentShaderDescriptor.TEXTURE_ROUNDED);

            // We are in a potentially async context, so let's aggressively load all our children
            // regardless of their alive state. this also gives children a clock so they can be checked
            // for their correct alive state in the case LifetimeStart is set to a definite value.
            internalChildren.ForEach(loadChild);
        }

        protected override void LoadAsyncComplete()
        {
            base.LoadAsyncComplete();

            // At this point we can assume that we are loaded although we're not in the "ready" state, because we'll be given
            // a "ready" state soon after this method terminates. Therefore we can perform an early check to add any alive children
            // while we're still in an asynchronous context and avoid putting pressure on the main thread during UpdateSubTree.
            checkChildrenLife();
        }

        private void loadChild(Drawable child)
        {
            child.Load(Clock, Dependencies);
            child.Parent = this;
        }

        protected override void LoadComplete()
        {
            schedulerAfterChildren?.SetCurrentThread(MainThread);
            base.LoadComplete();
        }

        protected override void Dispose(bool isDisposing)
        {
            InternalChildren?.ForEach(c => c.Dispose());

            OnAutoSize = null;
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
        /// Gets or sets the only child in <see cref="InternalChildren"/>.
        /// </summary>
        protected internal Drawable InternalChild
        {
            get
            {
                if (InternalChildren.Count != 1)
                    throw new InvalidOperationException($"{nameof(InternalChild)} is only available when there's only 1 in {nameof(InternalChildren)}!");

                return InternalChildren[0];
            }
            set
            {
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
            get { return internalChildren; }
            set { InternalChildrenEnumerable = value; }
        }

        /// <summary>
        /// Replaces all internal children of this <see cref="CompositeDrawable"/> with the elements contained in the enumerable.
        /// </summary>
        protected internal IEnumerable<Drawable> InternalChildrenEnumerable
        {
            set
            {
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
        protected internal int IndexOfInternal(Drawable drawable) => internalChildren.IndexOf(drawable);

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
                InvalidateFromChild(Invalidation.RequiredParentSizeToFit, drawable);

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
            if (internalChildren.Count == 0) return;

            foreach (Drawable t in internalChildren)
            {
                if (t.IsAlive)
                    ChildDied?.Invoke(t);

                t.IsAlive = false;

                if (disposeChildren)
                {
                    //cascade disposal
                    (t as CompositeDrawable)?.ClearInternal();
                    t.Dispose();
                }
                else
                    t.Parent = null;

                Trace.Assert(t.Parent == null);
            }

            internalChildren.Clear();
            aliveInternalChildren.Clear();

            if (AutoSizeAxes != Axes.None)
                InvalidateFromChild(Invalidation.RequiredParentSizeToFit);
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
            if (drawable == null)
                throw new ArgumentNullException(nameof(drawable), $"null {nameof(Drawable)}s may not be added to {nameof(CompositeDrawable)}.");

            if (drawable == this)
                throw new InvalidOperationException($"{nameof(CompositeDrawable)} may not be added to itself.");

            // If the drawable's ChildId is not zero, then it was added to another parent even if it wasn't loaded
            if (drawable.ChildID != 0)
                throw new InvalidOperationException("May not add a drawable to multiple containers.");

            drawable.ChildID = ++currentChildID;
            drawable.RemoveCompletedTransforms = RemoveCompletedTransforms;

            if (drawable.LoadState >= LoadState.Ready)
                drawable.Parent = this;
            // If we're already loaded, we can eagerly allow children to be loaded
            else if (LoadState >= LoadState.Loading)
                loadChild(drawable);

            internalChildren.Add(drawable);

            if (LoadState >= LoadState.Ready)
                checkChildLife(drawable);

            if (AutoSizeAxes != Axes.None)
                InvalidateFromChild(Invalidation.RequiredParentSizeToFit, drawable);
        }

        /// <summary>
        /// Adds a range of children to <see cref="InternalChildren"/>. This is equivalent to calling
        /// <see cref="AddInternal(Drawable)"/> on each element of the range in order.
        /// </summary>
        protected internal void AddRangeInternal(IEnumerable<Drawable> range)
        {
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
            if (child.Depth == newDepth) return;

            var index = IndexOfInternal(child);
            if (index < 0)
                throw new InvalidOperationException($"Can not change depth of drawable which is not contained within this {nameof(CompositeDrawable)}.");

            internalChildren.RemoveAt(index);
            var aliveIndex = aliveInternalChildren.IndexOf(child);
            if (aliveIndex >= 0) // remove if found
                aliveInternalChildren.RemoveAt(aliveIndex);

            var chId = child.ChildID;
            child.ChildID = 0; // ensure Depth-change does not throw an exception
            child.Depth = newDepth;
            child.ChildID = chId;

            internalChildren.Add(child);
            if (aliveIndex >= 0) // re-add if it used to be in aliveInternalChildren
                aliveInternalChildren.Add(child);
        }

        #endregion

        #region Updating (per-frame periodic)

        private Scheduler schedulerAfterChildren;

        protected Scheduler SchedulerAfterChildren => schedulerAfterChildren ?? (schedulerAfterChildren = new Scheduler(MainThread, Clock));

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

            if (!checkChildrenLife())
                return false;

            return true;
        }

        /// <summary>
        /// Checks whether the alive state of any child has changed and processes it. This will add or remove
        /// children from <see cref="aliveInternalChildren"/> depending on their alive states.
        /// <para>Note that this does NOT check the load state of this <see cref="CompositeDrawable"/> to check if it can hold any alive children.</para>
        /// </summary>
        /// <returns>Whether any child's alive state has changed.</returns>
        private bool checkChildrenLife()
        {
            bool anyAliveChanged = false;

            // checkChildLife may remove a child from internalChildren. In order to not skip children,
            // we keep track of the original amount children to apply an offset to the iterator
            int originalCount = internalChildren.Count;
            for (int i = 0; i < internalChildren.Count; i++)
                anyAliveChanged |= checkChildLife(internalChildren[i + internalChildren.Count - originalCount]);

            if (anyAliveChanged)
                childrenSizeDependencies.Invalidate();

            return anyAliveChanged;
        }

        /// <summary>
        /// Checks whether the alive state of a child has changed and processes it. This will add or remove
        /// the child from <see cref="aliveInternalChildren"/> depending on its alive state.
        /// <para>Note that this does NOT check the load state of this <see cref="CompositeDrawable"/> to check if it can hold any alive children.</para>
        /// </summary>
        /// <param name="child">The child to check.</param>
        /// <returns>Whether the child's alive state has changed.</returns>
        private bool checkChildLife(Drawable child)
        {
            Debug.Assert(internalChildren.Contains(child), "Can only check and react to the life of our own children.");

            bool changed = false;

            if (child.ShouldBeAlive)
            {
                if (!child.IsAlive)
                {
                    loadChild(child);

                    if (child.LoadState >= LoadState.Ready)
                    {
                        aliveInternalChildren.Add(child);
                        ChildBecameAlive?.Invoke(child);
                        child.IsAlive = true;
                        changed = true;
                    }
                }
            }
            else
            {
                if (child.IsAlive)
                {
                    aliveInternalChildren.Remove(child);
                    ChildDied?.Invoke(child);
                    child.IsAlive = false;
                    changed = true;
                }

                if (child.RemoveWhenNotAlive)
                {
                    RemoveInternal(child);

                    if (child.DisposeOnDeathRemoval)
                        child.Dispose();
                }
            }

            return changed;
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

            // We iterate by index to gain performance
            // ReSharper disable once ForCanBeConvertedToForeach
            for (int i = 0; i < aliveInternalChildren.Count; ++i)
            {
                Drawable c = aliveInternalChildren[i];
                Debug.Assert(c.LoadState >= LoadState.Ready);
                c.UpdateSubTree();
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


                // We iterate by index to gain performance
                // ReSharper disable once ForCanBeConvertedToForeach
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

        /// <summary>
        /// Informs this <see cref="CompositeDrawable"/> that a child has been invalidated.
        /// </summary>
        /// <param name="invalidation">The type of invalidation applied to the child.</param>
        /// <param name="source">The child which caused this invalidation. May be null to indicate that a specific child wasn't specified.</param>
        public virtual void InvalidateFromChild(Invalidation invalidation, Drawable source = null)
        {
            // We only want to recompute autosize if the child isn't bypassing all of our autosize axes
            if (source != null && (source.BypassAutoSizeAxes & AutoSizeAxes) == AutoSizeAxes)
                return;

            //Colour captures potential changes in IsPresent. If this ever becomes a bottleneck,
            //Invalidation could be further separated into presence changes.
            if ((invalidation & (Invalidation.RequiredParentSizeToFit | Invalidation.Colour)) > 0)
                childrenSizeDependencies.Invalidate();
        }

        public override bool Invalidate(Invalidation invalidation = Invalidation.All, Drawable source = null, bool shallPropagate = true)
        {
            if (!base.Invalidate(invalidation, source, shallPropagate))
                return false;

            if (!shallPropagate) return true;

            // This way of looping turns out to be slightly faster than a foreach
            // or directly indexing a SortedList<T>. This part of the code is often
            // hot, so an optimization like this makes sense here.
            SortedList<Drawable> current = internalChildren;
            // ReSharper disable once ForCanBeConvertedToForeach
            for (int i = 0; i < current.Count; ++i)
            {
                Drawable c = current[i];
                Debug.Assert(c != source);

                Invalidation childInvalidation = invalidation;
                if ((invalidation & Invalidation.RequiredParentSizeToFit) > 0)
                    childInvalidation |= Invalidation.DrawInfo;

                // Other geometry things like rotation, shearing, etc don't affect child properties.
                childInvalidation &= ~Invalidation.MiscGeometry;

                // Relative positioning can however affect child geometry
                // ReSharper disable once PossibleNullReferenceException
                if (c.RelativePositionAxes != Axes.None && (invalidation & Invalidation.DrawSize) > 0)
                    childInvalidation |= Invalidation.MiscGeometry;

                // No draw size changes if relative size axes does not propagate it downward.
                if (c.RelativeSizeAxes == Axes.None)
                    childInvalidation &= ~Invalidation.DrawSize;

                c.Invalidate(childInvalidation, this);
            }

            return true;
        }

        #endregion

        #region DrawNode

        private readonly CompositeDrawNodeSharedData compositeDrawNodeSharedData = new CompositeDrawNodeSharedData();
        private Shader shader;

        protected override DrawNode CreateDrawNode() => new CompositeDrawNode();

        protected override void ApplyDrawNode(DrawNode node)
        {
            CompositeDrawNode n = (CompositeDrawNode)node;

            if (!Masking && (BorderThickness != 0.0f || EdgeEffect.Type != EdgeEffectType.None))
                throw new InvalidOperationException("Can not have border effects/edge effects if masking is disabled.");

            Vector3 scale = DrawInfo.MatrixInverse.ExtractScale();

            n.MaskingInfo = !Masking
                ? (MaskingInfo?)null
                : new MaskingInfo
                {
                    ScreenSpaceAABB = ScreenSpaceDrawQuad.AABB,
                    MaskingRect = DrawRectangle,
                    ToMaskingSpace = DrawInfo.MatrixInverse,
                    CornerRadius = CornerRadius,
                    BorderThickness = BorderThickness,
                    BorderColour = BorderColour,
                    // We are setting the linear blend range to the approximate size of a _pixel_ here.
                    // This results in the optimal trade-off between crispness and smoothness of the
                    // edges of the masked region according to sampling theory.
                    BlendRange = MaskingSmoothness * (scale.X + scale.Y) / 2,
                    AlphaExponent = 1,
                };

            n.EdgeEffect = EdgeEffect;

            n.ScreenSpaceMaskingQuad = null;
            n.Shared = compositeDrawNodeSharedData;

            n.Shader = shader;

            base.ApplyDrawNode(node);
        }

        /// <summary>
        /// A flattened <see cref="CompositeDrawable"/> has its <see cref="DrawNode"/> merged into its parents'.
        /// In some cases, the <see cref="DrawNode"/> must always be generated and flattening should not occur.
        /// </summary>
        protected virtual bool CanBeFlattened =>
            // Masking composite DrawNodes define the masking area for their children
            !Masking
            // Proxied drawables have their DrawNodes drawn elsewhere in the scene graph
            && !HasProxy;

        private const int amount_children_required_for_masking_check = 2;

        /// <summary>
        /// This function adds all children's <see cref="DrawNode"/>s to a target List, flattening the children of certain types
        /// of <see cref="CompositeDrawable"/> subtrees for optimization purposes.
        /// </summary>
        /// <param name="frame">The frame which <see cref="DrawNode"/>s should be generated for.</param>
        /// <param name="treeIndex">The index of the currently in-use <see cref="DrawNode"/> tree.</param>
        /// <param name="j">The running index into the target List.</param>
        /// <param name="parentComposite">The <see cref="CompositeDrawable"/> whose children's <see cref="DrawNode"/>s to add.</param>
        /// <param name="target">The target list to fill with DrawNodes.</param>
        private static void addFromComposite(ulong frame, int treeIndex, ref int j, CompositeDrawable parentComposite, List<DrawNode> target)
        {
            SortedList<Drawable> current = parentComposite.aliveInternalChildren;
            // ReSharper disable once ForCanBeConvertedToForeach
            for (int i = 0; i < current.Count; ++i)
            {
                Drawable drawable = current[i];

                if (!drawable.IsProxy)
                {
                    if (!drawable.IsPresent)
                        continue;

                    CompositeDrawable composite = drawable as CompositeDrawable;
                    if (composite?.CanBeFlattened == true)
                    {
                        if (!composite.IsMaskedAway)
                            addFromComposite(frame, treeIndex, ref j, composite, target);

                        continue;
                    }

                    if (drawable.IsMaskedAway)
                        continue;
                }

                DrawNode next = drawable.GenerateDrawNodeSubtree(frame, treeIndex);
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

        internal virtual bool AddChildDrawNodes => true;

        internal override DrawNode GenerateDrawNodeSubtree(ulong frame, int treeIndex)
        {
            // No need for a draw node at all if there are no children and we are not glowing.
            if (aliveInternalChildren.Count == 0 && CanBeFlattened)
                return null;

            if (!(base.GenerateDrawNodeSubtree(frame, treeIndex) is CompositeDrawNode cNode))
                return null;

            if (cNode.Children == null)
                cNode.Children = new List<DrawNode>(aliveInternalChildren.Count);

            if (AddChildDrawNodes)
            {
                List<DrawNode> target = cNode.Children;

                int j = 0;
                addFromComposite(frame, treeIndex, ref j, this, target);

                if (j < target.Count)
                    target.RemoveRange(j, target.Count - j);
            }

            return cNode;
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
            get { return base.RemoveCompletedTransforms; }
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
            base.ApplyTransformsAt(time, propagateChildren);

            if (!propagateChildren)
                return;

            foreach (var c in internalChildren)
                c.ApplyTransformsAt(time, true);
        }

        public override void ClearTransformsAfter(double time, bool propagateChildren = false, string targetMember = null)
        {
            base.ClearTransformsAfter(time, propagateChildren, targetMember);

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
                foreach (var c in internalChildren)
                    c.AddDelay(duration, true);
        }

        protected ScheduledDelegate ScheduleAfterChildren(Action action) => SchedulerAfterChildren.AddDelayed(action, TransformDelay);

        public override InvokeOnDisposal BeginAbsoluteSequence(double newTransformStartTime, bool recursive = false)
        {
            var baseDisposalAction = base.BeginAbsoluteSequence(newTransformStartTime, recursive);
            if (!recursive)
                return baseDisposalAction;

            List<InvokeOnDisposal> disposalActions = new List<InvokeOnDisposal>(internalChildren.Count + 1) { baseDisposalAction };
            foreach (var c in internalChildren)
                disposalActions.Add(c.BeginAbsoluteSequence(newTransformStartTime, true));

            return new InvokeOnDisposal(() =>
            {
                foreach (var a in disposalActions)
                    a.Dispose();
            });
        }

        public override void FinishTransforms(bool propagateChildren = false, string targetMember = null)
        {
            base.FinishTransforms(propagateChildren, targetMember);

            if (propagateChildren)
                foreach (var c in internalChildren)
                    c.FinishTransforms(true, targetMember);
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

        #endregion

        #region Interaction / Input

        // Required to pass through input to children by default.
        // TODO: Evaluate effects of this on performance and address.
        public override bool HandleKeyboardInput => true;
        public override bool HandleMouseInput => true;

        public override bool Contains(Vector2 screenSpacePos)
        {
            float cRadius = CornerRadius;

            // Select a cheaper contains method when we don't need rounded edges.
            if (cRadius == 0.0f)
                return base.Contains(screenSpacePos);
            return DrawRectangle.Shrink(cRadius).DistanceSquared(ToLocalSpace(screenSpacePos)) <= cRadius * cRadius;
        }

        internal override bool BuildKeyboardInputQueue(List<Drawable> queue)
        {
            if (!base.BuildKeyboardInputQueue(queue))
                return false;

            // We iterate by index to gain performance
            // ReSharper disable once ForCanBeConvertedToForeach
            for (int i = 0; i < aliveInternalChildren.Count; ++i)
                aliveInternalChildren[i].BuildKeyboardInputQueue(queue);

            return true;
        }

        internal override bool BuildMouseInputQueue(Vector2 screenSpaceMousePos, List<Drawable> queue)
        {
            if (!base.BuildMouseInputQueue(screenSpaceMousePos, queue) && (!CanReceiveMouseInput || Masking))
                return false;

            // We iterate by index to gain performance
            // ReSharper disable once ForCanBeConvertedToForeach
            for (int i = 0; i < aliveInternalChildren.Count; ++i)
                aliveInternalChildren[i].BuildMouseInputQueue(screenSpaceMousePos, queue);

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
            get { return masking; }
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
            get { return maskingSmoothness; }
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
            get { return cornerRadius; }
            protected set
            {
                if (cornerRadius == value)
                    return;

                cornerRadius = value;
                Invalidate(Invalidation.DrawNode);
            }
        }

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
            get { return borderThickness; }
            protected set
            {
                if (borderThickness == value)
                    return;

                borderThickness = value;
                Invalidate(Invalidation.DrawNode);
            }
        }

        private SRGBColour borderColour = Color4.Black;

        /// <summary>
        /// Determines the color of the border controlled by <see cref="BorderThickness"/>.
        /// Only has an effect when <see cref="Masking"/> is true.
        /// </summary>
        public SRGBColour BorderColour
        {
            get { return borderColour; }
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
            get { return edgeEffect; }
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
                Vector2 inflation = new Vector2((float)Math.Sqrt(u.X * u.X + v.X * v.X), (float)Math.Sqrt(u.Y * u.Y + v.Y * v.Y));

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
            get { return padding; }
            protected set
            {
                if (padding.Equals(value)) return;

                if (!Validation.IsFinite(value)) throw new ArgumentException($@"{nameof(Padding)} must be finite, but is {value}.");

                padding = value;

                foreach (Drawable c in internalChildren)
                    c.Invalidate(c.InvalidationFromParentSize);
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
            get { return relativeChildSize; }
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
            get { return relativeChildOffset; }
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
        protected TransformSequence<CompositeDrawable> TransformRelativeChildSizeTo(Vector2 newSize, double duration = 0, Easing easing = Easing.None) =>
            this.TransformTo(nameof(RelativeChildSize), newSize, duration, easing);

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
            get { return base.RelativeSizeAxes; }
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
            get { return autoSizeAxes; }
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
        /// THIS EVENT PURELY EXISTS FOR THE SCENE GRAPH VISUALIZER. DO NOT USE.
        /// This event will fire after our <see cref="Size"/> is updated from autosizing.
        /// </summary>
        internal event Action OnAutoSize;

        private Cached childrenSizeDependencies = new Cached();

        public override float Width
        {
            get
            {
                if (!StaticCached.BypassCache && !isComputingChildrenSizeDependencies && (AutoSizeAxes & Axes.X) > 0)
                    updateChildrenSizeDependencies();
                return base.Width;
            }

            set
            {
                if ((AutoSizeAxes & Axes.X) != 0)
                    throw new InvalidOperationException($"The width of a {nameof(CompositeDrawable)} with {nameof(AutoSizeAxes)} should only be manually set if it is relative to its parent.");
                base.Width = value;
            }
        }

        public override float Height
        {
            get
            {
                if (!StaticCached.BypassCache && !isComputingChildrenSizeDependencies && (AutoSizeAxes & Axes.Y) > 0)
                    updateChildrenSizeDependencies();
                return base.Height;
            }

            set
            {
                if ((AutoSizeAxes & Axes.Y) != 0)
                    throw new InvalidOperationException($"The height of a {nameof(CompositeDrawable)} with {nameof(AutoSizeAxes)} should only be manually set if it is relative to its parent.");
                base.Height = value;
            }
        }

        private bool isComputingChildrenSizeDependencies;

        public override Vector2 Size
        {
            get
            {
                if (!StaticCached.BypassCache && !isComputingChildrenSizeDependencies && AutoSizeAxes != Axes.None)
                    updateChildrenSizeDependencies();
                return base.Size;
            }

            set
            {
                if ((AutoSizeAxes & Axes.Both) != 0)
                    throw new InvalidOperationException($"The Size of a {nameof(CompositeDrawable)} with {nameof(AutoSizeAxes)} should only be manually set if it is relative to its parent.");
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
                foreach (Drawable c in AliveInternalChildren)
                {
                    if (!c.IsPresent)
                        continue;

                    Vector2 cBound = c.RequiredParentSizeToFit;

                    if ((c.BypassAutoSizeAxes & Axes.X) == 0)
                        maxBoundSize.X = Math.Max(maxBoundSize.X, cBound.X);

                    if ((c.BypassAutoSizeAxes & Axes.Y) == 0)
                        maxBoundSize.Y = Math.Max(maxBoundSize.Y, cBound.Y);
                }

                if ((AutoSizeAxes & Axes.X) == 0)
                    maxBoundSize.X = DrawSize.X;
                if ((AutoSizeAxes & Axes.Y) == 0)
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
                (AutoSizeAxes & Axes.X) > 0 ? b.X : base.Width,
                (AutoSizeAxes & Axes.Y) > 0 ? b.Y : base.Height
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

        private void autoSizeResizeTo(Vector2 newSize, double duration = 0, Easing easing = Easing.None) =>
            this.TransformTo(this.PopulateTransform(new AutoSizeTransform { Rewindable = false }, newSize, duration, easing));

        /// <summary>
        /// A helper property for <see cref="autoSizeResizeTo(Vector2, double, Easing)"/> to change the size of <see cref="CompositeDrawable"/>s with <see cref="AutoSizeAxes"/>.
        /// </summary>
        private Vector2 baseSize
        {
            get { return new Vector2(base.Width, base.Height); }

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
