// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Framework.Input.Events;
using osuTK;
using osuTK.Input;

namespace osu.Framework.Graphics.Containers
{
    /// <summary>
    /// A list container that enables its children to be rearranged via dragging.
    /// </summary>
    public class RearrangeableListContainer<T> : CompositeDrawable where T : Drawable, IRearrangeableDrawable<T>
    {
        /// <summary>
        /// The spacing between individual elements. Default is <see cref="Vector2.Zero"/>.
        /// </summary>
        public Vector2 Spacing
        {
            get => ListContainer.Spacing;
            set => ListContainer.Spacing = value;
        }

        /// <summary>
        /// This event is fired after a rearrangement has occurred via dragging.
        /// </summary>
        public event Action ItemsRearranged;

        /// <summary>
        /// The list of children as they are currently arranged.
        /// </summary>
        public IEnumerable<T> ArrangedItems => ListContainer.FlowingChildren.Cast<T>();

        private int maxLayoutPosition;
        protected readonly ListScrollContainer ScrollContainer;
        protected readonly ListFillFlowContainer ListContainer;

        /// <summary>
        /// Creates a rearrangeable list container.
        /// </summary>
        public RearrangeableListContainer()
        {
            RelativeSizeAxes = Axes.Both;
            InternalChild = ScrollContainer = CreateListScrollContainer(ListContainer = CreateListFillFlowContainer());
            ListContainer.ItemsRearranged += OnRearrange;
        }

        /// <summary>
        /// Adds a child to the end of this list.
        /// </summary>
        public void AddItem(T item)
        {
            item.RequestRemoval += RemoveItem;
            ListContainer.Add(item);
            ListContainer.SetLayoutPosition(item, maxLayoutPosition++);
        }

        /// <summary>
        /// Removes a child from this container.
        /// </summary>
        public void RemoveItem(T item) => ListContainer.Remove(item);

        /// <summary>
        /// Removes all <see cref="Container{T}.Children"/> from this container.
        /// </summary>
        public void Clear()
        {
            ListContainer.Clear();
            // Explicitly reset scroll position here so that ScrollContainer doesn't retain our
            // scroll position if we quickly add new items after calling a Clear().
            ScrollContainer.ScrollToStart();
        }

        protected virtual void OnRearrange() => ItemsRearranged?.Invoke();

        /// <summary>
        /// Allows subclasses to customise the <see cref="ListFillFlowContainer"/>.
        /// </summary>
        protected virtual ListFillFlowContainer CreateListFillFlowContainer() =>
            new ListFillFlowContainer
            {
                RelativeSizeAxes = Axes.X,
                AutoSizeAxes = Axes.Y,
                LayoutDuration = 160,
                LayoutEasing = Easing.OutQuint,
                Direction = FillDirection.Vertical,
                Spacing = new Vector2(1),
            };

        /// <summary>
        /// Allows subclasses to customise the <see cref="ListScrollContainer"/>.
        /// </summary>
        protected virtual ListScrollContainer CreateListScrollContainer(ListFillFlowContainer flowContainer) =>
            new ListScrollContainer
            {
                Child = flowContainer,
            };

        protected class ListScrollContainer : ScrollContainer<ListFillFlowContainer>
        {
            public ListScrollContainer()
            {
                RelativeSizeAxes = Axes.Both;
                ScrollbarOverlapsContent = false;
                Padding = new MarginPadding(5);
            }

            protected override void Update()
            {
                base.Update();
                updateScrollPosition();
            }

            private void updateScrollPosition()
            {
                const float scroll_trigger_distance = 10;
                const double max_power = 50;
                const double exp_base = 1.05;

                var mouse = GetContainingInputManager().CurrentState.Mouse;

                if (!mouse.IsPressed(MouseButton.Left) || !Child.IsDragging)
                    return;

                var localPos = ToLocalSpace(mouse.Position);

                if (localPos.Y < scroll_trigger_distance)
                {
                    if (Current <= 0)
                        return;

                    var power = Math.Min(max_power, Math.Abs(scroll_trigger_distance - localPos.Y));
                    ScrollBy(-(float)Math.Pow(exp_base, power));
                }
                else if (localPos.Y > DrawHeight - scroll_trigger_distance)
                {
                    if (IsScrolledToEnd())
                        return;

                    var power = Math.Min(max_power, Math.Abs(DrawHeight - scroll_trigger_distance - localPos.Y));
                    ScrollBy((float)Math.Pow(exp_base, power));
                }
            }
        }

        protected class ListFillFlowContainer : FillFlowContainer<T>
        {
            public event Action ItemsRearranged;

            public bool IsDragging => currentlyDraggedItem != null;

            private T currentlyDraggedItem;
            private Vector2 nativeDragPosition;
            private List<Drawable> cachedFlowingChildren;

            protected override bool OnDragStart(DragStartEvent e)
            {
                nativeDragPosition = e.ScreenSpaceMousePosition;
                currentlyDraggedItem = this.FirstOrDefault(d => d.IsDraggable);
                cachedFlowingChildren = new List<Drawable>(FlowingChildren);
                return currentlyDraggedItem != null || base.OnDragStart(e);
            }

            protected override bool OnDrag(DragEvent e)
            {
                nativeDragPosition = e.ScreenSpaceMousePosition;
                return currentlyDraggedItem != null || base.OnDrag(e);
            }

            protected override bool OnDragEnd(DragEndEvent e)
            {
                nativeDragPosition = e.ScreenSpaceMousePosition;
                var handled = currentlyDraggedItem != null || base.OnDragEnd(e);
                currentlyDraggedItem = null;
                cachedFlowingChildren = new List<Drawable>();
                return handled;
            }

            protected override void Update()
            {
                base.Update();

                if (currentlyDraggedItem != null)
                    updateDragPosition();
            }

            private void updateDragPosition()
            {
                var itemsPos = ToLocalSpace(nativeDragPosition);
                int srcIndex = (int)GetLayoutPosition(currentlyDraggedItem);

                // Find the last item with position < mouse position. Note we can't directly use
                // the item positions as they are being transformed
                float heightAccumulator = 0;
                int dstIndex = 0;
                for (; dstIndex < Count; dstIndex++)
                {
                    // Using BoundingBox here takes care of scale, paddings, etc...
                    heightAccumulator += this[dstIndex].BoundingBox.Height + Spacing.Y;
                    if (heightAccumulator - Spacing.Y / 2 > itemsPos.Y)
                        break;
                }

                dstIndex = MathHelper.Clamp(dstIndex, 0, Count - 1);

                if (srcIndex == dstIndex)
                    return;

                cachedFlowingChildren.Remove(currentlyDraggedItem);

                if (srcIndex < dstIndex - 1)
                    dstIndex--;

                cachedFlowingChildren.Insert(dstIndex, currentlyDraggedItem);

                for (int i = 0; i < cachedFlowingChildren.Count; i++)
                    SetLayoutPosition(cachedFlowingChildren[i], i);

                ItemsRearranged?.Invoke();
            }
        }
    }
}
