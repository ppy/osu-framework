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
    public class TestCaseDropdownBox : TestCase
    {
        private const int items_to_add = 10;

        public TestCaseDropdownBox()
        {
            StyledDropdown styledDropdown, styledDropdownMenu2, keyboardInputDropdown1, keyboardInputDropdown2, keyboardInputDropdown3;

            var testItems = new string[10];
            int i = 0;
            while (i < items_to_add)
                testItems[i] = @"test " + i++;

            Add(styledDropdown = new StyledDropdown
            {
                Width = 150,
                Position = new Vector2(50, 70),
                Items = testItems.Select(item => new KeyValuePair<string, string>(item, item)),
            });

            Add(styledDropdownMenu2 = new StyledDropdown
            {
                Width = 150,
                Position = new Vector2(250, 70),
                Items = testItems.Select(item => new KeyValuePair<string, string>(item, item)),
            });

            PlatformActionContainer platformActionContainer1, platformActionContainer2;

            Add(keyboardInputDropdown1 = new StyledDropdown
            {
                Width = 150,
                Position = new Vector2(450, 70),
                Items = testItems.Select(item => new KeyValuePair<string, string>(item, item)),
            });
            keyboardInputDropdown1.Menu.Height = 80;

            Add(platformActionContainer1 = new PlatformActionContainer
            {
                Child = keyboardInputDropdown2 = new StyledDropdown
                {
                    Width = 150,
                    Position = new Vector2(650, 70),
                    Items = testItems.Select(item => new KeyValuePair<string, string>(item, item)),
                }
            });
            keyboardInputDropdown2.Menu.Height = 80;

            Add(platformActionContainer2 = new PlatformActionContainer
            {
                Child = keyboardInputDropdown3 = new StyledDropdown
                {
                    Width = 150,
                    Position = new Vector2(850, 70),
                    Items = testItems.Select(item => new KeyValuePair<string, string>(item, item)),
                }
            });
            keyboardInputDropdown3.Menu.Height = 80;

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

            AddStep("Select last item using down key", () =>
            {
                while (keyboardInputDropdown1.SelectedItem != keyboardInputDropdown1.Menu.DrawableMenuItems.Last().Item)
                {
                    keyboardInputDropdown1.Header.TriggerOnKeyDown(null, new KeyDownEventArgs { Key = Key.Down });
                    keyboardInputDropdown1.Header.TriggerOnKeyUp(null, new KeyUpEventArgs { Key = Key.Down });
                }
            });

            AddAssert("Last item is selected", () => keyboardInputDropdown1.SelectedItem == keyboardInputDropdown1.Menu.DrawableMenuItems.Last().Item);

            AddStep("Select first item using up key", () =>
            {
                while (keyboardInputDropdown1.SelectedItem != keyboardInputDropdown1.Menu.DrawableMenuItems.First().Item)
                {
                    keyboardInputDropdown1.Header.TriggerOnKeyDown(null, new KeyDownEventArgs { Key = Key.Up });
                    keyboardInputDropdown1.Header.TriggerOnKeyUp(null, new KeyUpEventArgs { Key = Key.Up });
                }
            });

            AddAssert("First item is selected", () => keyboardInputDropdown1.SelectedItem == keyboardInputDropdown1.Menu.DrawableMenuItems.First().Item);

            void performPlatformAction(PlatformAction action, PlatformActionContainer platformActionContainer, Drawable drawable)
            {
                var tIsHovered = drawable.IsHovered;
                var tHasFocus = drawable.HasFocus;

                drawable.IsHovered = true;
                drawable.HasFocus = true;

                platformActionContainer.TriggerPressed(action);
                platformActionContainer.TriggerReleased(action);

                drawable.IsHovered = tIsHovered;
                drawable.HasFocus = tHasFocus;
            }

            AddStep("Select last item", () => performPlatformAction(new PlatformAction(PlatformActionType.ListEnd), platformActionContainer1, keyboardInputDropdown2.Header));

            AddAssert("Last item selected", () => keyboardInputDropdown2.SelectedItem == keyboardInputDropdown2.Menu.DrawableMenuItems.Last().Item);

            AddStep("Select first item", () => performPlatformAction(new PlatformAction(PlatformActionType.ListStart), platformActionContainer1, keyboardInputDropdown2.Header));

            AddAssert("First item selected", () => keyboardInputDropdown2.SelectedItem == keyboardInputDropdown2.Menu.DrawableMenuItems.First().Item);

            AddStep("click keyboardInputDropdown3", () => toggleDropdownViaClick(keyboardInputDropdown3));
            AddAssert("dropdown is open", () => keyboardInputDropdown3.Menu.State == MenuState.Open);

            AddStep("Preselect last item using down key", () =>
            {
                while (keyboardInputDropdown3.Menu?.PreselectedItem?.Item != keyboardInputDropdown3.Menu.DrawableMenuItems.Last().Item)
                {
                    keyboardInputDropdown3.Menu.TriggerOnKeyDown(null, new KeyDownEventArgs { Key = Key.Down });
                    keyboardInputDropdown3.Menu.TriggerOnKeyUp(null, new KeyUpEventArgs { Key = Key.Down });
                }
            });

            AddAssert("Last item is preselected", () => keyboardInputDropdown3.Menu.PreselectedItem.Item == keyboardInputDropdown3.Menu.DrawableMenuItems.Last().Item);

            AddStep("Preselect first item using up key", () =>
            {
                while (keyboardInputDropdown3.Menu?.PreselectedItem?.Item != keyboardInputDropdown3.Menu.DrawableMenuItems.First().Item)
                {
                    keyboardInputDropdown3.Menu.TriggerOnKeyDown(null, new KeyDownEventArgs { Key = Key.Up });
                    keyboardInputDropdown3.Menu.TriggerOnKeyUp(null, new KeyUpEventArgs { Key = Key.Up });
                }
            });

            int lastVisibleIndexOnTheCurrentPage = 0;
            AddStep("Preselect last visible item on the current page", () =>
            {
                lastVisibleIndexOnTheCurrentPage = keyboardInputDropdown3.Menu.DrawableMenuItems.ToList().IndexOf(keyboardInputDropdown3.Menu.VisibleMenuItems.Last());
                keyboardInputDropdown3.Menu.TriggerOnKeyDown(null, new KeyDownEventArgs { Key = Key.PageDown });
                keyboardInputDropdown3.Menu.TriggerOnKeyUp(null, new KeyUpEventArgs { Key = Key.PageDown });
            });

            AddAssert("Last visible item on the current page preselected", () => keyboardInputDropdown3.PreselectedIndex == lastVisibleIndexOnTheCurrentPage);

            int lastVisibleIndexOnTheNextPage = 0;
            AddStep("Preselect last visible item on the next page", () =>
            {
                lastVisibleIndexOnTheNextPage = lastVisibleIndexOnTheCurrentPage + keyboardInputDropdown3.Menu.VisibleMenuItems.Count();
                keyboardInputDropdown3.Menu.TriggerOnKeyDown(null, new KeyDownEventArgs { Key = Key.PageDown });
                keyboardInputDropdown3.Menu.TriggerOnKeyUp(null, new KeyUpEventArgs { Key = Key.PageDown });
            });

            AddAssert("Last visible item on the next page preselected", () => keyboardInputDropdown3.PreselectedIndex == lastVisibleIndexOnTheNextPage);

            int firstVisibleIndexOnTheCurrentPage = 0;
            AddStep("Preselect first visible item on the current page", () =>
            {
                firstVisibleIndexOnTheCurrentPage = keyboardInputDropdown3.Menu.DrawableMenuItems.ToList().IndexOf(keyboardInputDropdown3.Menu.VisibleMenuItems.First());
                keyboardInputDropdown3.Menu.TriggerOnKeyDown(null, new KeyDownEventArgs { Key = Key.PageUp });
                keyboardInputDropdown3.Menu.TriggerOnKeyUp(null, new KeyUpEventArgs { Key = Key.PageUp });
            });

            AddAssert("First visible item on the current page preselected", () => keyboardInputDropdown3.PreselectedIndex == firstVisibleIndexOnTheCurrentPage);

            int firstVisibleIndexOnThePreviousPage = 0;
            AddStep("Preselect first visible item on the previous page", () =>
            {
                firstVisibleIndexOnThePreviousPage = firstVisibleIndexOnTheCurrentPage - keyboardInputDropdown3.Menu.VisibleMenuItems.Count();
                keyboardInputDropdown3.Menu.TriggerOnKeyDown(null, new KeyDownEventArgs { Key = Key.PageUp });
                keyboardInputDropdown3.Menu.TriggerOnKeyUp(null, new KeyUpEventArgs { Key = Key.PageUp });
            });

            AddAssert("First visible item on the previous page selected", () => keyboardInputDropdown3.PreselectedIndex == firstVisibleIndexOnThePreviousPage);

            AddAssert("First item is preselected", () => keyboardInputDropdown3.Menu.PreselectedItem.Item == keyboardInputDropdown3.Menu.DrawableMenuItems.First().Item);

            AddStep("Preselect last item", () => performPlatformAction(new PlatformAction(PlatformActionType.ListEnd), platformActionContainer2, keyboardInputDropdown3));

            AddAssert("Last item preselected", () => keyboardInputDropdown3.Menu.PreselectedItem.Item == keyboardInputDropdown3.Menu.DrawableMenuItems.Last().Item);

            AddStep("Preselect first item", () => performPlatformAction(new PlatformAction(PlatformActionType.ListStart), platformActionContainer2, keyboardInputDropdown3));

            AddAssert("First item preselected", () => keyboardInputDropdown3.Menu.PreselectedItem.Item == keyboardInputDropdown3.Menu.DrawableMenuItems.First().Item);
        }

        private void toggleDropdownViaClick(StyledDropdown dropdown) => dropdown.Children.First().TriggerOnClick();

        private class StyledDropdown : BasicDropdown<string>
        {
            public new DropdownMenu Menu => base.Menu;

            protected override DropdownMenu CreateMenu() => new StyledDropdownMenu();

            protected override DropdownHeader CreateHeader() => new StyledDropdownHeader();

            internal new DropdownMenuItem<string> SelectedItem => base.SelectedItem;

            public void SelectItem(MenuItem item) => ((StyledDropdownMenu)Menu).SelectItem(item);

            public int SelectedIndex => Menu.DrawableMenuItems.Select(d => d.Item).ToList().IndexOf(SelectedItem);
            public int PreselectedIndex => Menu.DrawableMenuItems.ToList().IndexOf(Menu.PreselectedItem);

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
