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
    /// A container that can be used to fluently arrange its children.
    /// </summary>
    public abstract class FlowContainer<T> : Container<T>
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

        protected void InvalidateLayout() => layout.Invalidate();

        Vector2 maximumSize;

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
                Invalidate(Invalidation.Geometry);
            }
        }

        protected override bool RequiresChildrenUpdate => base.RequiresChildrenUpdate || !layout.IsValid;

        public override bool Invalidate(Invalidation invalidation = Invalidation.All, Drawable source = null, bool shallPropagate = true)
        {
            if ((invalidation & Invalidation.SizeInParentSpace) > 0)
                layout.Invalidate();

            return base.Invalidate(invalidation, source, shallPropagate);
        }

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

        protected virtual IEnumerable<T> FlowingChildren => AliveInternalChildren.Where(d => d.IsPresent);

        protected abstract IEnumerable<Vector2> ComputeLayoutPositions();

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

                    var positions = ComputeLayoutPositions().ToArray();

                    int i = 0;
                    foreach (var d in FlowingChildren)
                    {
                        if (i > positions.Length)
                            throw new InvalidOperationException($"{GetType().FullName}.{nameof(ComputeLayoutPositions)} returned a total of {positions.Length} positions for {i} children. {nameof(ComputeLayoutPositions)} must return 1 position per child.");

                        if ((d.RelativeSizeAxes & AutoSizeAxes) != 0)
                            throw new InvalidOperationException(
                                $"Drawables inside a flow container may not have a relative size axis that the flow container is auto sizing for." +
                                $"The flow container is set to autosize in {AutoSizeAxes} axes and the child is set to relative size in {RelativeSizeAxes} axes.");

                        if (d.RelativePositionAxes != Axes.None)
                            throw new InvalidOperationException($"A flow container cannot contain a child with relative positioning (it is {RelativePositionAxes}).");

                        var finalPos = positions[i];
                        if (d.Position != finalPos)
                            d.MoveTo(finalPos, LayoutDuration, LayoutEasing);

                        ++i;
                    }

                    if (i != positions.Length)
                        throw new InvalidOperationException($"{GetType().FullName}.{nameof(ComputeLayoutPositions)} returned a total of {positions.Length} positions for {i} children. {nameof(ComputeLayoutPositions)} must return 1 position per child.");
                });
            }
        }
    }
}
