// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Framework.Caching;
using osu.Framework.Graphics.Transforms;
using OpenTK;

namespace osu.Framework.Graphics.Containers
{
    /// <summary>
    /// A container that can be used to fluently arrange its children according to a specific <see cref="IFlowStrategy"/>.
    /// </summary>
    public class FlowContainer : FlowContainer<Drawable>
    { }

    /// <summary>
    /// A container that can be used to fluently arrange its children according to a specific <see cref="IFlowStrategy"/>.
    /// </summary>
    public class FlowContainer<T> : Container<T>
        where T : Drawable
    {
        internal event Action OnLayout;

        /// <summary>
        /// The easing that should be used when children are moved to their position in the layout.
        /// </summary>
        public EasingTypes LayoutEasing
        {
            get
            {
                return AutoSizeEasing;
            }
            set
            {
                AutoSizeEasing = value;
            }
        }

        /// <summary>
        /// The time it should take to move a child from its current position to its new layout position.
        /// </summary>
        public float LayoutDuration
        {
            get
            {
                return AutoSizeDuration * 2;
            }
            set
            {
                //coupling with autosizeduration allows us to smoothly transition our size
                //when no children are left to dictate autosize.
                AutoSizeDuration = value / 2;
            }
        }

        private Cached layout = new Cached();

        /// <summary>
        /// True if the flow strategy should be exchangable by users of this container, false otherwise. Default is true.
        /// </summary>
        protected virtual bool CanChangeFlowStrategy => true;

        private IFlowStrategy flowStrategy;
        /// <summary>
        /// The strategy used to calculate the positioning of the children of this <see cref="FlowContainer"/>.
        /// </summary>
        public IFlowStrategy FlowStrategy
        {
            get { return flowStrategy; }
            set
            {
                if (!CanChangeFlowStrategy && flowStrategy != null)
                    throw new NotSupportedException($"This flow container does not allow alterations to its flow strategy. Flow container is of type {GetType().FullName}.");

                if (value == null)
                    throw new InvalidOperationException($"The flow strategy of a flow container may not be null.");

                if (flowStrategy == value)
                    return;

                flowStrategy = value;
                InvalidateLayout();
            }
        }

        Vector2 maximumSize;

        /// <summary>
        /// Optional maximum dimensions for this container. Note that the meaning of this value can change depending on the
        /// <see cref="FlowStrategy"/> the container operates with.
        /// </summary>
        public Vector2 MaximumSize
        {
            get { return maximumSize; }
            set
            {
                if (maximumSize == value) return;

                maximumSize = value;
                Invalidate(Invalidation.Geometry);
            }
        }

        /// <summary>
        /// Constructs a new flow container with the <see cref="FlowStrategies.Default"/> flow strategy.
        /// </summary>
        public FlowContainer()
        {
            FlowStrategy = FlowStrategies.Default;
        }

        protected override bool RequiresChildrenUpdate => base.RequiresChildrenUpdate || !layout.IsValid;

        public override bool Invalidate(Invalidation invalidation = Invalidation.All, Drawable source = null, bool shallPropagate = true)
        {
            if ((invalidation & Invalidation.SizeInParentSpace) > 0)
                layout.Invalidate();

            return base.Invalidate(invalidation, source, shallPropagate);
        }
        /// <summary>
        /// Invalidates the layout of this flow container.
        /// </summary>
        protected void InvalidateLayout() => layout.Invalidate();

        protected override bool UpdateChildrenLife()
        {
            bool changed = base.UpdateChildrenLife();

            if (changed)
                layout.Invalidate();

            return changed;
        }

        public override void InvalidateFromChild(Invalidation invalidation, IDrawable source)
        {
            if ((invalidation & Invalidation.SizeInParentSpace) > 0)
                layout.Invalidate();

            base.InvalidateFromChild(invalidation, source);
        }

        protected virtual IEnumerable<T> SortedChildren => AliveInternalChildren;

        protected override void UpdateAfterChildren()
        {
            base.UpdateAfterChildren();

            if (!layout.EnsureValid())
            {
                layout.Refresh(delegate
                {
                    OnLayout?.Invoke();

                    if (!Children.Any())
                        return;

                    var layoutChildren = SortedChildren.Where(d => d.IsPresent).ToArray();
                    if (layoutChildren.Any(d => (d.RelativeSizeAxes & AutoSizeAxes) != 0))
                        throw new InvalidOperationException($"Drawables inside a flow container may not have a relative size axis that the flow container is auto sizing for. The flow container is set to autosize in {AutoSizeAxes} axes.");
                    if (layoutChildren.Any(d => d.RelativePositionAxes != Axes.None))
                        throw new InvalidOperationException($"A flow container cannot contain a child with relative positioning.");

                    var positions = FlowStrategy.UpdateLayout(this, layoutChildren.Select(c => c.BoundingBox.Size).ToArray()).ToArray();
                    if (positions.Length != layoutChildren.Length)
                        throw new InvalidOperationException($"The flow strategy {FlowStrategy} returned a total of {positions.Length} positions for {layoutChildren.Length} children. Flow strategies must return 1 position per child.");

                    for (var i = 0; i < layoutChildren.Length; ++i)
                    {
                        var finalPos = positions[i];
                        if (layoutChildren[i].Position != finalPos)
                            layoutChildren[i].MoveTo(finalPos, LayoutDuration, LayoutEasing);
                    }
                });
            }
        }
    }
}
