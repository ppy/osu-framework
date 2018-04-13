// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System.Linq;
using OpenTK;
using OpenTK.Input;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Input;

namespace osu.Framework.Graphics.Cursor
{
    /// <summary>
    /// A container which manages a <see cref="Menu"/>.
    /// If a right-click happens on a <see cref="Drawable"/> that implements <see cref="IHasContextMenu"/> and exists as a child of the same <see cref="InputManager"/> as this container,
    /// a <see cref="Menu"/> will be displayed with bottom-right origin at the right-clicked position.
    /// </summary>
    public class ContextMenuContainer : CursorEffectContainer<ContextMenuContainer, IHasContextMenu>
    {
        private readonly Menu menu;

        private IHasContextMenu menuTarget;
        private Vector2 relativeCursorPosition;

        /// <summary>
        /// Creates a new context menu. Can be overridden to supply custom subclass of <see cref="Menu"/>.
        /// </summary>
        protected virtual Menu CreateMenu() => new Menu(Direction.Vertical);

        private readonly Container content;
        protected override Container<Drawable> Content => content;

        /// <summary>
        /// Creates a new <see cref="ContextMenuContainer"/>.
        /// </summary>
        public ContextMenuContainer()
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

        protected override bool OnMouseDown(InputState state, MouseDownEventArgs args)
        {
            switch (args.Button)
            {
                case MouseButton.Right:
                    menuTarget = FindTargets().FirstOrDefault();

                    if (menuTarget == null)
                    {
                        if (menu.State == MenuState.Open)
                            menu.Close();
                        return false;
                    }

                    menu.Items = menuTarget.ContextMenuItems;

                    menu.Position = ToLocalSpace(state.Mouse.NativeState.Position);
                    relativeCursorPosition = ToSpaceOfOtherDrawable(menu.Position, menuTarget);
                    menu.Open();
                    return true;
                default:
                    menu.Close();
                    return false;
            }
        }

        protected override void UpdateAfterChildren()
        {
            base.UpdateAfterChildren();
            if (menu.State == MenuState.Open && menuTarget != null)
                menu.Position = menuTarget.ToSpaceOfOtherDrawable(relativeCursorPosition, this);
        }
    }
}
