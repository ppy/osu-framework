// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System.Collections.Generic;
using System.Linq;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Input;
using osu.Framework.Input.EventArgs;
using osu.Framework.Testing;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Input;

namespace osu.Framework.Tests.Visual
{
    public class TestCaseDropdownBox : ManualInputManagerTestCase
    {
        private const int items_to_add = 10;

        public TestCaseDropdownBox()
        {
            StyledDropdown styledDropdown, styledDropdownMenu2, keyboardInputDropdown;

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

            PlatformActionContainer platformActionContainer;

            Add(platformActionContainer = new PlatformActionContainer
            {
                Child = keyboardInputDropdown = new StyledDropdown
                {
                    Width = 150,
                    Position = new Vector2(600, 70),
                    Items = testItems.Select(item => new KeyValuePair<string, string>(item, item)),
                }
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

            int currentKeyboardInputDropdownIndex() => keyboardInputDropdown.Items.Select(ii => ii.Value).ToList().IndexOf(keyboardInputDropdown.Current);
            var expectedIndex = 0;

            AddStep($"dropdown3: perform {Key.Up} keypress", () =>
            {
                expectedIndex = MathHelper.Clamp(currentKeyboardInputDropdownIndex() - 1, 0, keyboardInputDropdown.Items.Count() - 1);

                keyboardInputDropdown.Header.TriggerOnKeyDown(null, new KeyDownEventArgs { Key = Key.Up });
                keyboardInputDropdown.Header.TriggerOnKeyUp(null, new KeyUpEventArgs { Key = Key.Up });
            });
            AddAssert("Previous dropdown3 item is selected", () => currentKeyboardInputDropdownIndex() == expectedIndex);

            AddStep($"dropdown3: perform {Key.Down} keypress", () =>
            {
                expectedIndex = MathHelper.Clamp(currentKeyboardInputDropdownIndex() + 1, 0, keyboardInputDropdown.Items.Count() - 1);

                keyboardInputDropdown.Header.TriggerOnKeyDown(null, new KeyDownEventArgs { Key = Key.Down });
                keyboardInputDropdown.Header.TriggerOnKeyUp(null, new KeyUpEventArgs { Key = Key.Down });
            });
            AddAssert("Next dropdown3 item is selected", () => currentKeyboardInputDropdownIndex() == expectedIndex);

            void performPlatformAction(PlatformAction action)
            {
                var tIsHovered = keyboardInputDropdown.Header.IsHovered;
                var tHasFocus = keyboardInputDropdown.Header.HasFocus;

                keyboardInputDropdown.Header.IsHovered = true;
                keyboardInputDropdown.Header.HasFocus = true;

                platformActionContainer.TriggerPressed(action);
                platformActionContainer.TriggerReleased(action);

                keyboardInputDropdown.Header.IsHovered = tIsHovered;
                keyboardInputDropdown.Header.HasFocus = tHasFocus;
            }

            AddStep($"dropdown3: perform {PlatformActionType.ListStart} action", () => performPlatformAction(new PlatformAction(PlatformActionType.ListStart)));
            AddAssert("dropdown3: first item selected", () => currentKeyboardInputDropdownIndex() == 0);

            AddStep($"dropdown3: perform {PlatformActionType.ListEnd} action", () => { performPlatformAction(new PlatformAction(PlatformActionType.ListEnd)); });
            AddAssert("dropdown3: last item selected", () => currentKeyboardInputDropdownIndex() == keyboardInputDropdown.Items.Count() - 1);
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
