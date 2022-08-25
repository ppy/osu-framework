// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using osu.Framework.Bindables;
using osu.Framework.Input.Events;
using osuTK;

namespace osu.Framework.Graphics.Containers
{
    /// <summary>
    /// A list container that enables its children to be rearranged via dragging.
    /// </summary>
    /// <remarks>
    /// Adding duplicate items is not currently supported.
    /// </remarks>
    /// <typeparam name="TModel">The type of rearrangeable item.</typeparam>
    public abstract class RearrangeableListContainer<TModel> : CompositeDrawable
    {
        private const float exp_base = 1.05f;

        /// <summary>
        /// The items contained by this <see cref="RearrangeableListContainer{TModel}"/>, in the order they are arranged.
        /// </summary>
        public readonly BindableList<TModel> Items = new BindableList<TModel>();

        /// <summary>
        /// The maximum exponent of the automatic scroll speed at the boundaries of this <see cref="RearrangeableListContainer{TModel}"/>.
        /// </summary>
        protected float MaxExponent = 50;

        /// <summary>
        /// The <see cref="ScrollContainer"/> containing the flow of items.
        /// </summary>
        protected readonly ScrollContainer<Drawable> ScrollContainer;

        /// <summary>
        /// The <see cref="FillFlowContainer"/> containing of all the <see cref="RearrangeableListItem{TModel}"/>s.
        /// </summary>
        protected readonly FillFlowContainer<RearrangeableListItem<TModel>> ListContainer;

        /// <summary>
        /// The mapping of <typeparamref name="TModel"/> to <see cref="RearrangeableListItem{TModel}"/>.
        /// </summary>
        protected IReadOnlyDictionary<TModel, RearrangeableListItem<TModel>> ItemMap => itemMap;

        private readonly Dictionary<TModel, RearrangeableListItem<TModel>> itemMap = new Dictionary<TModel, RearrangeableListItem<TModel>>();
        private RearrangeableListItem<TModel> currentlyDraggedItem;
        private Vector2 screenSpaceDragPosition;

        /// <summary>
        /// Creates a new <see cref="RearrangeableListContainer{TModel}"/>.
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

            Items.CollectionChanged += collectionChanged;
        }

        /// <summary>
        /// Fired whenever new drawable items are added or removed from <see cref="ListContainer"/>.
        /// </summary>
        protected virtual void OnItemsChanged()
        {
        }

        private void collectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    addItems(e.NewItems);
                    break;

                case NotifyCollectionChangedAction.Remove:
                    removeItems(e.OldItems);

                    // Explicitly reset scroll position here so that ScrollContainer doesn't retain our
                    // scroll position if we quickly add new items after calling a Clear().
                    if (Items.Count == 0)
                        ScrollContainer.ScrollToStart();
                    break;

                case NotifyCollectionChangedAction.Reset:
                    currentlyDraggedItem = null;
                    ListContainer.Clear();
                    itemMap.Clear();
                    OnItemsChanged();
                    break;

                case NotifyCollectionChangedAction.Replace:
                    removeItems(e.OldItems);
                    addItems(e.NewItems);
                    break;
            }
        }

        private void removeItems(IList items)
        {
            foreach (var item in items.Cast<TModel>())
            {
                if (currentlyDraggedItem != null && EqualityComparer<TModel>.Default.Equals(currentlyDraggedItem.Model, item))
                    currentlyDraggedItem = null;

                var drawableItem = itemMap[item];

                ListContainer.Remove(drawableItem);
                DisposeChildAsync(drawableItem);

                itemMap.Remove(item);
            }

            sortItems();
            OnItemsChanged();
        }

        private void addItems(IList items)
        {
            var drawablesToAdd = new List<Drawable>();

            foreach (var item in items.Cast<TModel>())
            {
                if (itemMap.ContainsKey(item))
                {
                    throw new InvalidOperationException(
                        $"Duplicate items cannot be added to a {nameof(BindableList<TModel>)} that is currently bound with a {nameof(RearrangeableListContainer<TModel>)}.");
                }

                var drawable = CreateDrawable(item).With(d =>
                {
                    d.StartArrangement += startArrangement;
                    d.Arrange += arrange;
                    d.EndArrangement += endArrangement;
                });

                drawablesToAdd.Add(drawable);
                itemMap[item] = drawable;
            }

            if (!IsLoaded)
                addToHierarchy(drawablesToAdd);
            else
                LoadComponentsAsync(drawablesToAdd, addToHierarchy);

            void addToHierarchy(IEnumerable<Drawable> drawables)
            {
                foreach (var d in drawables.Cast<RearrangeableListItem<TModel>>())
                {
                    // Don't add drawables whose models were removed during the async load, or drawables that are no longer attached to the contained model.
                    if (itemMap.TryGetValue(d.Model, out var modelDrawable) && modelDrawable == d)
                        ListContainer.Add(d);
                }

                sortItems();
                OnItemsChanged();
            }
        }

        private void sortItems()
        {
            for (int i = 0; i < Items.Count; i++)
            {
                var drawable = itemMap[Items[i]];

                // If the async load didn't complete, the item wouldn't exist in the container and an exception would be thrown
                if (drawable.Parent == ListContainer)
                    ListContainer.SetLayoutPosition(drawable, i);
            }
        }

        private void startArrangement(RearrangeableListItem<TModel> item, DragStartEvent e)
        {
            currentlyDraggedItem = item;
            screenSpaceDragPosition = e.ScreenSpaceMousePosition;
        }

        private void arrange(RearrangeableListItem<TModel> item, DragEvent e) => screenSpaceDragPosition = e.ScreenSpaceMousePosition;

        private void endArrangement(RearrangeableListItem<TModel> item, DragEndEvent e) => currentlyDraggedItem = null;

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
            float scrollSpeed = 0;

            if (localPos.Y < 0)
            {
                float power = Math.Min(MaxExponent, Math.Abs(localPos.Y));
                scrollSpeed = (float)(-MathF.Pow(exp_base, power) * Clock.ElapsedFrameTime * 0.1);
            }
            else if (localPos.Y > ScrollContainer.DrawHeight)
            {
                float power = Math.Min(MaxExponent, Math.Abs(ScrollContainer.DrawHeight - localPos.Y));
                scrollSpeed = (float)(MathF.Pow(exp_base, power) * Clock.ElapsedFrameTime * 0.1);
            }

            if ((scrollSpeed < 0 && ScrollContainer.Current > 0) || (scrollSpeed > 0 && !ScrollContainer.IsScrolledToEnd()))
                ScrollContainer.ScrollBy(scrollSpeed);
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
                var drawable = itemMap[Items[dstIndex]];

                if (!drawable.IsLoaded || !drawable.IsPresent)
                    continue;

                // Using BoundingBox here takes care of scale, paddings, etc...
                float height = drawable.BoundingBox.Height;

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
                heightAccumulator += height / 2 + ListContainer.Spacing.Y;
            }

            dstIndex = Math.Clamp(dstIndex, 0, Items.Count - 1);

            if (srcIndex == dstIndex)
                return;

            Items.Move(srcIndex, dstIndex);

            // Todo: this could be optimised, but it's a very simple iteration over all the items
            sortItems();
        }

        /// <summary>
        /// Creates the <see cref="FillFlowContainer{DrawableRearrangeableListItem}"/> for the items.
        /// </summary>
        protected virtual FillFlowContainer<RearrangeableListItem<TModel>> CreateListFillFlowContainer() => new FillFlowContainer<RearrangeableListItem<TModel>>();

        /// <summary>
        /// Creates the <see cref="ScrollContainer"/> for the list of items.
        /// </summary>
        protected abstract ScrollContainer<Drawable> CreateScrollContainer();

        /// <summary>
        /// Creates the <see cref="Drawable"/> representation of an item.
        /// </summary>
        /// <param name="item">The item to create the <see cref="Drawable"/> representation of.</param>
        /// <returns>The <see cref="RearrangeableListItem{TModel}"/>.</returns>
        protected abstract RearrangeableListItem<TModel> CreateDrawable(TModel item);
    }
}
