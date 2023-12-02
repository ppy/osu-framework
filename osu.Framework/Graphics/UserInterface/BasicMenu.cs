// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;

namespace osu.Framework.Graphics.UserInterface
{
    public partial class BasicMenu : Menu
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

        public partial class BasicDrawableMenuItem : DrawableMenuItem
        {
            private bool matchingFilter = true;

            public override bool MatchingFilter
            {
                get => matchingFilter;
                set
                {
                    matchingFilter = value;
                    this.FadeTo(value ? 1 : 0);
                }
            }

            public override bool FilteringActive
            {
                set { }
            }

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
