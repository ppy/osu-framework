// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Framework.Bindables;
using osu.Framework.Input.Events;
using osuTK;
using osuTK.Input;

namespace osu.Framework.Graphics.Containers
{
    public class RearrangeableListContainer<T> : CompositeDrawable where T : Drawable, IRearrangeableDrawable<T>
    {
        public readonly BindableList<T> ListItems = new BindableList<T>();

        public Vector2 Spacing
        {
            get => ListContainer.Spacing;
            set => ListContainer.Spacing = value;
        }

        private int maxLayoutPosition;
        private readonly ListScrollContainer scrollContainer;
        protected readonly ListFillFlowContainer ListContainer;

        public RearrangeableListContainer()
        {
            RelativeSizeAxes = Axes.Both;
            InternalChild = scrollContainer = CreateListScrollContainer(ListContainer = CreateListFillFlowContainer());

            ListItems.ItemsAdded += itemsAdded;
            ListItems.ItemsRemoved += itemsRemoved;
        }

        public void AddItem(T item)
        {
            ListItems.Add(item);
        }

        public void RemoveItem(T item)
        {
            ListItems.Remove(item);
        }

        public void Clear()
        {
            ListItems.Clear();
            ListContainer.Clear();
            scrollContainer.ScrollToStart();
        }

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

        protected virtual ListScrollContainer CreateListScrollContainer(ListFillFlowContainer flowContainer) =>
            new ListScrollContainer
            {
                Child = flowContainer,
            };

        private void itemsAdded(IEnumerable<T> items)
        {
            foreach (var item in items)
            {
                item.RequestRemoval += RemoveItem;
                ListContainer.Add(item);
                ListContainer.SetLayoutPosition(item, maxLayoutPosition++);
            }
        }

        private void itemsRemoved(IEnumerable<T> items)
        {
            foreach (var item in items)
                ListContainer.Remove(item);
        }

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
            }
        }
    }
}
