// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Input.Events;
using osuTK;

namespace osu.Framework.Graphics.Containers
{
    public abstract class DrawableRearrangeableListItem<TModel> : CompositeDrawable
        where TModel : IEquatable<TModel>
    {
        internal Action<DrawableRearrangeableListItem<TModel>, DragStartEvent> StartArrangement;

        internal Action<DrawableRearrangeableListItem<TModel>, DragEvent> Arrange;

        internal Action<DrawableRearrangeableListItem<TModel>, DragEndEvent> EndArrangement;

        /// <summary>
        /// The item this <see cref="DrawableRearrangeableListItem{TModel}"/> represents.
        /// </summary>
        public TModel Model;

        /// <summary>
        /// Creates a new <see cref="DrawableRearrangeableListItem{TModel}"/>.
        /// </summary>
        /// <param name="item">The item to represent.</param>
        protected DrawableRearrangeableListItem(TModel item)
        {
            Model = item;
        }

        /// <summary>
        /// Returns whether the item is currently able to be dragged.
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
