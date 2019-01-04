// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System.Linq;
using NUnit.Framework;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Input;
using osu.Framework.Input.Events;
using osu.Framework.Input.States;
using osu.Framework.Testing;
using osuTK;
using osuTK.Graphics;
using osuTK.Input;

namespace osu.Framework.Tests.Visual
{
    public class TestCaseDropdownBox : ManualInputManagerTestCase
    {
        private const int items_to_add = 10;
        private const float explicit_height = 120;
        private float calculatedHeight;

        private readonly StyledDropdown styledDropdown, styledDropdownMenu2, keyboardSelectionDropdown, keyboardPreselectionDropdown;
        private readonly PlatformActionContainer platformActionContainerKeyboardSelection, platformActionContainerKeyboardPreselection;

        private int previousIndex;
        private int lastVisibleIndexOnTheCurrentPage, lastVisibleIndexOnTheNextPage;
        private int firstVisibleIndexOnTheCurrentPage, firstVisibleIndexOnThePreviousPage;

        public TestCaseDropdownBox()
        {
            var testItems = new string[10];
            int i = 0;
            while (i < items_to_add)
                testItems[i] = @"test " + i++;

            Add(styledDropdown = new StyledDropdown
            {
                Width = 150,
                Position = new Vector2(50, 70),
                Items = testItems
            });

            Add(styledDropdownMenu2 = new StyledDropdown
            {
                Width = 150,
                Position = new Vector2(250, 70),
                Items = testItems
            });

            Add(platformActionContainerKeyboardSelection = new PlatformActionContainer
            {
                Child = keyboardSelectionDropdown = new StyledDropdown
                {
                    Width = 150,
                    Position = new Vector2(450, 70),
                    Items = testItems
                }
            });

            Add(platformActionContainerKeyboardPreselection = new PlatformActionContainer
            {
                Child = keyboardPreselectionDropdown = new StyledDropdown
                {
                    Width = 150,
                    Position = new Vector2(650, 70),
                    Items = testItems
                }
            });
            keyboardPreselectionDropdown.Menu.MaxHeight = explicit_height;
        }

        [Test]
        public void Basic()
        {
            var i = items_to_add;

            AddStep("click dropdown1", () => toggleDropdownViaClick(styledDropdown));
            AddAssert("dropdown is open", () => styledDropdown.Menu.State == MenuState.Open);

            AddRepeatStep("add item", () => styledDropdown.AddDropdownItem("test " + i++), items_to_add);
            AddAssert("item count is correct", () => styledDropdown.Items.Count() == items_to_add * 2);

            AddStep($"Set dropdown1 height to {explicit_height}", () =>
            {
                calculatedHeight = styledDropdown.Menu.Height;
                styledDropdown.Menu.MaxHeight = explicit_height;
            });
            AddAssert($"dropdown1 height is {explicit_height}", () => styledDropdown.Menu.Height == explicit_height);

            AddStep($"Set dropdown1 height to {float.PositiveInfinity}", () => styledDropdown.Menu.MaxHeight = float.PositiveInfinity);
            AddAssert("dropdown1 height is calculated automatically", () => styledDropdown.Menu.Height == calculatedHeight);

            AddStep("click item 13", () => styledDropdown.SelectItem(styledDropdown.Menu.Items[13]));

            AddAssert("dropdown1 is closed", () => styledDropdown.Menu.State == MenuState.Closed);
            AddAssert("item 13 is selected", () => styledDropdown.Current == styledDropdown.Items.ElementAt(13));

            AddStep("select item 15", () => styledDropdown.Current.Value = styledDropdown.Items.ElementAt(15));
            AddAssert("item 15 is selected", () => styledDropdown.Current == styledDropdown.Items.ElementAt(15));

            AddStep("click dropdown1", () => toggleDropdownViaClick(styledDropdown));
            AddAssert("dropdown1 is open", () => styledDropdown.Menu.State == MenuState.Open);

            AddStep("click dropdown2", () => toggleDropdownViaClick(styledDropdownMenu2));

            AddAssert("dropdown1 is closed", () => styledDropdown.Menu.State == MenuState.Closed);
            AddAssert("dropdown2 is open", () => styledDropdownMenu2.Menu.State == MenuState.Open);

            AddStep("select 'invalid'", () => styledDropdown.Current.Value = "invalid");

            AddAssert("'invalid' is selected", () => styledDropdown.Current == "invalid");
            AddAssert("label shows 'invalid'", () => styledDropdown.Header.Label == "invalid");

            AddStep("select item 2", () => styledDropdown.Current.Value = styledDropdown.Items.ElementAt(2));
            AddAssert("item 2 is selected", () => styledDropdown.Current == styledDropdown.Items.ElementAt(2));

            AddStep("Select next item", () =>
            {
                previousIndex = keyboardSelectionDropdown.SelectedIndex;
                performKeystroke(keyboardSelectionDropdown.Header, Key.Down);
            });

            AddAssert("Next item is selected", () => keyboardSelectionDropdown.SelectedIndex == previousIndex + 1);

            AddStep("Select previous item", () =>
            {
                previousIndex = keyboardSelectionDropdown.SelectedIndex;
                performKeystroke(keyboardSelectionDropdown.Header, Key.Up);
            });

            AddAssert("Previous item is selected", () => keyboardSelectionDropdown.SelectedIndex == previousIndex - 1);

            AddStep("Select last item", () => performPlatformAction(new PlatformAction(PlatformActionType.ListEnd), platformActionContainerKeyboardSelection, keyboardSelectionDropdown.Header));

            AddAssert("Last item selected", () => keyboardSelectionDropdown.SelectedItem == keyboardSelectionDropdown.Menu.DrawableMenuItems.Last().Item);

            AddStep("Select first item", () => performPlatformAction(new PlatformAction(PlatformActionType.ListStart), platformActionContainerKeyboardSelection, keyboardSelectionDropdown.Header));

            AddAssert("First item selected", () => keyboardSelectionDropdown.SelectedItem == keyboardSelectionDropdown.Menu.DrawableMenuItems.First().Item);

            AddStep("click keyboardPreselectionDropdown", () => toggleDropdownViaClick(keyboardPreselectionDropdown));
            AddAssert("dropdown is open", () => keyboardPreselectionDropdown.Menu.State == MenuState.Open);

            AddStep("Preselect next item", () =>
            {
                previousIndex = keyboardPreselectionDropdown.SelectedIndex;
                performKeystroke(keyboardPreselectionDropdown.Header, Key.Down);
            });

            AddAssert("Next item is preselected", () => keyboardPreselectionDropdown.SelectedIndex == previousIndex + 1);

            AddStep("Preselect previous item", () =>
            {
                previousIndex = keyboardPreselectionDropdown.SelectedIndex;
                performKeystroke(keyboardPreselectionDropdown.Header, Key.Up);
            });

            AddAssert("Previous item is preselected", () => keyboardPreselectionDropdown.SelectedIndex == previousIndex - 1);

            AddStep("Preselect last visible item", () =>
            {
                lastVisibleIndexOnTheCurrentPage = keyboardPreselectionDropdown.Menu.DrawableMenuItems.ToList().IndexOf(keyboardPreselectionDropdown.Menu.VisibleMenuItems.Last());
                performKeystroke(keyboardPreselectionDropdown.Menu, Key.PageDown);
            });

            AddAssert("Last visible item preselected", () => keyboardPreselectionDropdown.PreselectedIndex == lastVisibleIndexOnTheCurrentPage);

            AddStep("Preselect last visible item on the next page", () =>
            {
                lastVisibleIndexOnTheNextPage =
                    MathHelper.Clamp(lastVisibleIndexOnTheCurrentPage + keyboardPreselectionDropdown.Menu.VisibleMenuItems.Count(), 0, keyboardPreselectionDropdown.Menu.Items.Count - 1);

                performKeystroke(keyboardPreselectionDropdown.Menu, Key.PageDown);
            });

            AddAssert("Last visible item on the next page preselected", () => keyboardPreselectionDropdown.PreselectedIndex == lastVisibleIndexOnTheNextPage);

            AddStep("Preselect first visible item", () =>
            {
                firstVisibleIndexOnTheCurrentPage = keyboardPreselectionDropdown.Menu.DrawableMenuItems.ToList().IndexOf(keyboardPreselectionDropdown.Menu.VisibleMenuItems.First());
                performKeystroke(keyboardPreselectionDropdown.Menu, Key.PageUp);
            });

            AddAssert("First visible item preselected", () => keyboardPreselectionDropdown.PreselectedIndex == firstVisibleIndexOnTheCurrentPage);

            AddStep("Preselect first visible item on the previous page", () =>
            {
                firstVisibleIndexOnThePreviousPage = MathHelper.Clamp(firstVisibleIndexOnTheCurrentPage - keyboardPreselectionDropdown.Menu.VisibleMenuItems.Count(), 0,
                    keyboardPreselectionDropdown.Menu.Items.Count - 1);
                performKeystroke(keyboardPreselectionDropdown.Menu, Key.PageUp);
            });

            AddAssert("First visible item on the previous page selected", () => keyboardPreselectionDropdown.PreselectedIndex == firstVisibleIndexOnThePreviousPage);

            AddAssert("First item is preselected", () => keyboardPreselectionDropdown.Menu.PreselectedItem.Item == keyboardPreselectionDropdown.Menu.DrawableMenuItems.First().Item);

            AddStep("Preselect last item", () => performPlatformAction(new PlatformAction(PlatformActionType.ListEnd), platformActionContainerKeyboardPreselection, keyboardPreselectionDropdown));

            AddAssert("Last item preselected", () => keyboardPreselectionDropdown.Menu.PreselectedItem.Item == keyboardPreselectionDropdown.Menu.DrawableMenuItems.Last().Item);

            AddStep("Preselect first item", () => performPlatformAction(new PlatformAction(PlatformActionType.ListStart), platformActionContainerKeyboardPreselection, keyboardPreselectionDropdown));

            AddAssert("First item preselected", () => keyboardPreselectionDropdown.Menu.PreselectedItem.Item == keyboardPreselectionDropdown.Menu.DrawableMenuItems.First().Item);
        }

        private void performKeystroke(Drawable drawable, Key key)
        {
            drawable.TriggerEvent(new KeyDownEvent(new InputState(), key));
            drawable.TriggerEvent(new KeyUpEvent(new InputState(), key));
        }

        private void performPlatformAction(PlatformAction action, PlatformActionContainer platformActionContainer, Drawable drawable)
        {
            var tempIsHovered = drawable.IsHovered;
            var tempHasFocus = drawable.HasFocus;

            drawable.IsHovered = true;
            drawable.HasFocus = true;

            platformActionContainer.TriggerPressed(action);
            platformActionContainer.TriggerReleased(action);

            drawable.IsHovered = tempIsHovered;
            drawable.HasFocus = tempHasFocus;
        }

        private void toggleDropdownViaClick(StyledDropdown dropdown)
        {
            InputManager.MoveMouseTo(dropdown.Children.First());
            InputManager.Click(MouseButton.Left);
        }

        private class StyledDropdown : BasicDropdown<string>
        {
            public new DropdownMenu Menu => base.Menu;

            protected override DropdownMenu CreateMenu() => new StyledDropdownMenu();

            protected override DropdownHeader CreateHeader() => new StyledDropdownHeader();

            public void SelectItem(MenuItem item) => ((StyledDropdownMenu)Menu).SelectItem(item);
            internal new DropdownMenuItem<string> SelectedItem => base.SelectedItem;

            public int SelectedIndex => Menu.DrawableMenuItems.Select(d => d.Item).ToList().IndexOf(SelectedItem);
            public int PreselectedIndex => Menu.DrawableMenuItems.ToList().IndexOf(Menu.PreselectedItem);

            private class StyledDropdownMenu : DropdownMenu
            {
                public void SelectItem(MenuItem item) => Children.FirstOrDefault(c => c.Item == item)?
                    .TriggerEvent(new ClickEvent(GetContainingInputManager().CurrentState, MouseButton.Left));
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
