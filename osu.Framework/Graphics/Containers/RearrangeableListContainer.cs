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
    public abstract class RearrangeableListContainer<T> : CompositeDrawable where T : RearrangeableListItem
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
        public IEnumerable<T> ArrangedItems => ListContainer.FlowingChildren.Cast<DrawableRearrangeableListItem>().Select(i => i.Model);

        private int maxLayoutPosition;
        protected readonly ListScrollContainer ScrollContainer;
        protected readonly ListFillFlowContainer ListContainer;

        /// <summary>
        /// Creates a rearrangeable list container.
        /// </summary>
        protected RearrangeableListContainer()
        {
            RelativeSizeAxes = Axes.Both;
            InternalChild = ScrollContainer = CreateListScrollContainer(ListContainer = CreateListFillFlowContainer());
            ListContainer.ItemsRearranged += OnItemsRearranged;
        }

        /// <summary>
        /// Adds a child to the end of this list.
        /// </summary>
        public void AddItem(T item)
        {
            var drawable = CreateDrawable(item);
            drawable.RemovalRequested += RemoveItem;
            ListContainer.Add(drawable);
            ListContainer.SetLayoutPosition(drawable, maxLayoutPosition++);
        }

        /// <summary>
        /// Removes a child from this container.
        /// </summary>
        public void RemoveItem(DrawableRearrangeableListItem item) => ListContainer.Remove(item);

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

        protected virtual void OnItemsRearranged() => ItemsRearranged?.Invoke();

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

        protected abstract DrawableRearrangeableListItem CreateDrawable(T item);

        #region ListScrollContainer

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

        #endregion

        #region ListFillFlowContainer

        protected class ListFillFlowContainer : FillFlowContainer<DrawableRearrangeableListItem>
        {
            /// <summary>
            /// This event is fired after a rearrangement has occurred via dragging.
            /// </summary>
            public event Action ItemsRearranged;

            /// <summary>
            /// Returns whether an item is currently being dragged.
            /// </summary>
            public bool IsDragging => currentlyDraggedItem != null;

            private DrawableRearrangeableListItem currentlyDraggedItem;
            private Vector2 nativeDragPosition;
            private readonly List<Drawable> cachedFlowingChildren = new List<Drawable>();

            protected void OnItemsRearranged() => ItemsRearranged?.Invoke();

            protected override bool OnDragStart(DragStartEvent e)
            {
                nativeDragPosition = e.ScreenSpaceMousePosition;
                currentlyDraggedItem = this.FirstOrDefault(d => d.IsBeingDragged);
                cachedFlowingChildren.Clear();
                cachedFlowingChildren.AddRange(FlowingChildren);
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
                cachedFlowingChildren.Clear();
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

        #endregion

        #region DrawableRearrangeableListItem

        public abstract class DrawableRearrangeableListItem : CompositeDrawable
        {
            /// <summary>
            /// This event is fired when a removal is requested. e.g. on item removal.
            /// </summary>
            public event Action<DrawableRearrangeableListItem> RemovalRequested;

            /// <summary>
            /// Returns whether the item is currently being dragged.
            /// </summary>
            public bool IsBeingDragged => isBeingDragged;

            /// <summary>
            /// Returns whether the item is currently able to be dragged.
            /// </summary>
            protected virtual bool IsDraggableAt(Vector2 screenSpacePos) => true;

            protected void OnRequestRemoval() => RemovalRequested?.Invoke(this);

            /// <summary>
            /// The RearrangeableListItem backing for this Drawable.
            /// </summary>
            public T Model;

            private bool isBeingDragged;

            protected override bool OnMouseDown(MouseDownEvent e)
            {
                if (IsDraggableAt(e.ScreenSpaceMousePosition))
                    isBeingDragged = true;

                return base.OnMouseDown(e);
            }

            protected override bool OnMouseUp(MouseUpEvent e)
            {
                isBeingDragged = false;
                return base.OnMouseUp(e);
            }

            protected DrawableRearrangeableListItem(T item)
            {
                Model = item;
            }
        }

        #endregion
    }
}
