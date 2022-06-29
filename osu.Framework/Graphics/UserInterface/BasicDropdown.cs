// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Localisation;

namespace osu.Framework.Graphics.UserInterface
{
    public class BasicDropdown<T> : Dropdown<T>
    {
        protected override DropdownMenu CreateMenu() => new BasicDropdownMenu();

        protected override DropdownHeader CreateHeader() => new BasicDropdownHeader();

        public class BasicDropdownHeader : DropdownHeader
        {
            private readonly SpriteText label;

            protected internal override LocalisableString Label
            {
                get => label.Text;
                set => label.Text = value;
            }

            public BasicDropdownHeader()
            {
                var font = FrameworkFont.Condensed;

                Foreground.Padding = new MarginPadding(5);
                BackgroundColour = FrameworkColour.Green;
                BackgroundColourHover = FrameworkColour.YellowGreen;

                Children = new[]
                {
                    label = new SpriteText
                    {
                        AlwaysPresent = true,
                        Font = font,
                        Height = font.Size,
                    },
                };
            }
        }

        public class BasicDropdownMenu : DropdownMenu
        {
            protected override Menu CreateSubMenu() => new BasicMenu(Direction.Vertical);

            protected override DrawableDropdownMenuItem CreateDrawableDropdownMenuItem(MenuItem item) => new DrawableBasicDropdownMenuItem(item);

            protected override ScrollContainer<Drawable> CreateScrollContainer(Direction direction) => new BasicScrollContainer(direction);

            private class DrawableBasicDropdownMenuItem : DrawableDropdownMenuItem
            {
                public DrawableBasicDropdownMenuItem(MenuItem item)
                    : base(item)
                {
                    Foreground.Padding = new MarginPadding(2);
                    BackgroundColour = FrameworkColour.BlueGreen;
                    BackgroundColourHover = FrameworkColour.Green;
                    BackgroundColourSelected = FrameworkColour.GreenDark;
                }

                protected override Drawable CreateContent() => new SpriteText
                {
                    Font = FrameworkFont.Condensed
                };
            }
        }
    }
}
