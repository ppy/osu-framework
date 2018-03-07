// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using OpenTK;
using osu.Framework.Caching;
using System;
using System.Collections.Generic;
using System.Linq;
using osu.Framework.Graphics.Transforms;

namespace osu.Framework.Graphics.Containers
{
    /// <summary>
    /// A container that can be used to fluently arrange its children.
    /// </summary>
    public abstract class FlowContainer<T> : Container<T>
        where T : Drawable
    {
        internal event Action OnLayout;

        /// <summary>
        /// The easing that should be used when children are moved to their position in the layout.
        /// </summary>
        public Easing LayoutEasing
        {
            get { return AutoSizeEasing; }
            set { AutoSizeEasing = value; }
        }

        /// <summary>
        /// The time it should take to move a child from its current position to its new layout position.
        /// </summary>
        public float LayoutDuration
        {
            get { return AutoSizeDuration * 2; }
            set
            {
                //coupling with autosizeduration allows us to smoothly transition our size
                //when no children are left to dictate autosize.
                AutoSizeDuration = value / 2;
            }
        }

        private Cached layout = new Cached();

        protected void InvalidateLayout() => layout.Invalidate();

        private Vector2 maximumSize;

        /// <summary>
        /// Optional maximum dimensions for this container. Note that the meaning of this value can change
        /// depending on the implementation.
        /// </summary>
        public Vector2 MaximumSize
        {
            get { return maximumSize; }
            set
            {
                if (maximumSize == value) return;

                maximumSize = value;
                Invalidate(Invalidation.DrawSize);
            }
        }

        protected override bool RequiresChildrenUpdate => base.RequiresChildrenUpdate || !layout.IsValid;

        public override bool Invalidate(Invalidation invalidation = Invalidation.All, Drawable source = null, bool shallPropagate = true)
        {
            if ((invalidation & Invalidation.DrawSize) > 0)
                InvalidateLayout();

            return base.Invalidate(invalidation, source, shallPropagate);
        }

        private readonly Dictionary<Drawable, float> layoutChildren = new Dictionary<Drawable, float>();

        protected internal override void AddInternal(Drawable drawable)
        {
            layoutChildren.Add(drawable, 0f);
            // we have to ensure that the layout gets invalidated since Adding or Removing a child will affect the layout. The base class will not invalidate
            // if we are set to AutoSizeAxes.None, but even in that situation, the layout can and often does change when children are added/removed.
            InvalidateLayout();
            base.AddInternal(drawable);
        }

        protected internal override bool RemoveInternal(Drawable drawable)
        {
            layoutChildren.Remove(drawable);
            // we have to ensure that the layout gets invalidated since Adding or Removing a child will affect the layout. The base class will not invalidate
            // if we are set to AutoSizeAxes.None, but even in that situation, the layout can and often does change when children are added/removed.
            InvalidateLayout();
            return base.RemoveInternal(drawable);
        }

        protected internal override void ClearInternal(bool disposeChildren = true)
        {
            layoutChildren.Clear();
            // we have to ensure that the layout gets invalidated since Adding or Removing a child will affect the layout. The base class will not invalidate
            // if we are set to AutoSizeAxes.None, but even in that situation, the layout can and often does change when children are added/removed.
            InvalidateLayout();
            base.ClearInternal(disposeChildren);
        }

        /// <summary>
        /// Adds a child at a specific layout position within <see cref="FlowContainer{T}.Content"/>.
        /// This amounts to adding a child to <see cref="FlowContainer{T}.Content"/>'s <see cref="FlowContainer{T}.Children"/>, recursing until <see cref="FlowContainer{T}.Content"/> == this.
        /// </summary>
        /// <param name="drawable">The <see cref="Drawable"/> to add.</param>
        /// <param name="layoutPosition">The position of <paramref name="drawable"/> in the layout of <see cref="Container.Content"/>.</param>
        public void Add(T drawable, float layoutPosition)
        {
            if (Content == this)
                AddInternal(drawable, layoutPosition);
            else if (Content is FlowContainer<T> tFlowContent)
                tFlowContent.Add(drawable, layoutPosition);
            else
                Content.Add(drawable);
        }

        /// <summary>
        /// Adds a child at a specific layout position within this <see cref="FlowContainer{T}"/>.
        /// </summary>
        /// <param name="drawable">The <see cref="Drawable"/> to add.</param>
        /// <param name="layoutPosition">The position of <paramref name="drawable"/> in the layout of this <see cref="FlowContainer{T}"/>.</param>
        protected internal void AddInternal(Drawable drawable, float layoutPosition)
        {
            AddInternal(drawable);
            SetInternalLayoutPosition(drawable, layoutPosition);
        }

        /// <summary>
        /// Changes the layout position of a <see cref="Drawable"/> within <see cref="FlowContainer{T}.Content"/>.
        /// This amounts to setting the layout position of <paramref name="drawable"/> in <see cref="FlowContainer{T}.Content"/>, recursing until <see cref="FlowContainer{T}.Content"/> == this.
        /// A lower layout position indicates that <paramref name="drawable"/> will appear closer to the anchor position of <see cref="FlowContainer{T}.Children"/>.
        /// A higher layout position indicates that <paramref name="drawable"/> will appear further from the anchor position of <see cref="FlowContainer{T}.Children"/>
        /// </summary>
        /// <param name="drawable">The <see cref="Drawable"/> whose layout position should be changed, must be a child of this <see cref="FlowContainer{T}"/>.</param>
        /// <param name="newPosition">The new layout position <paramref name="drawable"/> should have.</param>
        public void SetLayoutPosition(Drawable drawable, float newPosition)
        {
            if (Content == this)
                SetInternalLayoutPosition(drawable, newPosition);
            else if (Content is FlowContainer<T> tFlowContent)
                tFlowContent.SetLayoutPosition(drawable, newPosition);
        }

        /// <summary>
        /// Changes the layout position of a <see cref="Drawable"/> in this <see cref="FlowContainer{T}"/>.
        /// A lower layout position indicates that <paramref name="drawable"/> will appear closer to the anchor position of <see cref="FlowContainer{T}.Children"/>.
        /// A higher layout position indicates that <paramref name="drawable"/> will appear further from the anchor position of <see cref="FlowContainer{T}.Children"/>
        /// </summary>
        /// <param name="drawable">The <see cref="Drawable"/> whose layout position should be changed, must be a child of this <see cref="FlowContainer{T}"/>.</param>
        /// <param name="newPosition">The new layout position <paramref name="drawable"/> should have.</param>
        protected internal void SetInternalLayoutPosition(Drawable drawable, float newPosition)
        {
            if (!layoutChildren.ContainsKey(drawable))
                throw new InvalidOperationException($"Cannot change layout position of drawable which is not contained within this {nameof(FlowContainer<T>)}.");
            layoutChildren[drawable] = newPosition;
            InvalidateLayout();
        }

        /// <summary>
        /// Gets the layout position of a <see cref="Drawable"/> within <see cref="FlowContainer{T}.Content"/>.
        /// This amounts to retrieving the layout position of <paramref name="drawable"/> from <see cref="FlowContainer{T}.Content"/>, recursing until <see cref="FlowContainer{T}.Content"/> == this.
        /// A lower layout position indicates that <paramref name="drawable"/> will appear closer to the anchor position of <see cref="FlowContainer{T}.Children"/>.
        /// A higher layout position indicates that <paramref name="drawable"/> will appear further from the anchor position of <see cref="FlowContainer{T}.Children"/>
        /// </summary>
        /// <param name="drawable">The <see cref="Drawable"/> whose layout position should be retrieved.</param>
        /// <returns>The layout position of <paramref name="drawable"/>.</returns>
        public float GetLayoutPosition(Drawable drawable)
        {
            if (Content == this)
                return GetInternalLayoutPosition(drawable);
            if (Content is FlowContainer<T> tFlowContent)
                return tFlowContent.GetLayoutPosition(drawable);
            return 0;
        }

        /// <summary>
        /// Gets the layout position of a <see cref="Drawable"/> in this <see cref="FlowContainer{T}"/>..
        /// A lower layout position indicates that <paramref name="drawable"/> will appear closer to the anchor position of <see cref="FlowContainer{T}.Children"/>.
        /// A higher layout position indicates that <paramref name="drawable"/> will appear further from the anchor position of <see cref="FlowContainer{T}.Children"/>
        /// </summary>
        /// <param name="drawable">The <see cref="Drawable"/> whose layout position should be retrieved, must be a child of this <see cref="FlowContainer{T}"/>.</param>
        /// <returns>The layout position of <paramref name="drawable"/>.</returns>
        protected internal float GetInternalLayoutPosition(Drawable drawable)
        {
            if (!layoutChildren.ContainsKey(drawable))
                throw new InvalidOperationException($"Cannot get layout position of drawable which is not contained within this {nameof(FlowContainer<T>)}.");
            return layoutChildren[drawable];
        }

        protected override bool UpdateChildrenLife()
        {
            bool changed = base.UpdateChildrenLife();

            if (changed)
                InvalidateLayout();

            return changed;
        }

        public override void InvalidateFromChild(Invalidation invalidation)
        {
            //Colour captures potential changes in IsPresent. If this ever becomes a bottleneck,
            //Invalidation could be further separated into presence changes.
            if ((invalidation & (Invalidation.RequiredParentSizeToFit | Invalidation.Colour)) > 0)
                InvalidateLayout();

            base.InvalidateFromChild(invalidation);
        }

        /// <summary>
        /// Gets the children that appear in the flow of this <see cref="FlowContainer{T}"/> in the order in which they are processed within the flowing layout.
        /// </summary>
        public virtual IEnumerable<Drawable> FlowingChildren => AliveInternalChildren.Where(d => d.IsPresent).OrderBy(d => layoutChildren[d]).ThenBy(d => d.ChildID);

        protected abstract IEnumerable<Vector2> ComputeLayoutPositions();

        private void performLayout()
        {
            OnLayout?.Invoke();

            if (!Children.Any())
                return;

            var positions = ComputeLayoutPositions().ToArray();

            int i = 0;
            foreach (var d in FlowingChildren)
            {
                if (i > positions.Length)
                    throw new InvalidOperationException(
                        $"{GetType().FullName}.{nameof(ComputeLayoutPositions)} returned a total of {positions.Length} positions for {i} children. {nameof(ComputeLayoutPositions)} must return 1 position per child.");

                // In some cases (see the right hand side of the conditional) we want to permit relatively sized children
                // in our flow direction; specifically, when children use FillMode.Fit to preserve the aspect ratio.
                // Consider the following use case: A flow container has a fixed width but an automatic height, and flows
                // in the vertical direction. Now, we can add relatively sized children with FillMode.Fit to make sure their
                // aspect ratio is preserved while still allowing them to flow vertically. This special case can not result
                // in an autosize-related feedback loop, and we can thus simply allow it.
                if ((d.RelativeSizeAxes & AutoSizeAxes) != 0 && (d.FillMode != FillMode.Fit || d.RelativeSizeAxes != Axes.Both || d.Size.X > RelativeChildSize.X || d.Size.Y > RelativeChildSize.Y || AutoSizeAxes == Axes.Both))
                    throw new InvalidOperationException(
                        "Drawables inside a flow container may not have a relative size axis that the flow container is auto sizing for." +
                        $"The flow container is set to autosize in {AutoSizeAxes} axes and the child is set to relative size in {d.RelativeSizeAxes} axes.");

                if (d.RelativePositionAxes != Axes.None)
                    throw new InvalidOperationException($"A flow container cannot contain a child with relative positioning (it is {d.RelativePositionAxes}).");

                var finalPos = positions[i];
                if (d.Position != finalPos)
                    d.TransformTo(d.PopulateTransform(new FlowTransform { Rewindable = false }, finalPos, LayoutDuration, LayoutEasing));

                ++i;
            }

            if (i != positions.Length)
                throw new InvalidOperationException(
                    $"{GetType().FullName}.{nameof(ComputeLayoutPositions)} returned a total of {positions.Length} positions for {i} children. {nameof(ComputeLayoutPositions)} must return 1 position per child.");
        }

        protected override void UpdateAfterChildren()
        {
            base.UpdateAfterChildren();

            if (!layout.IsValid)
            {
                performLayout();
                layout.Validate();
            }
        }

        private class FlowTransform : TransformCustom<Vector2, Drawable>
        {
            public FlowTransform()
                : base(nameof(Position))
            {
            }
        }
    }
}
