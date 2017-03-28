// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System.Collections.Generic;
using System.Linq;
using OpenTK;
using OpenTK.Graphics;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Testing;

namespace osu.Framework.VisualTests.Tests
{
    internal class TestCaseDropdownBox : TestCase
    {
        public override string Description => @"Drop-down boxes";

        private StyledDropdownMenu styledDropdownMenu;

        public override void Reset()
        {
            base.Reset();
            string[] testItems = new string[10];
            int i = 0;
            while (i < 10)
                testItems[i] = @"test " + i++;
            styledDropdownMenu = new StyledDropdownMenu
            {
                Width = 150,
                Position = new Vector2(200, 70),
                //Description = @"Drop-down menu",
                Depth = 1,
                Items = testItems.Select(item => new KeyValuePair<string, string>(item, item)),
            };
            styledDropdownMenu.SelectedValue.Value = testItems[4];
            Add(styledDropdownMenu);

            AddButton("AddItem", () => styledDropdownMenu.AddDropdownItem(@"test " + i, @"test " + i++));
        }

        private class StyledDropdownMenu : Dropdown<string>
        {
            protected override Menu CreateMenu() => new Menu();

            protected override DropdownHeader CreateHeader() => new StyledDropdownHeader();

            protected override DropdownMenuItem<string> CreateMenuItem(string key, string value) => new StyledDropdownMenuItem(key);

            public StyledDropdownMenu()
            {
                Header.CornerRadius = 4;
                DropdownMenu.CornerRadius = 4;
            }
        }

        private class StyledDropdownHeader : DropdownHeader
        {
            private readonly SpriteText label;
            protected override string Label
            {
                get { return label.Text; }
                set { label.Text = value; }
            }

            public StyledDropdownHeader()
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

        private class StyledDropdownMenuItem : DropdownMenuItem<string>
        {
            public StyledDropdownMenuItem(string text) : base(text, text)
            {
                AutoSizeAxes = Axes.Y;
                Foreground.Padding = new MarginPadding(2);

                Children = new[]
                {
                    new SpriteText { Text = text },
                };
            }
        }
    }
}