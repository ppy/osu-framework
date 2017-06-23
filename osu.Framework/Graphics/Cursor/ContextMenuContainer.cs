// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using OpenTK;
using OpenTK.Input;
using osu.Framework.Allocation;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Input;
using System.Linq;

namespace osu.Framework.Graphics.Cursor
{
    /// <summary>
    /// A container which manages a <see cref="ContextMenu{TItem}"/>.
    /// If a right-click happens on a <see cref="Drawable"/> that implements <see cref="IHasContextMenu"/> and exists as a child of the same <see cref="InputManager"/> as this container,
    /// a <see cref="ContextMenu{TItem}"/> will be displayed with bottom-right origin at the right-clicked position.
    /// </summary>
    public class ContextMenuContainer : Container
    {
        private readonly CursorContainer cursorContainer;
        private readonly ContextMenu<ContextMenuItem> menu;

        private UserInputManager inputManager;
        private IHasContextMenu menuTarget;
        private Vector2 relativeCursorPosition;

        /// <summary>
        /// Creates a new context menu. Can be overridden to supply custom subclass of <see cref="ContextMenu{TItem}"/>.
        /// </summary>
        protected virtual ContextMenu<ContextMenuItem> CreateContextMenu() => new ContextMenu<ContextMenuItem>();

        /// <summary>
        /// Creates a new <see cref="ContextMenuContainer"/>.
        /// </summary>
        /// <param name="cursorContainer">The <see cref="CursorContainer"/> of which the <see cref="CursorContainer.ActiveCursor"/>
        /// shall be used for positioning. The current mouse position is used if null is provided.</param>
        public ContextMenuContainer(CursorContainer cursorContainer = null)
        {
            this.cursorContainer = cursorContainer;
            RelativeSizeAxes = Axes.Both;
            Add(menu = CreateContextMenu());
        }

        [BackgroundDependencyLoader]
        private void load(UserInputManager input)
        {
            inputManager = input;
        }

        protected override bool OnMouseDown(InputState state, MouseDownEventArgs args)
        {
            switch (args.Button)
            {
                case MouseButton.Right:
                    menuTarget = inputManager.HoveredDrawables.OfType<IHasContextMenu>().FirstOrDefault();

                    if (menuTarget == null)
                    {
                        if (menu.State == MenuState.Opened)
                            menu.Close();
                        return false;
                    }

                    menu.Items = menuTarget.ContextMenuItems;

                    menu.Position = ToLocalSpace(cursorContainer?.ActiveCursor.ScreenSpaceDrawQuad.TopLeft ?? inputManager.CurrentState.Mouse.Position);
                    relativeCursorPosition = ToSpaceOfOtherDrawable(menu.Position, menuTarget);
                    menu.Open();
                    return true;
                default:
                    menu.Close();
                    return false;
            }
        }

        protected override void Update()
        {
            if (menu.State == MenuState.Opened && menuTarget != null)
                menu.Position = menuTarget.ToSpaceOfOtherDrawable(relativeCursorPosition, this);
            base.Update();
        }
    }
}
