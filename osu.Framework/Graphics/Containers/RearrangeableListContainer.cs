// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
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
        private const double exp_base = 1.05;

        /// <summary>
        /// The distance from the top and bottom of this <see cref="RearrangeableListContainer{T}"/> at which automatic scroll begins.
        /// </summary>
        protected double AutomaticTriggerDistance = 10;

        /// <summary>
        /// The maximum exponent of the automatic scroll speed at the boundaries of this <see cref="RearrangeableListContainer{T}"/>.
        /// </summary>
        protected double MaxExponent = 50;

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

        protected readonly ScrollContainer<Drawable> ScrollContainer;
        protected readonly FillFlowContainer<DrawableRearrangeableListItem> ListContainer;

        private int maxLayoutPosition;
        private DrawableRearrangeableListItem currentlyDraggedItem;
        private Vector2 screenSpaceDragPosition;

        /// <summary>
        /// Creates a new <see cref="RearrangeableListContainer{T}"/>.
        /// </summary>
        protected RearrangeableListContainer()
        {
            ListContainer = CreateListFillFlowContainer().With(d =>
            {
                d.RelativeSizeAxes = Axes.X;
                d.AutoSizeAxes = Axes.Y;
                d.Direction = FillDirection.Vertical;
            });

            InternalChild = ScrollContainer = CreateScrollContainer().With(d =>
            {
                d.RelativeSizeAxes = Axes.Both;
                d.Child = ListContainer;
            });
        }

        /// <summary>
        /// Adds an item to the end of this container.
        /// </summary>
        public void AddItem(T item)
        {
            var drawable = CreateDrawable(item).With(d =>
            {
                d.StartArrangement += startArrangement;
                d.Arrange += arrange;
                d.EndArrangement += endArrangement;
            });

            ListContainer.Add(drawable);
            ListContainer.SetLayoutPosition(drawable, maxLayoutPosition++);
        }

        private void startArrangement(DrawableRearrangeableListItem item, DragStartEvent e)
        {
            currentlyDraggedItem = item;
            screenSpaceDragPosition = e.ScreenSpaceMousePosition;
        }

        private void arrange(DrawableRearrangeableListItem item, DragEvent e) => screenSpaceDragPosition = e.ScreenSpaceMousePosition;

        private void endArrangement(DrawableRearrangeableListItem item, DragEndEvent e) => currentlyDraggedItem = null;

        /// <summary>
        /// Removes an item from this container.
        /// </summary>
        public bool RemoveItem(T item)
        {
            var drawable = ListContainer.FirstOrDefault(d => d.Model.Equals(item));
            if (drawable == null)
                return false;

            if (drawable == currentlyDraggedItem)
                currentlyDraggedItem = null;

            return ListContainer.Remove(drawable);
        }

        /// <summary>
        /// Removes all items from this container.
        /// </summary>
        public void ClearItems()
        {
            ListContainer.Clear();

            // Explicitly reset scroll position here so that ScrollContainer doesn't retain our
            // scroll position if we quickly add new items after calling a Clear().
            ScrollContainer.ScrollToStart();
        }

        protected override void Update()
        {
            base.Update();

            if (currentlyDraggedItem != null)
                updateScrollPosition();
        }

        protected override void UpdateAfterChildren()
        {
            base.UpdateAfterChildren();

            if (currentlyDraggedItem != null)
                updateArrangement();
        }

        private void updateScrollPosition()
        {
            Vector2 localPos = ScrollContainer.ToLocalSpace(screenSpaceDragPosition);
            double scrollSpeed = 0;

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

            if ((scrollSpeed < 0 && ScrollContainer.Current > 0) || (scrollSpeed > 0 && !ScrollContainer.IsScrolledToEnd()))
                ScrollContainer.ScrollBy((float)scrollSpeed);
        }

        private readonly List<Drawable> flowingChildrenCache = new List<Drawable>();

        private void updateArrangement()
        {
            var localPos = ListContainer.ToLocalSpace(screenSpaceDragPosition);
            int srcIndex = (int)ListContainer.GetLayoutPosition(currentlyDraggedItem);

            // Find the last item with position < mouse position. Note we can't directly use
            // the item positions as they are being transformed
            float heightAccumulator = 0;
            int dstIndex = 0;

            for (; dstIndex < ListContainer.Count; dstIndex++)
            {
                // Using BoundingBox here takes care of scale, paddings, etc...
                heightAccumulator += ListContainer[dstIndex].BoundingBox.Height + Spacing.Y;
                if (heightAccumulator - Spacing.Y / 2 > localPos.Y)
                    break;
            }

            dstIndex = MathHelper.Clamp(dstIndex, 0, ListContainer.Count - 1);

            if (srcIndex == dstIndex)
                return;

            if (srcIndex < dstIndex - 1)
                dstIndex--;

            flowingChildrenCache.AddRange(ListContainer.FlowingChildren);
            flowingChildrenCache.Remove(currentlyDraggedItem);
            flowingChildrenCache.Insert(dstIndex, currentlyDraggedItem);

            for (int i = 0; i < flowingChildrenCache.Count; i++)
                ListContainer.SetLayoutPosition(flowingChildrenCache[i], i);

            flowingChildrenCache.Clear();
        }

        /// <summary>
        /// Creates the <see cref="FillFlowContainer{DrawableRearrangeableListItem}"/> for the items.
        /// </summary>
        protected virtual FillFlowContainer<DrawableRearrangeableListItem> CreateListFillFlowContainer() => new FillFlowContainer<DrawableRearrangeableListItem>();

        /// <summary>
        /// Creates the <see cref="ScrollContainer"/> for the list of items.
        /// </summary>
        protected abstract ScrollContainer<Drawable> CreateScrollContainer();

        /// <summary>
        /// Creates the <see cref="Drawable"/> representation of an item.
        /// </summary>
        /// <param name="item">The item to create the <see cref="Drawable"/> representation of.</param>
        /// <returns>The <see cref="DrawableRearrangeableListItem"/>.</returns>
        protected abstract DrawableRearrangeableListItem CreateDrawable(T item);

        #region DrawableRearrangeableListItem

        public abstract class DrawableRearrangeableListItem : CompositeDrawable
        {
            internal Action<DrawableRearrangeableListItem, DragStartEvent> StartArrangement;

            internal Action<DrawableRearrangeableListItem, DragEvent> Arrange;

            internal Action<DrawableRearrangeableListItem, DragEndEvent> EndArrangement;

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

            protected override bool OnDragStart(DragStartEvent e)
            {
                if (IsDraggableAt(e.ScreenSpaceMousePosition))
                {
                    StartArrangement?.Invoke(this, e);
                    return true;
                }

                return false;
            }

            protected override void OnDrag(DragEvent e) => Arrange?.Invoke(this, e);

            protected override void OnDragEnd(DragEndEvent e) => EndArrangement?.Invoke(this, e);
        }

        #endregion
    }
}
