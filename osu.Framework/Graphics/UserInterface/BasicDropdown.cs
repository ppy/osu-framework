﻿// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics.Sprites;
using osuTK.Graphics;

namespace osu.Framework.Graphics.UserInterface
{
    public class BasicDropdown<T> : Dropdown<T>
    {
        protected override DropdownMenu CreateMenu() => new BasicDropdownMenu();

        protected override DropdownHeader CreateHeader() => new BasicDropdownHeader();

        public BasicDropdown()
        {
            Header.CornerRadius = 4;
        }

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
                Foreground.Padding = new MarginPadding(4);
                BackgroundColour = new Color4(255, 255, 255, 100);
                BackgroundColourHover = Color4.HotPink;
                Children = new[]
                {
                    label = new SpriteText(),
                };
            }
        }

        private class BasicDropdownMenu : DropdownMenu
        {
            public BasicDropdownMenu()
            {
                CornerRadius = 4;
            }

            protected override DrawableMenuItem CreateDrawableMenuItem(MenuItem item) => new DrawableBasicDropdownMenuItem(item);

            private class DrawableBasicDropdownMenuItem : DrawableDropdownMenuItem
            {
                public DrawableBasicDropdownMenuItem(MenuItem item)
                    : base(item)
                {
                    Foreground.Padding = new MarginPadding(2);
                }

                protected override Drawable CreateContent() => new SpriteText();
            }
        }
    }
}
