// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System.Collections.Generic;
using System.Linq;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Testing;
using OpenTK;
using OpenTK.Graphics;

namespace osu.Framework.Tests.Visual
{
    public class TestCaseDropdownBox : TestCase
    {
        private const int items_to_add = 10;

        public TestCaseDropdownBox()
        {
            StyledDropdown styledDropdown, styledDropdownMenu2;

            var testItems = new string[10];
            int i = 0;
            while (i < items_to_add)
                testItems[i] = @"test " + i++;

            Add(styledDropdown = new StyledDropdown
            {
                Width = 150,
                Position = new Vector2(200, 70),
                Items = testItems.Select(item => new KeyValuePair<string, string>(item, item)),
            });

            Add(styledDropdownMenu2 = new StyledDropdown
            {
                Width = 150,
                Position = new Vector2(400, 70),
                Items = testItems.Select(item => new KeyValuePair<string, string>(item, item)),
            });

            AddStep("click dropdown1", () => toggleDropdownViaClick(styledDropdown));
            AddAssert("dropdown is open", () => styledDropdown.Menu.State == MenuState.Open);

            AddRepeatStep("add item", () => styledDropdown.AddDropdownItem(@"test " + i, @"test " + i++), items_to_add);
            AddAssert("item count is correct", () => styledDropdown.Items.Count() == items_to_add * 2);

            AddStep("click item 13", () => styledDropdown.SelectItem(styledDropdown.Menu.Items[13]));

            AddAssert("dropdown1 is closed", () => styledDropdown.Menu.State == MenuState.Closed);
            AddAssert("item 13 is selected", () => styledDropdown.Current == styledDropdown.Items.ElementAt(13).Value);

            AddStep("select item 15", () => styledDropdown.Current.Value = styledDropdown.Items.ElementAt(15).Value);
            AddAssert("item 15 is selected", () => styledDropdown.Current == styledDropdown.Items.ElementAt(15).Value);

            AddStep("click dropdown1", () => toggleDropdownViaClick(styledDropdown));
            AddAssert("dropdown1 is open", () => styledDropdown.Menu.State == MenuState.Open);

            AddStep("click dropdown2", () => toggleDropdownViaClick(styledDropdownMenu2));

            AddAssert("dropdown1 is closed", () => styledDropdown.Menu.State == MenuState.Closed);
            AddAssert("dropdown2 is open", () => styledDropdownMenu2.Menu.State == MenuState.Open);
        }

        private void toggleDropdownViaClick(StyledDropdown dropdown) => dropdown.Children.First().TriggerOnClick();

        private class StyledDropdown : BasicDropdown<string>
        {
            public new DropdownMenu Menu => base.Menu;

            protected override DropdownMenu CreateMenu() => new StyledDropdownMenu();

            protected override DropdownHeader CreateHeader() => new StyledDropdownHeader();

            public void SelectItem(MenuItem item) => ((StyledDropdownMenu)Menu).SelectItem(item);

            private class StyledDropdownMenu : DropdownMenu
            {
                public void SelectItem(MenuItem item) => Children.FirstOrDefault(c => c.Item == item)?.TriggerOnClick();
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
    }
}
