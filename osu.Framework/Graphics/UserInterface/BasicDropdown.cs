// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics.Sprites;

namespace osu.Framework.Graphics.UserInterface
{
    public class BasicDropdown<T> : Dropdown<T>
    {
        protected override DropdownMenu CreateMenu() => new BasicDropdownMenu();

        protected override DropdownHeader CreateHeader() => new BasicDropdownHeader();

        public class BasicDropdownHeader : DropdownHeader
        {
            private readonly SpriteText label;

            protected internal override string Label
            {
                get => label.Text;
                set => label.Text = value;
            }

            public BasicDropdownHeader()
            {
                var font = new FontUsage("RobotoCondensed", weight: "Regular");

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
            protected override DrawableMenuItem CreateDrawableMenuItem(MenuItem item) => new DrawableBasicDropdownMenuItem(item);

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
                    Font = new FontUsage("RobotoCondensed", weight: "Regular")
                };
            }
        }
    }
}
