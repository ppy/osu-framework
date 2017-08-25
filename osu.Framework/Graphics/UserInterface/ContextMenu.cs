// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Caching;
using System;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Input;

namespace osu.Framework.Graphics.UserInterface
{
    /// <summary>
    /// Creates a container that manages <see cref="TItem"/>s within a <see cref="Menu"/>.
    /// This container will auto-size its width to fit the maximum size of the <see cref="ContextMenuItem"/>s inside <see cref="Menu{TItem}.Items"/>.
    /// </summary>
    public class ContextMenu<TItem> : Menu<TItem>
        where TItem : MenuItem
    {
        private float computeMenuWidth()
        {
            // The menu items cannot be both relative and auto-sized to fit the entire width of the menu so they (along with the menu)
            // are defined to be relatively-sized on the x-axis. We need to define the size ourselves to give them a valid size.
            float textWidth = 0;
            float contentWidth = 0;

            foreach (var item in Children)
            {
                var ourRepresentation = (ContextMenuItemRepresentation)item;

                textWidth = Math.Max(textWidth, ourRepresentation.TextDrawWidth);
                contentWidth = Math.Max(contentWidth, ourRepresentation.ContentDrawWidth);
            }

            return textWidth + contentWidth;
        }

        protected override void UpdateAfterChildren()
        {
            base.UpdateAfterChildren();
            if (!menuWidth.IsValid)
            {
                Width = computeMenuWidth();
                menuWidth.Validate();
            }
        }

        private Cached menuWidth = new Cached();

        public override void InvalidateFromChild(Invalidation invalidation)
        {
            if ((invalidation & Invalidation.RequiredParentSizeToFit) > 0)
                menuWidth.Invalidate();
            base.InvalidateFromChild(invalidation);
        }

        protected override MenuItemRepresentation CreateMenuItemRepresentation(TItem model) => new ContextMenuItemRepresentation(model, this);

        #region ContextMenuItemRepresentation
        protected class ContextMenuItemRepresentation : MenuItemRepresentation
        {
            /// <summary>
            /// The draw width of the text of this <see cref="ContextMenuItemRepresentation"/>.
            /// </summary>
            public float TextDrawWidth => text.DrawWidth;

            /// <summary>
            /// The draw width of the content of this <see cref="ContextMenuItemRepresentation"/>. This does not include <see cref="TextDrawWidth"/>.
            /// </summary>
            public float ContentDrawWidth => content.DrawWidth;

            private readonly Container text;
            private readonly FillFlowContainer content;

            private readonly ContextMenu<TItem> menu;

            public ContextMenuItemRepresentation(TItem model, ContextMenu<TItem> menu)
                : base(model)
            {
                this.menu = menu;

                AddRange(new Drawable[]
                {
                    text = CreateTextContainer(model.Text),
                    content = new FillFlowContainer
                    {
                        Direction = FillDirection.Horizontal,
                        AutoSizeAxes = Axes.Both,
                        Anchor = Anchor.CentreRight,
                        Origin = Anchor.CentreRight,
                    }
                });
            }

            protected override bool OnClick(InputState state)
            {
                if (!base.OnClick(state))
                    return false;

                menu.Close();
                return true;
            }

            /// <summary>
            /// Creates a new container with text which will be displayed at the centre-left of this <see cref="ContextMenuItemRepresentation"/>.
            /// </summary>
            /// <param name="title">The text to be displayed in this <see cref="ContextMenuItemRepresentation"/>.</param>
            protected virtual Container CreateTextContainer(string title) => new Container
            {
                AutoSizeAxes = Axes.Both,
                Anchor = Anchor.CentreLeft,
                Origin = Anchor.CentreLeft,
                Child = new SpriteText
                {
                    Anchor = Anchor.CentreLeft,
                    Origin = Anchor.CentreLeft,
                    TextSize = 17,
                    Text = title,
                }
            };
        }
        #endregion
    }
}
