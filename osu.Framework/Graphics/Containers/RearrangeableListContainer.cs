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
    /// <typeparam name="T">The type of rearrangeable item</typeparam>
    public abstract class RearrangeableListContainer<T> : CompositeDrawable
        where T : IEquatable<T>
    {
        /// <summary>
        /// The spacing between individual elements.
        /// </summary>
        public Vector2 Spacing
        {
            get => ListContainer.Spacing;
            set => ListContainer.Spacing = value;
        }

        /// <summary>
        /// The items contained by this <see cref="RearrangeableListContainer{T}"/> in their arranged order.
        /// </summary>
        public IEnumerable<T> ArrangedItems => ListContainer.FlowingChildren.Cast<DrawableRearrangeableListItem>().Select(i => i.Model);

        private int maxLayoutPosition;
        protected readonly ListScrollContainer ScrollContainer;
        protected readonly ListFillFlowContainer ListContainer;

        /// <summary>
        /// Creates a new <see cref="RearrangeableListContainer{T}"/>.
        /// </summary>
        protected RearrangeableListContainer()
        {
            RelativeSizeAxes = Axes.Both;

            InternalChild = ScrollContainer = CreateListScrollContainer(ListContainer = CreateListFillFlowContainer().With(d => d.Rearranged += OnRearrange));
        }

        /// <summary>
        /// Adds an item to the end of this container.
        /// </summary>
        public void AddItem(T item)
        {
            var drawable = CreateDrawable(item);

            ListContainer.Add(drawable);
            ListContainer.SetLayoutPosition(drawable, maxLayoutPosition++);
        }

        /// <summary>
        /// Removes an item from this container.
        /// </summary>
        public bool RemoveItem(T item)
        {
            var drawable = ListContainer.FirstOrDefault(d => d.Model.Equals(item));
            if (drawable == null)
                return false;

            return ListContainer.Remove(drawable);
        }

        /// <summary>
        /// Removes all items from this container.
        /// </summary>
        public void Clear()
        {
            ListContainer.Clear();

            // Explicitly reset scroll position here so that ScrollContainer doesn't retain our
            // scroll position if we quickly add new items after calling a Clear().
            ScrollContainer.ScrollToStart();
        }

        /// <summary>
        /// Invoked after an arrangement has occurred.
        /// </summary>
        protected virtual void OnRearrange()
        {
        }

        /// <summary>
        /// Creates the <see cref="ListFillFlowContainer"/> for the items.
        /// </summary>
        protected virtual ListFillFlowContainer CreateListFillFlowContainer() => new ListFillFlowContainer
        {
            RelativeSizeAxes = Axes.X,
            AutoSizeAxes = Axes.Y,
            LayoutDuration = 160,
            LayoutEasing = Easing.OutQuint,
            Direction = FillDirection.Vertical,
            Spacing = new Vector2(1),
        };

        /// <summary>
        /// Creates the <see cref="ListScrollContainer"/> for the list of items.
        /// </summary>
        protected abstract ListScrollContainer CreateListScrollContainer(ListFillFlowContainer flowContainer);

        /// <summary>
        /// Creates the <see cref="Drawable"/> representation of an item.
        /// </summary>
        /// <param name="item">The item to create the <see cref="Drawable"/> representation of.</param>
        /// <returns>The <see cref="DrawableRearrangeableListItem"/>.</returns>
        protected abstract DrawableRearrangeableListItem CreateDrawable(T item);

        #region ListScrollContainer

        protected abstract class ListScrollContainer : ScrollContainer<ListFillFlowContainer>
        {
            private const double exp_base = 1.05;

            /// <summary>
            /// The distance from the top and bottom of this <see cref="ListScrollContainer"/> at which automatic scroll begins.
            /// </summary>
            protected double AutomaticTriggerDistance = 10;

            /// <summary>
            /// The maximum exponent of the automatic scroll speed at the boundaries of this <see cref="ListScrollContainer"/>.
            /// </summary>
            protected double MaxExponent = 50;

            private bool autoScrolling;
            private double scrollSpeed;

            protected ListScrollContainer(ListFillFlowContainer flowContainer)
            {
                RelativeSizeAxes = Axes.Both;

                Child = flowContainer.With(d =>
                {
                    d.DragStart += _ => autoScrolling = true;
                    d.Drag += updateDragPosition;
                    d.DragEnd += _ =>
                    {
                        autoScrolling = false;
                        scrollSpeed = 0;
                    };
                });
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

                if (localPos.Y < AutomaticTriggerDistance)
                {
                    var power = Math.Min(MaxExponent, Math.Abs(AutomaticTriggerDistance - localPos.Y));
                    scrollSpeed = -(float)Math.Pow(exp_base, power);
                }
                else if (localPos.Y > DrawHeight - AutomaticTriggerDistance)
                {
                    var power = Math.Min(MaxExponent, Math.Abs(DrawHeight - AutomaticTriggerDistance - localPos.Y));
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
            /// Invoked after a rearrangement has occurred via dragging.
            /// </summary>
            internal event Action Rearranged;

            /// <summary>
            /// Invoked when a drag start occurs.
            /// </summary>
            internal event Action<DragStartEvent> DragStart;

            /// <summary>
            /// Invoked when a drag occurs.
            /// </summary>
            internal event Action<DragEvent> Drag;

            /// <summary>
            /// Invoked when a drag end occurs.
            /// </summary>
            internal event Action<DragEndEvent> DragEnd;

            private DrawableRearrangeableListItem currentlyDraggedItem;
            private Vector2 nativeDragPosition;
            private readonly List<Drawable> cachedFlowingChildren = new List<Drawable>();

            protected override bool OnDragStart(DragStartEvent e)
            {
                nativeDragPosition = e.ScreenSpaceMousePosition;
                currentlyDraggedItem = this.FirstOrDefault(d => d.IsBeingDragged);

                cachedFlowingChildren.AddRange(FlowingChildren);

                if (currentlyDraggedItem != null)
                    DragStart?.Invoke(e);

                return currentlyDraggedItem != null || base.OnDragStart(e);
            }

            protected override void OnDrag(DragEvent e)
            {
                base.OnDrag(e);

                nativeDragPosition = e.ScreenSpaceMousePosition;

                if (currentlyDraggedItem != null)
                    Drag?.Invoke(e);
            }

            protected override void OnDragEnd(DragEndEvent e)
            {
                base.OnDragEnd(e);

                nativeDragPosition = e.ScreenSpaceMousePosition;

                cachedFlowingChildren.Clear();

                if (currentlyDraggedItem != null)
                    DragEnd?.Invoke(e);

                currentlyDraggedItem = null;
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
            /// Whether the item is currently being dragged.
            /// </summary>
            internal bool IsBeingDragged { get; private set; }

            /// <summary>
            /// The item this <see cref="DrawableRearrangeableListItem"/> represents.
            /// </summary>
            public T Model;

            /// <summary>
            /// Creates a new <see cref="DrawableRearrangeableListItem"/>.
            /// </summary>
            /// <param name="item">The item to represent.</param>
            protected DrawableRearrangeableListItem(T item)
            {
                Model = item;
            }

            /// <summary>
            /// Returns whether the item is currently able to be dragged.
            /// </summary>
            protected virtual bool IsDraggableAt(Vector2 screenSpacePos) => true;

            protected override bool OnMouseDown(MouseDownEvent e)
            {
                base.OnMouseDown(e);

                if (IsDraggableAt(e.ScreenSpaceMousePosition))
                    IsBeingDragged = true;

                // Don't block drag to allow parenting containers to handle it
                return false;
            }

            protected override void OnMouseUp(MouseUpEvent e)
            {
                base.OnMouseUp(e);
                IsBeingDragged = false;
            }
        }

        #endregion
    }
}
