// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Lists;
using System.Collections.Generic;
using System;
using System.Diagnostics;
using OpenTK;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.OpenGL;
using OpenTK.Graphics;
using osu.Framework.Graphics.Shaders;
using osu.Framework.Extensions.IEnumerableExtensions;
using osu.Framework.Graphics.Colour;
using osu.Framework.Allocation;
using osu.Framework.Graphics.Transforms;
using osu.Framework.Timing;
using osu.Framework.Caching;
using System.Threading.Tasks;

namespace osu.Framework.Graphics.Containers
{
    /// <summary>
    /// A drawable which can have children added to it. Transformations applied to
    /// a container are also applied to its children.
    /// Additionally, containers support various effects, such as masking, edge effect,
    /// padding, and automatic sizing depending on their children.
    /// If all children are of a specific non-<see cref="Drawable"/> type, use the
    /// generic version <see cref="Container{T}"/>.
    /// </summary>
    public class Container : Container<Drawable>
    {
    }

    /// <summary>
    /// A drawable which can have children added to it. Transformations applied to
    /// a container are also applied to its children.
    /// Additionally, containers support various effects, such as masking, edge effect,
    /// padding, and automatic sizing depending on their children.
    /// </summary>
    public class Container<T> : Drawable, IContainerEnumerable<T>, IContainerCollection<T>
        where T : Drawable
    {
        #region Contruction and disposal

        /// <summary>
        /// Contructs a container that stores its children in a given <see cref="LifetimeList{T}"/>.
        /// If null is provides, then a new <see cref="LifetimeList{T}"/> is automatically created.
        /// </summary>
        public Container(LifetimeList<T> lifetimeList = null)
        {
            internalChildren = lifetimeList ?? new LifetimeList<T>(DepthComparer);
            internalChildren.Removed += obj =>
            {
                if (obj.DisposeOnDeathRemoval) obj.Dispose();
            };
        }

        private Game game;

        protected Task LoadComponentAsync(Drawable component, Action<Drawable> onLoaded = null) => component.LoadAsync(game, this, onLoaded);

        [BackgroundDependencyLoader(true)]
        private void load(Game game, ShaderManager shaders)
        {
            this.game = game;

            if (shader == null)
                shader = shaders?.Load(VertexShaderDescriptor.TEXTURE_2, FragmentShaderDescriptor.TEXTURE_ROUNDED);

            // From now on, since we ourself are loaded now,
            // we actually permit children to be loaded if our
            // lifetimelist (internalChildren) requests a load.
            internalChildren.LoadRequested += loadChild;

            // We are in a potentially async context, so let's aggressively load all our children
            // regardless of their alive state. this also gives children a clock so they can be checked
            // for their correct alive state in the case LifetimeStart is set to a definite value.
            internalChildren.ForEach(loadChild);

            // Let's also perform an update on our LifetimeList to add any alive children.
            internalChildren.Update(Clock.TimeInfo);
        }

        private void loadChild(T child)
        {
            child.Load(Clock, Dependencies);
            child.Parent = this;
        }

        protected override void Dispose(bool isDisposing)
        {
            InternalChildren?.ForEach(c => c.Dispose());

            OnAutoSize = null;

            base.Dispose(isDisposing);
        }

        #endregion

        #region Children management

        /// <summary>
        /// The content of this container. <see cref="Children"/> and all methods that mutate
        /// <see cref="Children"/> (e.g. <see cref="Add(T)"/> and <see cref="Remove(T)"/>) are
        /// forwarded to the content. By default a container's content is itself, in which case
        /// <see cref="Children"/> refers to <see cref="InternalChildren"/>.
        /// This property is useful for containers that require internal children that should
        /// not be exposed to the outside world, e.g. <see cref="ScrollContainer"/>.
        /// </summary>
        protected virtual Container<T> Content => this;

        /// <summary>
        /// The publicly accessible list of children. Forwards to the children of <see cref="Content"/>.
        /// If <see cref="Content"/> is this container, then returns <see cref="InternalChildren"/>.
        /// </summary>
        public IEnumerable<T> Children
        {
            get { return Content != this ? Content.Children : internalChildren; }

            set
            {
                Clear();
                Add(value);
            }
        }

        private readonly LifetimeList<T> internalChildren;

        /// <summary>
        /// This container's own list of children.
        /// </summary>
        public IEnumerable<T> InternalChildren
        {
            get { return internalChildren; }

            set
            {
                Clear();
                AddInternal(value);
            }
        }

        protected IEnumerable<T> AliveInternalChildren => internalChildren.AliveItems;

        /// <summary>
        /// The index of a given child within <see cref="InternalChildren"/>.
        /// </summary>
        /// <returns>
        /// If the child is found, its index. Otherwise, the negated index it would obtain
        /// if it were added to <see cref="InternalChildren"/>.
        /// </returns>
        public int IndexOf(T drawable)
        {
            return internalChildren.IndexOf(drawable);
        }

        /// <summary>
        /// Checks whether a given child is contained within <see cref="InternalChildren"/>.
        /// </summary>
        public bool Contains(T drawable)
        {
            return IndexOf(drawable) >= 0;
        }

        /// <summary>
        /// Adds a child to this container. This amount to adding a child to <see cref="Content"/>'s
        /// <see cref="Children"/>, recursing until <see cref="Content"/> == this.
        /// </summary>
        public virtual void Add(T drawable)
        {
            if (drawable == Content)
                throw new InvalidOperationException("Content may not be added to itself.");

            if (Content == this)
                AddInternal(drawable);
            else
                Content.Add(drawable);
        }

        /// <summary>
        /// Adds a range of children. This is equivalent to calling <see cref="Add(T)"/> on
        /// each element of the range in order.
        /// </summary>
        public void Add(IEnumerable<T> range)
        {
            foreach (T d in range)
                Add(d);
        }

        /// <summary>
        /// Removes a given child from this container.
        /// </summary>
        public void Remove(T drawable)
        {
            if (drawable == null)
                throw new ArgumentNullException(nameof(drawable));

            if (Content != this)
            {
                Content.Remove(drawable);
                return;
            }

            if (!internalChildren.Remove(drawable))
                throw new InvalidOperationException($@"Cannot remove a drawable ({drawable}) which is not a child of this ({this}), but {drawable.Parent}.");

            // The string construction is quite expensive, so we are using Debug.Assert here.
            Debug.Assert(drawable.Parent == this, $@"Removed a drawable ({drawable}) whose parent was not this ({this}), but {drawable.Parent}.");

            drawable.Parent = null;

            if (AutoSizeAxes != Axes.None)
                InvalidateFromChild(Invalidation.Geometry);
        }

        /// <summary>
        /// Removes all children which match the given predicate.
        /// This is equivalent to calling <see cref="Remove(T)"/> for each child that
        /// matches the given predicate.
        /// </summary>
        /// <returns>The amount of removed children.</returns>
        public int RemoveAll(Predicate<T> pred)
        {
            List<T> toRemove = internalChildren.FindAll(pred);
            foreach (T removable in toRemove)
                Remove(removable);

            return toRemove.Count;
        }

        /// <summary>
        /// Removes a range of children. This is equivalent to calling <see cref="Remove(T)"/> on
        /// each element of the range in order.
        /// </summary>
        public void Remove(IEnumerable<T> range)
        {
            if (range == null)
                return;

            foreach (T p in range)
                Remove(p);
        }

        /// <summary>
        /// Removes all children.
        /// </summary>
        /// <param name="disposeChildren">Whether removed children should also get disposed.</param>
        public virtual void Clear(bool disposeChildren = true)
        {
            if (Content != null && Content != this)
            {
                Content.Clear(disposeChildren);
                return;
            }

            foreach (T t in internalChildren)
            {
                if (disposeChildren)
                {
                    //cascade disposal
                    (t as IContainer)?.Clear();
                    t.Dispose();
                }
                else
                    t.Parent = null;

                Trace.Assert(t.Parent == null);
            }

            internalChildren.Clear();

            if (AutoSizeAxes != Axes.None)
                InvalidateFromChild(Invalidation.Geometry);
        }

        /// <summary>
        /// Adds a child to <see cref="InternalChildren"/>.
        /// </summary>
        protected void AddInternal(T drawable)
        {
            if (drawable == null)
                throw new ArgumentNullException(nameof(drawable), "null Drawables may not be added to Containers.");
            if (drawable == this)
                throw new InvalidOperationException("Container may not be added to itself.");

            if (drawable.IsLoaded)
                drawable.Parent = this;

            internalChildren.Add(drawable);

            if (AutoSizeAxes != Axes.None)
                InvalidateFromChild(Invalidation.Geometry);
        }

        /// <summary>
        /// Adds a range of children to <see cref="InternalChildren"/>. This is equivalent to calling
        /// <see cref="AddInternal(T)"/> on each element of the range in order.
        /// </summary>
        protected void AddInternal(IEnumerable<T> range)
        {
            foreach (T d in range)
                AddInternal(d);
        }

        #endregion

        #region Updating (per-frame periodic)

        /// <summary>
        /// Updates the life status of <see cref="InternalChildren"/> according to their
        /// <see cref="IHasLifetime.IsAlive"/> property.
        /// </summary>
        /// <returns>True iff the life status of at least one child changed.</returns>
        protected virtual bool UpdateChildrenLife()
        {
            if (internalChildren.Update(Time))
            {
                childrenSizeDependencies.Invalidate();
                return true;
            }

            return false;
        }

        public sealed override void UpdateClock(IFrameBasedClock clock)
        {
            if (Clock == clock)
                return;

            base.UpdateClock(clock);
            foreach (T child in InternalChildren)
                child.UpdateClock(Clock);
        }

        /// <summary>
        /// Specifies whether this Container requires an update of its children.
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

            foreach (T child in internalChildren.AliveItems)
                if (child.IsLoaded) child.UpdateSubTree();

            UpdateAfterChildren();

            updateChildrenSizeDependencies();
            return true;
        }

        /// <summary>
        /// An opportunity to update state once-per-frame after <see cref="Drawable.Update"/> has been called
        /// for all <see cref="InternalChildren"/>.
        /// </summary>
        protected virtual void UpdateAfterChildren()
        {
        }

        #endregion

        #region Caching & invalidation

        private Cached<Quad> screenSpaceEdgeEffectQuadBacking = new Cached<Quad>();

        /// <summary>
        /// Computes the screen space quad which the edge effect occupies.
        /// </summary>
        /// <returns></returns>
        protected Quad ComputeScreenSpaceEdgeEffectQuad() => ToScreenSpace(EdgeEffectRectangle);

        /// <summary>
        /// The screen space quad which the edge effect occupies.
        /// </summary>
        public Quad ScreenSpaceEdgeEffectQuad
        {
            get
            {
                if (!edgeEffectVisible)
                    return ScreenSpaceDrawQuad;

                return screenSpaceEdgeEffectQuadBacking.EnsureValid()
                    ? screenSpaceEdgeEffectQuadBacking.Value
                    : screenSpaceEdgeEffectQuadBacking.Refresh(ComputeScreenSpaceEdgeEffectQuad);
            }
        }

        /// <summary>
        /// Informs this container that a child has been invalidated.
        /// </summary>
        /// <param name="invalidation">The type of invalidation applied to the child.</param>
        public virtual void InvalidateFromChild(Invalidation invalidation)
        {
            //Colour captures potential changes in IsPresent. If this ever becomes a bottleneck,
            //Invalidation could be further separated into presence changes.
            if ((invalidation & (Invalidation.Geometry | Invalidation.Colour)) > 0)
                childrenSizeDependencies.Invalidate();
        }

        public override bool Invalidate(Invalidation invalidation = Invalidation.All, Drawable source = null, bool shallPropagate = true)
        {
            bool alreadyInvalidated = true;

            if ((invalidation & (Invalidation.Geometry | Invalidation.DrawNode)) > 0)
                alreadyInvalidated &= !screenSpaceEdgeEffectQuadBacking.Invalidate();

            if (!base.Invalidate(invalidation, source, shallPropagate))
                return !alreadyInvalidated;

            if (!shallPropagate) return true;

            // This way of looping turns out to be slightly faster than a foreach
            // or directly indexing a SortedList<T>. This part of the code is often
            // hot, so an optimization like this makes sense here.
            List<T> current = internalChildren;
            // ReSharper disable once ForCanBeConvertedToForeach
            for (int i = 0; i < current.Count; ++i)
            {
                T c = current[i];
                Debug.Assert(c != source);

                Invalidation childInvalidation = invalidation;
                if (c.RelativeSizeAxes == Axes.None)
                    childInvalidation = childInvalidation & ~Invalidation.SizeInParentSpace;

                c.Invalidate(childInvalidation, this);
            }

            return true;
        }

        #endregion

        #region DrawNode

        private readonly ContainerDrawNodeSharedData containerDrawNodeSharedData = new ContainerDrawNodeSharedData();
        private Shader shader;

        protected override DrawNode CreateDrawNode() => new ContainerDrawNode();

        protected override void ApplyDrawNode(DrawNode node)
        {
            ContainerDrawNode n = (ContainerDrawNode)node;

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

            n.EdgeEffectInfo = !edgeEffectVisible
                ? (EdgeEffectInfo?)null
                : new EdgeEffectInfo
                {
                    Effect = EdgeEffect,
                    MaskingRect = EdgeEffectRectangle,
                    ScreenSpaceDrawQuad = ScreenSpaceEdgeEffectQuad
                };

            n.Shared = containerDrawNodeSharedData;
            n.Shader = shader;

            base.ApplyDrawNode(node);
        }

        protected virtual bool CanBeFlattened => !Masking;

        private const int amount_children_required_for_masking_check = 2;

        /// <summary>
        /// This function adds all children's DrawNodes to a target List, flattening the children of certain types
        /// of container subtrees for optimization purposes.
        /// </summary>
        /// <param name="treeIndex">The index of the currently in-use DrawNode tree.</param>
        /// <param name="j">The running index into the target List.</param>
        /// <param name="parentContainer">The container whose children's DrawNodes to add.</param>
        /// <param name="target">The target list to fill with DrawNodes.</param>
        /// <param name="maskingBounds">The masking bounds. Children lying outside of them should be ignored.</param>
        private static void addFromContainer(int treeIndex, ref int j, Container<T> parentContainer, List<DrawNode> target, RectangleF maskingBounds)
        {
            List<T> current = parentContainer.internalChildren.AliveItems;
            // ReSharper disable once ForCanBeConvertedToForeach
            for (int i = 0; i < current.Count; ++i)
            {
                Drawable drawable = current[i];

                // If we are proxied somewhere, then we want to be drawn at the proxy's location
                // in the scene graph, rather than at our own location, thus no draw nodes for us.
                if (drawable.HasProxy)
                    continue;

                // Take drawable.Original until drawable.Original == drawable
                while (drawable != (drawable = drawable.Original))
                {
                }

                if (!drawable.IsPresent)
                    continue;

                // We are consciously missing out on potential flattening (due to lack of covariance)
                // in order to be able to let this loop be over integers instead of using
                // IContainerEnumerable<Drrawable>.AliveChildren which measures to be a _major_ slowdown.
                Container<T> container = drawable as Container<T>;
                if (container?.CanBeFlattened == true)
                {
                    // The masking check is overly expensive (requires creation of ScreenSpaceDrawQuad)
                    // when only few children exist.
                    container.IsMaskedAway = container.internalChildren.AliveItems.Count >= amount_children_required_for_masking_check &&
                                             !maskingBounds.IntersectsWith(drawable.ScreenSpaceDrawQuad.AABBFloat);

                    if (!container.IsMaskedAway)
                        addFromContainer(treeIndex, ref j, container, target, maskingBounds);

                    continue;
                }

                drawable.IsMaskedAway = !maskingBounds.IntersectsWith(drawable.ScreenSpaceDrawQuad.AABBFloat);
                if (drawable.IsMaskedAway)
                    continue;

                DrawNode next = drawable.GenerateDrawNodeSubtree(treeIndex, maskingBounds);
                if (next == null)
                    continue;

                if (j < target.Count)
                    target[j] = next;
                else
                    target.Add(next);

                j++;
            }
        }

        internal sealed override DrawNode GenerateDrawNodeSubtree(int treeIndex, RectangleF bounds)
        {
            // No need for a draw node at all if there are no children and we are not glowing.
            if (internalChildren.AliveItems.Count == 0 && CanBeFlattened)
                return null;

            ContainerDrawNode cNode = base.GenerateDrawNodeSubtree(treeIndex, bounds) as ContainerDrawNode;
            if (cNode == null)
                return null;

            RectangleF childBounds = bounds;
            // If we are going to render a buffered container we need to make sure no children get masked away,
            // even if they are off-screen.
            if (this is BufferedContainer)
                childBounds = ScreenSpaceDrawQuad.AABBFloat;
            else if (Masking)
                childBounds.Intersect(ScreenSpaceDrawQuad.AABBFloat);

            if (cNode.Children == null)
                cNode.Children = new List<DrawNode>(internalChildren.AliveItems.Count);

            List<DrawNode> target = cNode.Children;

            int j = 0;
            addFromContainer(treeIndex, ref j, this, target, childBounds);

            if (j < target.Count)
                target.RemoveRange(j, target.Count - j);

            return cNode;
        }

        #endregion

        #region Transforms

        public override void ClearTransforms(bool propagateChildren = false)
        {
            base.ClearTransforms(propagateChildren);

            if (propagateChildren)
                foreach (var c in internalChildren) c.ClearTransforms(true);
        }

        public override Drawable Delay(double duration, bool propagateChildren = false)
        {
            if (duration == 0) return this;

            base.Delay(duration, propagateChildren);

            if (propagateChildren)
                foreach (var c in internalChildren) c.Delay(duration, true);
            return this;
        }

        public override void Flush(bool propagateChildren = false, Type flushType = null)
        {
            base.Flush(propagateChildren, flushType);

            if (propagateChildren)
                foreach (var c in internalChildren) c.Flush(true, flushType);
        }

        public override Drawable DelayReset()
        {
            base.DelayReset();
            foreach (var c in internalChildren) c.DelayReset();

            return this;
        }

        /// <summary>
        /// Helper function for creating and adding a <see cref="Transform{T}"/> that fades the current <see cref="EdgeEffect"/>.
        /// </summary>
        public void FadeEdgeEffectTo(float newAlpha, double duration = 0, EasingTypes easing = EasingTypes.None)
        {
            TransformTo(() => EdgeEffect.Colour.Linear.A, newAlpha, duration, easing, new TransformEdgeEffectAlpha());
        }

        #endregion

        #region Interaction / Input

        // Required to pass through input to children by default.
        // TODO: Evaluate effects of this on performance and address.
        public override bool HandleInput => true;

        protected override bool InternalContains(Vector2 screenSpacePos)
        {
            float cRadius = CornerRadius;

            // Select a cheaper contains method when we don't need rounded edges.
            if (cRadius == 0.0f)
                return base.InternalContains(screenSpacePos);
            return DrawRectangle.Shrink(cRadius).DistanceSquared(ToLocalSpace(screenSpacePos)) <= cRadius * cRadius;
        }

        internal override bool BuildKeyboardInputQueue(List<Drawable> queue)
        {
            if (!base.BuildKeyboardInputQueue(queue))
                return false;

            //don't use AliveInternalChildren here as it will cause too many allocations (IEnumerable).
            foreach (T d in internalChildren.AliveItems)
                d.BuildKeyboardInputQueue(queue);

            return true;
        }

        internal override bool BuildMouseInputQueue(Vector2 screenSpaceMousePos, List<Drawable> queue)
        {
            if (!base.BuildMouseInputQueue(screenSpaceMousePos, queue))
                return false;

            //don't use AliveInternalChildren here as it will cause too many allocations (IEnumerable).
            foreach (T d in internalChildren.AliveItems)
                d.BuildMouseInputQueue(screenSpaceMousePos, queue);

            return true;
        }

        #endregion

        #region Masking and related effects (e.g. round corners)

        private bool masking;

        /// <summary>
        /// If enabled, only the portion of children that falls within this container's
        /// shape is drawn to the screen.
        /// </summary>
        public bool Masking
        {
            get { return masking; }
            set
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
            set
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
        public virtual float CornerRadius
        {
            get { return cornerRadius; }
            set
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
            set
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
        public virtual SRGBColour BorderColour
        {
            get { return borderColour; }
            set
            {
                if (borderColour.Equals(value))
                    return;

                borderColour = value;
                Invalidate(Invalidation.DrawNode);
            }
        }

        private EdgeEffect edgeEffect;

        /// <summary>
        /// Determines an edge effect of this container.
        /// Edge effects are e.g. glow or a shadow.
        /// Only has an effect when <see cref="Masking"/> is true.
        /// </summary>
        public virtual EdgeEffect EdgeEffect
        {
            get { return edgeEffect; }
            set
            {
                if (edgeEffect.Equals(value))
                    return;

                edgeEffect = value;
                Invalidate(Invalidation.DrawNode);
            }
        }

        /// <summary>
        /// The rectangle which the edge effect occupies.
        /// </summary>
        public RectangleF EdgeEffectRectangle => DrawRectangle.Inflate(EdgeEffect.Radius).Offset(EdgeEffect.Offset);

        /// <summary>
        /// Whether the edge effect produces a visible affect.
        /// </summary>
        private bool edgeEffectVisible => Masking && EdgeEffect.Type != EdgeEffectType.None && EdgeEffect.Radius > 0 && EdgeEffect.Colour.Linear.A > 0;

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

        public MarginPadding Padding
        {
            get { return padding; }
            set
            {
                if (padding.Equals(value)) return;

                padding = value;
                padding.ThrowIfNegative();

                foreach (T c in internalChildren)
                    c.Invalidate(Invalidation.Geometry);
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

        private Vector2 relativeCoordinateSpace = Vector2.One;
        /// <summary>
        /// The coordinate space revealed to relatively-sized or positioned children such that changing this to
        /// values (x_1, y_1) will require a child to have a relative size of (x_1, y_1) to fill the container.
        /// <para>
        /// Note that relative size children will have a (1, 1) size by default.
        /// </para>
        /// </summary>
        public Vector2 RelativeCoordinateSpace
        {
            get { return relativeCoordinateSpace; }
            set
            {
                if (relativeCoordinateSpace == value)
                    return;
                relativeCoordinateSpace = value;

                foreach (T c in internalChildren)
                    c.Invalidate(Invalidation.Geometry);
            }
        }

        /// <summary>
        /// Conversion factor from relative to absolute coordinates in our space.
        /// </summary>
        public Vector2 RelativeToAbsoluteFactor => Vector2.Divide(ChildSize, RelativeCoordinateSpace);

        /// <summary>
        /// Tweens the RelativeCoordinateSpace of this container.
        /// </summary>
        /// <param name="newCoordinateSpace">The coordinate space to tween to.</param>
        /// <param name="duration">The tween duration.</param>
        /// <param name="easing">The tween easing.</param>
        public void TransformRelativeCoordinateSpaceTo(Vector2 newCoordinateSpace, double duration = 0, EasingTypes easing = EasingTypes.None)
        {
            TransformTo(() => RelativeCoordinateSpace, newCoordinateSpace, duration, easing, new TransformRelativeCoordinateSpace());
        }

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
        public Axes AutoSizeAxes
        {
            get { return autoSizeAxes; }
            set
            {
                if (value == autoSizeAxes)
                    return;

                if ((RelativeSizeAxes & value) != 0)
                    throw new InvalidOperationException("No axis can be relatively sized and automatically sized at the same time.");

                autoSizeAxes = value;
                childrenSizeDependencies.Invalidate();
            }
        }

        private float autoSizeDuration;

        /// <summary>
        /// The duration which automatic sizing should take. If zero, then it is instantaneous.
        /// Otherwise, this is equivalent to applying an automatic size via <see cref="Drawable.ResizeTo(Vector2, double, EasingTypes)"/>.
        /// </summary>
        public float AutoSizeDuration
        {
            get { return autoSizeDuration; }
            set
            {
                if (autoSizeDuration == value) return;

                autoSizeDuration = value;

                // if we have an existing transform, we want to update its duration.
                // not doing this could potentially cause incorrect final autosize dimensions.
                var existing = Transforms.Find(t => t is TransformAutoSize);
                if (existing != null)
                    existing.EndTime = existing.StartTime + autoSizeDuration;
            }
        }

        /// <summary>
        /// The type of easing which should be used for smooth automatic sizing when <see cref="AutoSizeDuration"/>
        /// is non-zero.
        /// </summary>
        public EasingTypes AutoSizeEasing;

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
                    throw new InvalidOperationException("The width of an AutoSizeContainer should only be manually set if it is relative to its parent.");
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
                    throw new InvalidOperationException("The height of an AutoSizeContainer should only be manually set if it is relative to its parent.");
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
                    throw new InvalidOperationException("The Size of an AutoSizeContainer should only be manually set if it is relative to its parent.");
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
                foreach (T c in AliveInternalChildren)
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

            if (AutoSizeDuration > 0)
                autoSizeResizeTo(new Vector2(
                    (AutoSizeAxes & Axes.X) > 0 ? b.X : base.Width,
                    (AutoSizeAxes & Axes.Y) > 0 ? b.Y : base.Height
                ), AutoSizeDuration, AutoSizeEasing);
            else
            {
                if ((AutoSizeAxes & Axes.X) > 0) base.Width = b.X;
                if ((AutoSizeAxes & Axes.Y) > 0) base.Height = b.Y;
            }

            //note that this is called before autoSize becomes valid. may be something to consider down the line.
            //might work better to add an OnRefresh event in Cached<> and invoke there.
            OnAutoSize?.Invoke();
        }

        private void updateChildrenSizeDependencies()
        {
            isComputingChildrenSizeDependencies = true;

            try
            {
                if (!childrenSizeDependencies.EnsureValid())
                    childrenSizeDependencies.Refresh(updateAutoSize);
            }
            finally
            {
                isComputingChildrenSizeDependencies = false;
            }
        }

        private void autoSizeResizeTo(Vector2 newSize, double duration = 0, EasingTypes easing = EasingTypes.None)
        {
            TransformTo(() => Size, newSize, duration, easing, new TransformAutoSize());
        }

        /// <summary>
        /// A helper method for <see cref="TransformAutoSize"/> to change the size of auto size containers.
        /// </summary>
        /// <param name="newSize"></param>
        private void setBaseSize(Vector2 newSize)
        {
            base.Width = newSize.X;
            base.Height = newSize.Y;
        }

        /// <summary>
        /// A special type of transform which can change the size of auto size containers.
        /// Used for <see cref="AutoSizeDuration"/>.
        /// </summary>
        private class TransformAutoSize : TransformVector
        {
            public override void Apply(Drawable d)
            {
                base.Apply(d);

                var c = (Container<T>)d;
                c.setBaseSize(CurrentValue);
            }
        }

        #endregion

        private class TransformRelativeCoordinateSpace : TransformVector
        {
            public override void Apply(Drawable d)
            {
                base.Apply(d);

                var c = (Container<T>)d;
                c.RelativeCoordinateSpace = CurrentValue;
            }
        }
    }

    public struct EdgeEffectInfo : IEquatable<EdgeEffectInfo>
    {
        public EdgeEffect Effect;
        public RectangleF MaskingRect;
        public Quad ScreenSpaceDrawQuad;

        public bool Equals(EdgeEffectInfo other)
        {
            return Effect.Equals(other.Effect)
                   && MaskingRect.Equals(other.MaskingRect)
                   && ScreenSpaceDrawQuad.Equals(other.ScreenSpaceDrawQuad);
        }
    }
}
