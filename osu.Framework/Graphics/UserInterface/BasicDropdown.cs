// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Graphics.Sprites;
using OpenTK.Graphics;

namespace osu.Framework.Graphics.UserInterface
{
    public class BasicDropdown<T> : Dropdown<T>
    {
        protected override Menu CreateMenu() => new Menu();

        protected override DropdownHeader CreateHeader() => new BasicDropdownHeader();

        protected override DropdownMenuItem<T> CreateMenuItem(string key, T value) => new BasicDropdownMenuItem(key, value);

        public BasicDropdown()
        {
            Header.CornerRadius = 4;
            DropdownMenu.CornerRadius = 4;
        }

        public class BasicDropdownHeader : DropdownHeader
        {
            private readonly SpriteText label;

            protected internal override string Label
            {
                get { return label.Text; }
                set { label.Text = value; }
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

        public class BasicDropdownMenuItem : DropdownMenuItem<T>
        {
            public BasicDropdownMenuItem(string key, T value)
                : base(key, value)
            {
                AutoSizeAxes = Axes.Y;
                Foreground.Padding = new MarginPadding(2);

                Children = new[]
                {
                    new SpriteText { Text = key },
                };
            }
        }
    }
}
