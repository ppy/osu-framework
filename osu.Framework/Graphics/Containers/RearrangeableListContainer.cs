// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Framework.Input;
using osu.Framework.Input.Events;
using osuTK;

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
        public event Action Rearranged;

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
            ListContainer.Rearranged += OnRearrange;
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

        protected virtual void OnRearrange() => Rearranged?.Invoke();

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
        protected virtual ListScrollContainer CreateListScrollContainer(ListFillFlowContainer flowContainer) => new ListScrollContainer(flowContainer);

        protected abstract DrawableRearrangeableListItem CreateDrawable(T item);

        #region ListScrollContainer

        protected class ListScrollContainer : ScrollContainer<ListFillFlowContainer>
        {
            private const float scroll_trigger_distance = 10;
            private const double max_power = 50;
            private const double exp_base = 1.05;
            private bool autoScrolling;
            private double scrollSpeed;

            public ListScrollContainer(ListFillFlowContainer flowContainer)
            {
                RelativeSizeAxes = Axes.Both;
                ScrollbarOverlapsContent = false;
                Padding = new MarginPadding(5);

                Child = flowContainer;

                flowContainer.DragStart += _ => autoScrolling = true;
                flowContainer.Drag += updateDragPosition;
                flowContainer.DragEnd += _ =>
                {
                    autoScrolling = false;
                    scrollSpeed = 0;
                };
            }

            protected override void Update()
            {
                base.Update();

                if (autoScrolling)
                    updateScrollPosition();
            }

            private void updateDragPosition(DragEvent dragEvent)
            {
                var localPos = ToLocalSpace(dragEvent.ScreenSpaceMousePosition);

                if (localPos.Y < scroll_trigger_distance)
                {
                    var power = Math.Min(max_power, Math.Abs(scroll_trigger_distance - localPos.Y));
                    scrollSpeed = -(float)Math.Pow(exp_base, power);
                }
                else if (localPos.Y > DrawHeight - scroll_trigger_distance)
                {
                    var power = Math.Min(max_power, Math.Abs(DrawHeight - scroll_trigger_distance - localPos.Y));
                    scrollSpeed = (float)Math.Pow(exp_base, power);
                }
                else
                {
                    scrollSpeed = 0;
                }
            }

            private void updateScrollPosition()
            {
                if ((scrollSpeed < 0 && Current > 0) || (scrollSpeed > 0 && !IsScrolledToEnd()))
                    ScrollBy((float)scrollSpeed);
            }
        }

        #endregion

        #region ListFillFlowContainer

        protected class ListFillFlowContainer : FillFlowContainer<DrawableRearrangeableListItem>, IRequireHighFrequencyMousePosition
        {
            /// <summary>
            /// This event is fired after a rearrangement has occurred via dragging.
            /// </summary>
            public event Action Rearranged;

            /// <summary>
            /// This event is fired when a drag start occurs.
            /// </summary>
            public event Action<DragStartEvent> DragStart;

            /// <summary>
            /// This event is fired when a drag occurs.
            /// </summary>
            public event Action<DragEvent> Drag;

            /// <summary>
            /// This event is fired when a drag end occurs.
            /// </summary>
            public event Action<DragEndEvent> DragEnd;

            private DrawableRearrangeableListItem currentlyDraggedItem;
            private Vector2 nativeDragPosition;
            private readonly List<Drawable> cachedFlowingChildren = new List<Drawable>();

            protected void OnRearrange() => Rearranged?.Invoke();

            protected override bool OnDragStart(DragStartEvent e)
            {
                nativeDragPosition = e.ScreenSpaceMousePosition;
                currentlyDraggedItem = this.FirstOrDefault(d => d.IsBeingDragged);

                cachedFlowingChildren.Clear();
                cachedFlowingChildren.AddRange(FlowingChildren);

                if (currentlyDraggedItem != null)
                    DragStart?.Invoke(e);

                return currentlyDraggedItem != null || base.OnDragStart(e);
            }

            protected override bool OnDrag(DragEvent e)
            {
                nativeDragPosition = e.ScreenSpaceMousePosition;

                if (currentlyDraggedItem != null)
                    Drag?.Invoke(e);

                return currentlyDraggedItem != null || base.OnDrag(e);
            }

            protected override bool OnDragEnd(DragEndEvent e)
            {
                nativeDragPosition = e.ScreenSpaceMousePosition;

                if (currentlyDraggedItem != null)
                    DragEnd?.Invoke(e);

                var handled = currentlyDraggedItem != null || base.OnDragEnd(e);
                currentlyDraggedItem = null;

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

                Rearranged?.Invoke();
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
            public bool IsBeingDragged { get; private set; }

            /// <summary>
            /// Returns whether the item is currently able to be dragged.
            /// </summary>
            protected virtual bool IsDraggableAt(Vector2 screenSpacePos) => true;

            protected void OnRequestRemoval() => RemovalRequested?.Invoke(this);

            /// <summary>
            /// The RearrangeableListItem backing for this Drawable.
            /// </summary>
            public T Model;

            protected override bool OnMouseDown(MouseDownEvent e)
            {
                if (IsDraggableAt(e.ScreenSpaceMousePosition))
                    IsBeingDragged = true;

                return base.OnMouseDown(e);
            }

            protected override bool OnMouseUp(MouseUpEvent e)
            {
                IsBeingDragged = false;
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
