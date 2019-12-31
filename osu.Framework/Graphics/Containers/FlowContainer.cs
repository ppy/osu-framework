﻿// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osuTK;
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
            get => AutoSizeEasing;
            set => AutoSizeEasing = value;
        }

        /// <summary>
        /// The time it should take to move a child from its current position to its new layout position.
        /// </summary>
        public float LayoutDuration
        {
            get => AutoSizeDuration * 2;
            set => AutoSizeDuration = value / 2;
        }

        private Vector2 maximumSize;

        /// <summary>
        /// Optional maximum dimensions for this container. Note that the meaning of this value can change
        /// depending on the implementation.
        /// </summary>
        public Vector2 MaximumSize
        {
            get => maximumSize;
            set
            {
                if (maximumSize == value) return;

                maximumSize = value;
                Invalidate(Invalidation.DrawSize);
            }
        }

        private readonly Cached layout = new Cached();

        protected override bool RequiresChildrenUpdate => base.RequiresChildrenUpdate || !layout.IsValid;

        /// <summary>
        /// Invoked when layout should be invalidated.
        /// </summary>
        protected virtual void InvalidateLayout() => layout.Invalidate();

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
        /// Changes the position of the drawable in the layout. A higher position value means the drawable will be processed later (that is, the drawables with the lowest position appear first, and the drawable with the highest position appear last).
        /// For example, the drawable with the lowest position value will be the left-most drawable in a horizontal <see cref="FillFlowContainer{T}"/> and the drawable with the highest position value will be the right-most drawable in a horizontal <see cref="FillFlowContainer{T}"/>.
        /// </summary>
        /// <param name="drawable">The drawable whose position should be changed, must be a child of this container.</param>
        /// <param name="newPosition">The new position in the layout the drawable should have.</param>
        public void SetLayoutPosition(Drawable drawable, float newPosition)
        {
            if (!layoutChildren.ContainsKey(drawable))
                throw new InvalidOperationException($"Cannot change layout position of drawable which is not contained within this {nameof(FlowContainer<T>)}.");

            layoutChildren[drawable] = newPosition;
            InvalidateLayout();
        }

        /// <summary>
        /// Inserts a new drawable at the specified layout position.
        /// </summary>
        /// <param name="position">The layout position of the new child.</param>
        /// <param name="drawable">The drawable to be inserted.</param>
        public void Insert(int position, T drawable)
        {
            Add(drawable);
            SetLayoutPosition(drawable, position);
        }

        /// <summary>
        /// Gets the position of the drawable in the layout. A higher position value means the drawable will be processed later (that is, the drawables with the lowest position appear first, and the drawable with the highest position appear last).
        /// For example, the drawable with the lowest position value will be the left-most drawable in a horizontal <see cref="FillFlowContainer{T}"/> and the drawable with the highest position value will be the right-most drawable in a horizontal <see cref="FillFlowContainer{T}"/>.
        /// </summary>
        /// <param name="drawable">The drawable whose position should be retrieved, must be a child of this container.</param>
        /// <returns>The position of the drawable in the layout.</returns>
        public float GetLayoutPosition(Drawable drawable)
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

        public override void InvalidateFromChild(Invalidation invalidation, Drawable source = null)
        {
            if ((invalidation & (Invalidation.RequiredParentSizeToFit | Invalidation.Presence)) > 0)
                InvalidateLayout();

            base.InvalidateFromChild(invalidation, source);
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
                    break;

                if (d.RelativePositionAxes != Axes.None)
                    throw new InvalidOperationException($"A flow container cannot contain a child with relative positioning (it is {d.RelativePositionAxes}).");

                var finalPos = positions[i];

                var existingTransform = d.Transforms.OfType<FlowTransform>().FirstOrDefault();
                Vector2 currentTargetPos = existingTransform?.EndValue ?? d.Position;

                if (currentTargetPos != finalPos)
                {
                    if (LayoutDuration > 0)
                        d.TransformTo(d.PopulateTransform(new FlowTransform { Rewindable = false }, finalPos, LayoutDuration, LayoutEasing));
                    else
                    {
                        if (existingTransform != null) d.ClearTransforms(false, nameof(FlowTransform));
                        d.Position = finalPos;
                    }
                }

                ++i;
            }

            if (i != positions.Length)
            {
                throw new InvalidOperationException(
                    $"{GetType().FullName}.{nameof(ComputeLayoutPositions)} returned a total of {positions.Length} positions for {i} children. {nameof(ComputeLayoutPositions)} must return 1 position per child.");
            }
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
