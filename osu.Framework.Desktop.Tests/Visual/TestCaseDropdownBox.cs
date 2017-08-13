﻿// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Testing;
using OpenTK;
using OpenTK.Graphics;

namespace osu.Framework.Desktop.Tests.Visual
{
    [TestFixture]
    internal class TestCaseDropdownBox : TestCase
    {
        public override string Description => @"Drop-down boxes";

        private const int items_to_add = 10;

        public TestCaseDropdownBox()
        {
            StyledDropdownMenu styledDropdownMenu, styledDropdownMenu2;

            var testItems = new string[10];
            int i = 0;
            while (i < items_to_add)
                testItems[i] = @"test " + i++;

            Add(styledDropdownMenu = new StyledDropdownMenu
            {
                Width = 150,
                Position = new Vector2(200, 70),
                Items = testItems.Select(item => new KeyValuePair<string, string>(item, item)),
            });

            Add(styledDropdownMenu2 = new StyledDropdownMenu
            {
                Width = 150,
                Position = new Vector2(400, 70),
                Items = testItems.Select(item => new KeyValuePair<string, string>(item, item)),
            });

            AddStep("click dropdown1", () => toggleDropdownViaClick(styledDropdownMenu));
            AddAssert("dropdown is open", () => getMenuFromDropdown(styledDropdownMenu).State == MenuState.Opened);

            AddRepeatStep("add item", () => styledDropdownMenu.AddDropdownItem(@"test " + i, @"test " + i++), items_to_add);
            AddAssert("item count is correct", () => styledDropdownMenu.Items.Count() == items_to_add * 2);

            AddStep("click item 13", () => getMenuFromDropdown(styledDropdownMenu).ItemsContainer.Children[13].TriggerOnClick());

            AddAssert("dropdown1 is closed", () => getMenuFromDropdown(styledDropdownMenu).State == MenuState.Closed);
            AddAssert("item 13 is selected", () => styledDropdownMenu.Current == styledDropdownMenu.Items.ElementAt(13).Value);

            AddStep("select item 15", () => styledDropdownMenu.Current.Value = styledDropdownMenu.Items.ElementAt(15).Value);
            AddAssert("item 15 is selected", () => styledDropdownMenu.Current == styledDropdownMenu.Items.ElementAt(15).Value);

            AddStep("click dropdown1", () => toggleDropdownViaClick(styledDropdownMenu));
            AddAssert("dropdown1 is open", () => getMenuFromDropdown(styledDropdownMenu).State == MenuState.Opened);

            AddStep("click dropdown2", () => toggleDropdownViaClick(styledDropdownMenu2));

            AddAssert("dropdown1 is closed", () => getMenuFromDropdown(styledDropdownMenu).State == MenuState.Closed);
            AddAssert("dropdown2 is open", () => getMenuFromDropdown(styledDropdownMenu2).State == MenuState.Opened);
        }

        private Menu getMenuFromDropdown(StyledDropdownMenu dropdown) => (Menu)dropdown.Children[1];

        private void toggleDropdownViaClick(StyledDropdownMenu dropdown) => dropdown.Children.First().TriggerOnClick();

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

            protected internal override string Label
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
            public StyledDropdownMenuItem(string text)
                : base(text, text)
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
