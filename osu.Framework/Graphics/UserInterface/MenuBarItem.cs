// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System.Collections.Generic;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Input;

namespace osu.Framework.Graphics.UserInterface
{
    /// <summary>
    /// A singular item of a <see cref="MenuBar"/>.
    /// </summary>
    public class MenuBarItem : CompositeDrawable
    {
        /// <summary>
        /// The <see cref="ContextMenuItem"/>s to display when this <see cref="MenuBarItem"/> is opened.
        /// </summary>
        public IEnumerable<ContextMenuItem> Items { set { ContextMenu.Items = value; } }

        /// <summary>
        /// The content of this <see cref="MenuBarItem"/>. This contains <see cref="TitleText"/> by default.
        /// </summary>
        protected readonly Container Content;

        /// <summary>
        /// The title of this <see cref="MenuBarItem"/>.
        /// </summary>
        protected readonly SpriteText TitleText;

        /// <summary>
        /// The <see cref="ContextMenu{TContextItem}"/> which will be displayed when this <see cref="MenuBarItem"/> is opened.
        /// </summary>
        protected readonly ContextMenu<ContextMenuItem> ContextMenu;

        public MenuBarItem(string title)
        {
            AutoSizeAxes = Axes.Both;

            AddInternal(Content = new Container
            {
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                AutoSizeAxes = Axes.Both,
                Child = TitleText = CreateTitleText(),
            });

            AddInternal(ContextMenu = CreateContextMenu());
            ContextMenu.Anchor = Anchor.BottomLeft;
            ContextMenu.BypassAutoSizeAxes = Axes.Both;
            ContextMenu.OnClose += Close;

            TitleText.Anchor = Anchor.Centre;
            TitleText.Origin = Anchor.Centre;
            TitleText.Text = title;
        }

        protected override bool OnClick(InputState state)
        {
            switch (State)
            {
                case MenuState.Opened:
                    Close();
                    break;
                case MenuState.Closed:
                    Open();
                    break;
            }

            return true;
        }

        /// <summary>
        /// The state of the <see cref="ContextMenu{TContextItem}"/> of this <see cref="MenuBarItem"/>.
        /// </summary>
        public MenuState State => ContextMenu.State;

        /// <summary>
        /// Opens the <see cref="ContextMenu{TContextItem}"/> of this <see cref="MenuBarItem"/>.
        /// </summary>
        public virtual void Open() => ContextMenu.Open();

        /// <summary>
        /// Closes the <see cref="ContextMenu{TContextItem}"/> of this <see cref="MenuBarItem"/>.
        /// </summary>
        public virtual void Close() => ContextMenu.Close();

        /// <summary>
        /// Creates the <see cref="ContextMenu{TContextItem}"/> that will be shown when this <see cref="MenuBarItem"/> is opened.
        /// </summary>
        /// <returns>The <see cref="ContextMenu{TContextItem}"/>.</returns>
        protected virtual ContextMenu<ContextMenuItem> CreateContextMenu() => new ContextMenu<ContextMenuItem>();

        /// <summary>
        /// Creates the <see cref="SpriteText"/> that will be shown as the title of this <see cref="MenuBarItem"/>.
        /// </summary>
        /// <returns>The <see cref="SpriteText"/>.</returns>
        protected virtual SpriteText CreateTitleText() => new SpriteText();
    }
}
