// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using osu.Framework.Bindables;
using osu.Framework.Input.Events;
using osuTK;

namespace osu.Framework.Graphics.Containers
{
    /// <summary>
    /// A list container that enables its children to be rearranged via dragging.
    /// </summary>
    /// <typeparam name="TModel">The type of rearrangeable item.</typeparam>
    public abstract class RearrangeableListContainer<TModel> : CompositeDrawable
    {
        private const double exp_base = 1.05;

        /// <summary>
        /// The items contained by this <see cref="RearrangeableListContainer{TModel}"/>, in the order they are arranged.
        /// </summary>
        public readonly BindableList<TModel> Items = new BindableList<TModel>();

        /// <summary>
        /// The spacing between individual elements.
        /// </summary>
        public Vector2 Spacing
        {
            get => ListContainer.Spacing;
            set => ListContainer.Spacing = value;
        }

        /// <summary>
        /// The distance from the top and bottom of this <see cref="RearrangeableListContainer{T}"/> at which automatic scroll begins.
        /// </summary>
        protected double AutomaticTriggerDistance = 0;

        /// <summary>
        /// The maximum exponent of the automatic scroll speed at the boundaries of this <see cref="RearrangeableListContainer{T}"/>.
        /// </summary>
        protected double MaxExponent = 50;

        protected readonly ScrollContainer<Drawable> ScrollContainer;
        protected readonly FillFlowContainer<DrawableRearrangeableListItem<TModel>> ListContainer;

        protected IReadOnlyDictionary<TModel, DrawableRearrangeableListItem<TModel>> ItemMap => itemMap;

        private readonly Dictionary<TModel, DrawableRearrangeableListItem<TModel>> itemMap = new Dictionary<TModel, DrawableRearrangeableListItem<TModel>>();
        private DrawableRearrangeableListItem<TModel> currentlyDraggedItem;
        private Vector2 screenSpaceDragPosition;
        private bool isCurrentlyRearranging; // Will be true only for the duration that indices are being moved around

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

            Items.ItemsAdded += addItems;
            Items.ItemsRemoved += removeItems;
        }

        private void addItems(IEnumerable<TModel> items)
        {
            if (isCurrentlyRearranging)
                return;

            foreach (var item in items)
            {
                var drawable = CreateDrawable(item).With(d =>
                {
                    d.StartArrangement += startArrangement;
                    d.Arrange += arrange;
                    d.EndArrangement += endArrangement;
                });

                ListContainer.Add(drawable);
                itemMap[item] = drawable;
            }

            reSort();
        }

        private void removeItems(IEnumerable<TModel> items)
        {
            if (isCurrentlyRearranging)
                return;

            foreach (var item in items)
            {
                if (currentlyDraggedItem != null && EqualityComparer<TModel>.Default.Equals(currentlyDraggedItem.Model, item))
                    currentlyDraggedItem = null;

                ListContainer.Remove(itemMap[item]);
                itemMap.Remove(item);
            }

            reSort();

            if (Items.Count == 0)
            {
                // Explicitly reset scroll position here so that ScrollContainer doesn't retain our
                // scroll position if we quickly add new items after calling a Clear().
                ScrollContainer.ScrollToStart();
            }
        }

        private void reSort()
        {
            for (int i = 0; i < Items.Count; i++)
                ListContainer.SetLayoutPosition(itemMap[Items[i]], i);
        }

        private void startArrangement(DrawableRearrangeableListItem<TModel> item, DragStartEvent e)
        {
            currentlyDraggedItem = item;
            screenSpaceDragPosition = e.ScreenSpaceMousePosition;
        }

        private void arrange(DrawableRearrangeableListItem<TModel> item, DragEvent e) => screenSpaceDragPosition = e.ScreenSpaceMousePosition;

        private void endArrangement(DrawableRearrangeableListItem<TModel> item, DragEndEvent e) => currentlyDraggedItem = null;

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
            else if (localPos.Y > ScrollContainer.DrawHeight - AutomaticTriggerDistance)
            {
                var power = Math.Min(MaxExponent, Math.Abs(ScrollContainer.DrawHeight - AutomaticTriggerDistance - localPos.Y));
                scrollSpeed = (float)Math.Pow(exp_base, power);
            }

            if ((scrollSpeed < 0 && ScrollContainer.Current > 0) || (scrollSpeed > 0 && !ScrollContainer.IsScrolledToEnd()))
                ScrollContainer.ScrollBy((float)scrollSpeed);
        }

        private void updateArrangement()
        {
            var localPos = ListContainer.ToLocalSpace(screenSpaceDragPosition);
            int srcIndex = Items.IndexOf(currentlyDraggedItem.Model);

            // Find the last item with position < mouse position. Note we can't directly use
            // the item positions as they are being transformed
            float heightAccumulator = 0;
            int dstIndex = 0;

            for (; dstIndex < Items.Count; dstIndex++)
            {
                // Using BoundingBox here takes care of scale, paddings, etc...
                float height = itemMap[Items[dstIndex]].BoundingBox.Height;

                // Rearrangement should occur only after the mid-point of items is crossed
                heightAccumulator += height / 2;

                // Check if the midpoint has been crossed (i.e. cursor is located above the midpoint)
                if (heightAccumulator > localPos.Y)
                {
                    if (dstIndex > srcIndex)
                    {
                        // Suppose an item is dragged just slightly below its own midpoint. The rearrangement condition (accumulator > pos) will be satisfied for the next immediate item
                        // but not the currently-dragged item, which will invoke a rearrangement. This is an off-by-one condition.
                        // Rearrangement should not occur until the midpoint of the next item is crossed, and so to fix this the next item's index is discarded.
                        dstIndex--;
                    }

                    break;
                }

                // Add the remainder of the height of the current item
                heightAccumulator += height / 2 + Spacing.Y;
            }

            dstIndex = Math.Clamp(dstIndex, 0, Items.Count - 1);

            if (srcIndex == dstIndex)
                return;

            isCurrentlyRearranging = true;

            Items.RemoveAt(srcIndex);
            Items.Insert(dstIndex, currentlyDraggedItem.Model);

            // Todo: this could be optimised, but it's a very simple iteration over all the items
            reSort();

            isCurrentlyRearranging = false;
        }

        /// <summary>
        /// Creates the <see cref="FillFlowContainer{DrawableRearrangeableListItem}"/> for the items.
        /// </summary>
        protected virtual FillFlowContainer<DrawableRearrangeableListItem<TModel>> CreateListFillFlowContainer() => new FillFlowContainer<DrawableRearrangeableListItem<TModel>>();

        /// <summary>
        /// Creates the <see cref="ScrollContainer"/> for the list of items.
        /// </summary>
        protected abstract ScrollContainer<Drawable> CreateScrollContainer();

        /// <summary>
        /// Creates the <see cref="Drawable"/> representation of an item.
        /// </summary>
        /// <param name="item">The item to create the <see cref="Drawable"/> representation of.</param>
        /// <returns>The <see cref="DrawableRearrangeableListItem{T}"/>.</returns>
        protected abstract DrawableRearrangeableListItem<TModel> CreateDrawable(TModel item);
    }
}
