// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.Diagnostics;
using System.Linq;
using osuTK;
using osuTK.Input;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Input;
using osu.Framework.Input.Events;

namespace osu.Framework.Graphics.Cursor
{
    /// <summary>
    /// A container which manages a <see cref="Menu"/>.
    /// If a right-click happens on a <see cref="Drawable"/> that implements <see cref="IHasContextMenu"/> and exists as a child of the same <see cref="InputManager"/> as this container,
    /// a <see cref="Menu"/> will be displayed with bottom-right origin at the right-clicked position.
    /// </summary>
    public abstract class ContextMenuContainer : CursorEffectContainer<ContextMenuContainer, IHasContextMenu>
    {
        private readonly Menu menu;

        private IHasContextMenu menuTarget;
        private Vector2 targetRelativePosition;

        /// <summary>
        /// Creates a new context menu. Can be overridden to supply custom subclass of <see cref="Menu"/>.
        /// </summary>
        protected abstract Menu CreateMenu();

        private readonly Container content;

        protected override Container<Drawable> Content => content;

        /// <summary>
        /// Creates a new <see cref="ContextMenuContainer"/>.
        /// </summary>
        protected ContextMenuContainer()
        {
            AddInternal(content = new Container
            {
                RelativeSizeAxes = Axes.Both,
            });

            AddInternal(menu = CreateMenu());
        }

        protected override void OnSizingChanged()
        {
            base.OnSizingChanged();

            if (content != null)
            {
                // reset to none to prevent exceptions
                content.RelativeSizeAxes = Axes.None;
                content.AutoSizeAxes = Axes.None;

                // in addition to using this.RelativeSizeAxes, sets RelativeSizeAxes on every axis that is neither relative size nor auto size
                content.RelativeSizeAxes = Axes.Both & ~AutoSizeAxes;
                content.AutoSizeAxes = AutoSizeAxes;
            }
        }

        protected override bool OnMouseDown(MouseDownEvent e)
        {
            switch (e.Button)
            {
                case MouseButton.Right:
                    var (target, items) = FindTargets()
                                          .Select(t => (target: t, items: t.ContextMenuItems))
                                          .FirstOrDefault(result => result.items != null);

                    menuTarget = target;

                    if (menuTarget == null || items.Length == 0)
                    {
                        if (menu.State == MenuState.Open)
                            menu.Close();
                        return false;
                    }

                    menu.Items = items;

                    targetRelativePosition = menuTarget.ToLocalSpace(e.ScreenSpaceMousePosition);

                    menu.Open();
                    return true;

                default:
                    cancelDisplay();
                    return false;
            }
        }

        private void cancelDisplay()
        {
            Debug.Assert(menu != null);

            menu.Close();
            menuTarget = null;
        }

        protected override void UpdateAfterChildren()
        {
            base.UpdateAfterChildren();

            if (menu.State != MenuState.Open || menuTarget == null) return;

            if ((menuTarget as Drawable)?.FindClosestParent<ContextMenuContainer>() != this || !menuTarget.IsPresent)
            {
                cancelDisplay();
                return;
            }

            Vector2 pos = menuTarget.ToSpaceOfOtherDrawable(targetRelativePosition, this);

            Vector2 overflow = pos + menu.DrawSize - DrawSize;

            if (overflow.X > 0)
                pos.X -= Math.Clamp(overflow.X, 0, menu.DrawWidth);
            if (overflow.Y > 0)
                pos.Y -= Math.Clamp(overflow.Y, 0, menu.DrawHeight);

            if (pos.X < 0)
                pos.X += Math.Clamp(-pos.X, 0, menu.DrawWidth);
            if (pos.Y < 0)
                pos.Y += Math.Clamp(-pos.Y, 0, menu.DrawHeight);

            menu.Position = pos;
        }
    }
}
