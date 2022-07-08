// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;

namespace osu.Framework.Graphics.UserInterface
{
    public class BasicMenu : Menu
    {
        public BasicMenu(Direction direction, bool topLevelMenu = false)
            : base(direction, topLevelMenu)
        {
            BackgroundColour = FrameworkColour.Blue;
        }

        protected override Menu CreateSubMenu() => new BasicMenu(Direction.Vertical)
        {
            Anchor = Direction == Direction.Horizontal ? Anchor.BottomLeft : Anchor.TopRight
        };

        protected override DrawableMenuItem CreateDrawableMenuItem(MenuItem item) => new BasicDrawableMenuItem(item);

        protected override ScrollContainer<Drawable> CreateScrollContainer(Direction direction) => new BasicScrollContainer(direction);

        public class BasicDrawableMenuItem : DrawableMenuItem
        {
            public BasicDrawableMenuItem(MenuItem item)
                : base(item)
            {
                BackgroundColour = FrameworkColour.BlueGreen;
                BackgroundColourHover = FrameworkColour.Green;
            }

            protected override Drawable CreateContent() => new SpriteText
            {
                Anchor = Anchor.CentreLeft,
                Origin = Anchor.CentreLeft,
                Padding = new MarginPadding(2),
                Font = FrameworkFont.Condensed,
            };
        }
    }
}
