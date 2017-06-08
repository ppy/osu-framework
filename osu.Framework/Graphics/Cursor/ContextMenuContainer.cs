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
        /// Creates a context menu container where the context menu is positioned at the bottom-right of
        /// the <see cref="CursorContainer.ActiveCursor"/> of the given <see cref="CursorContainer"/>.
        /// </summary>
        /// <param name="cursorContainer">The <see cref="CursorContainer"/> of which the <see cref="CursorContainer.ActiveCursor"/>
        /// shall be used for positioning. If null is provided, then a small offset from the current mouse position is used.</param>
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
                    break;
                default:
                    menu.Close();
                    break;
            }
            return true;
        }

        protected override void Update()
        {
            if (menu.State == MenuState.Opened && menuTarget != null)
                menu.Position = new Vector2(-ToSpaceOfOtherDrawable(-relativeCursorPosition, menuTarget).X, -ToSpaceOfOtherDrawable(-relativeCursorPosition, menuTarget).Y);
            base.Update();
        }
    }
}
