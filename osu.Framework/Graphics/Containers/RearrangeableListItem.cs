// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using osu.Framework.Input.Events;
using osuTK;

namespace osu.Framework.Graphics.Containers
{
    /// <summary>
    /// An item of a <see cref="RearrangeableListContainer{TModel}"/>.
    /// </summary>
    /// <typeparam name="TModel"></typeparam>
    public abstract class RearrangeableListItem<TModel> : CompositeDrawable
    {
        /// <summary>
        /// Invoked on drag start, if an arrangement should be started.
        /// </summary>
        internal Action<RearrangeableListItem<TModel>, DragStartEvent> StartArrangement;

        /// <summary>
        /// Invoked on drag, if this item is being arranged.
        /// </summary>
        internal Action<RearrangeableListItem<TModel>, DragEvent> Arrange;

        /// <summary>
        /// Invoked on drag end, if this item is being arranged.
        /// </summary>
        internal Action<RearrangeableListItem<TModel>, DragEndEvent> EndArrangement;

        /// <summary>
        /// The item this <see cref="RearrangeableListItem{TModel}"/> represents.
        /// </summary>
        public readonly TModel Model;

        /// <summary>
        /// Creates a new <see cref="RearrangeableListItem{TModel}"/>.
        /// </summary>
        /// <param name="item">The item to represent.</param>
        protected RearrangeableListItem(TModel item)
        {
            Model = item;
        }

        /// <summary>
        /// Whether the item is able to be dragged at the given screen-space position.
        /// </summary>
        protected virtual bool IsDraggableAt(Vector2 screenSpacePos) => true;

        protected override bool OnDragStart(DragStartEvent e)
        {
            if (IsDraggableAt(e.ScreenSpaceMouseDownPosition))
            {
                StartArrangement?.Invoke(this, e);
                return true;
            }

            return false;
        }

        protected override void OnDrag(DragEvent e) => Arrange?.Invoke(this, e);

        protected override void OnDragEnd(DragEndEvent e) => EndArrangement?.Invoke(this, e);
    }
}
