// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using OpenTK;
using osu.Framework.Caching;
using System;
using System.Collections.Generic;
using System.Linq;

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

        protected virtual IEnumerable<Drawable> FlowingChildren => AliveInternalChildren.Where(d => d.IsPresent);

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
                    d.MoveTo(finalPos, LayoutDuration, LayoutEasing);

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
    }
}
