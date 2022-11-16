// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.Linq;
using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Extensions.IEnumerableExtensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Input;
using osu.Framework.Localisation;
using osu.Framework.Platform;
using osu.Framework.Testing;
using osuTK;
using osuTK.Input;

namespace osu.Framework.Tests.Visual.UserInterface
{
    public class TestSceneDropdown : ManualInputManagerTestScene
    {
        private const int items_to_add = 10;
        private const float explicit_height = 100;
        private float calculatedHeight;
        private readonly TestDropdown testDropdown, testDropdownMenu, bindableDropdown, emptyDropdown, disabledDropdown;
        private readonly PlatformActionContainer platformActionContainerKeyboardSelection, platformActionContainerKeyboardPreselection, platformActionContainerEmptyDropdown;
        private readonly BindableList<TestModel> bindableList = new BindableList<TestModel>();

        private int previousIndex;
        private int lastVisibleIndexOnTheCurrentPage, lastVisibleIndexOnTheNextPage;
        private int firstVisibleIndexOnTheCurrentPage, firstVisibleIndexOnThePreviousPage;

        public TestSceneDropdown()
        {
            var testItems = new TestModel[10];
            int i = 0;
            while (i < items_to_add)
                testItems[i] = @"test " + i++;

            Add(platformActionContainerKeyboardSelection = new PlatformActionContainer
            {
                Child = testDropdown = new TestDropdown
                {
                    Width = 150,
                    Position = new Vector2(50, 50),
                    Items = testItems
                }
            });

            Add(platformActionContainerKeyboardPreselection = new PlatformActionContainer
            {
                Child = testDropdownMenu = new TestDropdown
                {
                    Width = 150,
                    Position = new Vector2(250, 50),
                    Items = testItems
                }
            });
            testDropdownMenu.Menu.MaxHeight = explicit_height;

            Add(bindableDropdown = new TestDropdown
            {
                Width = 150,
                Position = new Vector2(450, 50),
                ItemSource = bindableList
            });

            Add(platformActionContainerEmptyDropdown = new PlatformActionContainer
            {
                Child = emptyDropdown = new TestDropdown
                {
                    Width = 150,
                    Position = new Vector2(650, 50),
                }
            });

            Add(disabledDropdown = new TestDropdown
            {
                Width = 150,
                Position = new Vector2(50, 350),
                Items = testItems,
                Current =
                {
                    Value = testItems[3],
                    Disabled = true
                }
            });
        }

        [Test]
        public void TestExternalBindableChangeKeepsSelection()
        {
            toggleDropdownViaClick(testDropdown, "dropdown1");
            AddStep("click item 4", () =>
            {
                InputManager.MoveMouseTo(testDropdown.Menu.Children[4]);
                InputManager.Click(MouseButton.Left);
            });

            AddAssert("item 4 is selected", () => testDropdown.Current.Value.Identifier == "test 4");

            AddStep("replace items", () =>
            {
                testDropdown.Items = testDropdown.Items.Select(i => new TestModel(i.ToString())).ToArray();
            });

            AddAssert("item 4 is selected", () => testDropdown.Current.Value.Identifier == "test 4");
            AddAssert("item 4 is selected item", () => testDropdown.SelectedItem.Value.Identifier == "test 4");
            AddAssert("item 4 is visually selected", () => (testDropdown.ChildrenOfType<Dropdown<TestModel>.DropdownMenu.DrawableDropdownMenuItem>()
                                                                        .SingleOrDefault(i => i.IsSelected)?
                                                                        .Item as DropdownMenuItem<TestModel>)?.Value.Identifier == "test 4");
        }

        [Test]
        public void TestBasic()
        {
            int i = items_to_add;

            toggleDropdownViaClick(testDropdown, "dropdown1");
            AddAssert("dropdown is open", () => testDropdown.Menu.State == MenuState.Open);

            AddRepeatStep("add item", () => testDropdown.AddDropdownItem("test " + i++), items_to_add);
            AddAssert("item count is correct", () => testDropdown.Items.Count() == items_to_add * 2);

            AddStep($"Set dropdown1 height to {explicit_height}", () =>
            {
                calculatedHeight = testDropdown.Menu.Height;
                testDropdown.Menu.MaxHeight = explicit_height;
            });
            AddAssert($"dropdown1 height is {explicit_height}", () => testDropdown.Menu.Height == explicit_height);

            AddStep($"Set dropdown1 height to {float.PositiveInfinity}", () => testDropdown.Menu.MaxHeight = float.PositiveInfinity);
            AddAssert("dropdown1 height is calculated automatically", () => testDropdown.Menu.Height == calculatedHeight);

            AddStep("click item 13", () =>
            {
                InputManager.MoveMouseTo(testDropdown.Menu.Children[13]);
                InputManager.Click(MouseButton.Left);
            });

            AddAssert("dropdown1 is closed", () => testDropdown.Menu.State == MenuState.Closed);
            AddAssert("item 13 is selected", () => testDropdown.Current.Value.Equals(testDropdown.Items.ElementAt(13)));

            AddStep("select item 15", () => testDropdown.Current.Value = testDropdown.Items.ElementAt(15));
            AddAssert("item 15 is selected", () => testDropdown.Current.Value.Equals(testDropdown.Items.ElementAt(15)));

            toggleDropdownViaClick(testDropdown, "dropdown1");
            AddAssert("dropdown1 is open", () => testDropdown.Menu.State == MenuState.Open);

            toggleDropdownViaClick(testDropdownMenu, "dropdown2");

            AddAssert("dropdown1 is closed", () => testDropdown.Menu.State == MenuState.Closed);
            AddAssert("dropdown2 is open", () => testDropdownMenu.Menu.State == MenuState.Open);

            AddStep("select 'invalid'", () => testDropdown.Current.Value = "invalid");

            AddAssert("'invalid' is selected", () => testDropdown.Current.Value.Identifier == "invalid");
            AddAssert("label shows 'invalid'", () => testDropdown.Header.Label.ToString() == "invalid");

            AddStep("select item 2", () => testDropdown.Current.Value = testDropdown.Items.ElementAt(2));
            AddAssert("item 2 is selected", () => testDropdown.Current.Value.Equals(testDropdown.Items.ElementAt(2)));

            AddStep("close dropdown", () => InputManager.Key(Key.Escape));
        }

        private void performPlatformAction(PlatformAction action, PlatformActionContainer platformActionContainer, Drawable drawable)
        {
            bool tempIsHovered = drawable.IsHovered;
            bool tempHasFocus = drawable.HasFocus;

            drawable.IsHovered = true;
            drawable.HasFocus = true;

            platformActionContainer.TriggerPressed(action);
            platformActionContainer.TriggerReleased(action);

            drawable.IsHovered = tempIsHovered;
            drawable.HasFocus = tempHasFocus;
        }

        [TestCase(false)]
        [TestCase(true)]
        public void TestKeyboardSelection(bool cleanSelection)
        {
            AddStep("Hover dropdown 1", () => InputManager.MoveMouseTo(testDropdown.Header));

            if (cleanSelection)
                AddStep("Clean selection", () => testDropdown.Current.Value = null);

            AddStep("Select next item", () =>
            {
                previousIndex = testDropdown.SelectedIndex;
                InputManager.Key(Key.Down);
            });
            AddAssert("Next item is selected", () => testDropdown.SelectedIndex == previousIndex + 1);

            AddStep("Select previous item", () =>
            {
                previousIndex = testDropdown.SelectedIndex;
                InputManager.Key(Key.Up);
            });
            AddAssert("Previous item is selected", () => testDropdown.SelectedIndex == Math.Max(0, previousIndex - 1));

            AddStep("Select last item",
                () => performPlatformAction(PlatformAction.MoveToListEnd, platformActionContainerKeyboardSelection, testDropdown.Header));
            AddAssert("Last item selected", () => testDropdown.SelectedItem == testDropdown.Menu.DrawableMenuItems.Last().Item);

            AddStep("Select first item",
                () => performPlatformAction(PlatformAction.MoveToListStart, platformActionContainerKeyboardSelection, testDropdown.Header));
            AddAssert("First item selected", () => testDropdown.SelectedItem == testDropdown.Menu.DrawableMenuItems.First().Item);

            AddStep("Select next item when empty", () => InputManager.Key(Key.Up));
            AddStep("Select previous item when empty", () => InputManager.Key(Key.Down));
            AddStep("Select last item when empty", () => InputManager.Key(Key.PageUp));
            AddStep("Select first item when empty", () => InputManager.Key(Key.PageDown));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void TestKeyboardPreselection(bool cleanSelection)
        {
            if (cleanSelection)
                AddStep("Clean selection", () => testDropdownMenu.Current.Value = null);

            toggleDropdownViaClick(testDropdownMenu);
            assertDropdownIsOpen(testDropdownMenu);

            AddStep("Preselect next item", () =>
            {
                previousIndex = testDropdownMenu.PreselectedIndex;
                InputManager.Key(Key.Down);
            });
            AddAssert("Next item is preselected", () => testDropdownMenu.PreselectedIndex == previousIndex + 1);

            AddStep("Preselect previous item", () =>
            {
                previousIndex = testDropdownMenu.PreselectedIndex;
                InputManager.Key(Key.Up);
            });
            AddAssert("Previous item is preselected", () => testDropdownMenu.PreselectedIndex == Math.Max(0, previousIndex - 1));

            AddStep("Preselect last visible item", () =>
            {
                lastVisibleIndexOnTheCurrentPage = testDropdownMenu.Menu.DrawableMenuItems.ToList().IndexOf(testDropdownMenu.Menu.VisibleMenuItems.Last());
                InputManager.Key(Key.PageDown);
            });
            AddAssert("Last visible item preselected", () => testDropdownMenu.PreselectedIndex == lastVisibleIndexOnTheCurrentPage);

            AddStep("Preselect last visible item on the next page", () =>
            {
                lastVisibleIndexOnTheNextPage =
                    Math.Clamp(lastVisibleIndexOnTheCurrentPage + testDropdownMenu.Menu.VisibleMenuItems.Count(), 0, testDropdownMenu.Menu.Items.Count - 1);

                InputManager.Key(Key.PageDown);
            });
            AddAssert("Last visible item on the next page preselected", () => testDropdownMenu.PreselectedIndex == lastVisibleIndexOnTheNextPage);

            AddStep("Preselect first visible item", () =>
            {
                firstVisibleIndexOnTheCurrentPage = testDropdownMenu.Menu.DrawableMenuItems.ToList().IndexOf(testDropdownMenu.Menu.VisibleMenuItems.First());
                InputManager.Key(Key.PageUp);
            });
            AddAssert("First visible item preselected", () => testDropdownMenu.PreselectedIndex == firstVisibleIndexOnTheCurrentPage);

            AddStep("Preselect first visible item on the previous page", () =>
            {
                firstVisibleIndexOnThePreviousPage = Math.Clamp(firstVisibleIndexOnTheCurrentPage - testDropdownMenu.Menu.VisibleMenuItems.Count(), 0,
                    testDropdownMenu.Menu.Items.Count - 1);
                InputManager.Key(Key.PageUp);
            });
            AddAssert("First visible item on the previous page selected", () => testDropdownMenu.PreselectedIndex == firstVisibleIndexOnThePreviousPage);
            AddAssert("First item is preselected", () => testDropdownMenu.Menu.PreselectedItem.Item == testDropdownMenu.Menu.DrawableMenuItems.First().Item);

            AddStep("Preselect last item",
                () => performPlatformAction(PlatformAction.MoveToListEnd, platformActionContainerKeyboardPreselection, testDropdownMenu));
            AddAssert("Last item preselected", () => testDropdownMenu.Menu.PreselectedItem.Item == testDropdownMenu.Menu.DrawableMenuItems.Last().Item);

            AddStep("Finalize selection", () => InputManager.Key(Key.Enter));
            assertLastItemSelected();
            assertDropdownIsClosed(testDropdownMenu);

            toggleDropdownViaClick(testDropdownMenu);
            assertDropdownIsOpen(testDropdownMenu);

            AddStep("Preselect first item",
                () => performPlatformAction(PlatformAction.MoveToListStart, platformActionContainerKeyboardPreselection, testDropdownMenu));
            AddAssert("First item preselected", () => testDropdownMenu.Menu.PreselectedItem.Item == testDropdownMenu.Menu.DrawableMenuItems.First().Item);

            AddStep("Discard preselection", () => InputManager.Key(Key.Escape));
            assertDropdownIsClosed(testDropdownMenu);
            assertLastItemSelected();

            toggleDropdownViaClick(emptyDropdown, "empty dropdown");
            AddStep("Preselect next item when empty", () => InputManager.Key(Key.Down));
            AddStep("Preselect previous item when empty", () => InputManager.Key(Key.Up));
            AddStep("Preselect first visible item when empty", () => InputManager.Key(Key.PageUp));
            AddStep("Preselect last visible item when empty", () => InputManager.Key(Key.PageDown));
            AddStep("Preselect first item when empty",
                () => performPlatformAction(PlatformAction.MoveToListStart, platformActionContainerEmptyDropdown, emptyDropdown));
            AddStep("Preselect last item when empty",
                () => performPlatformAction(PlatformAction.MoveToListEnd, platformActionContainerEmptyDropdown, emptyDropdown));

            void assertLastItemSelected() => AddAssert("Last item selected", () => testDropdownMenu.SelectedItem == testDropdownMenu.Menu.DrawableMenuItems.Last().Item);
        }

        [Test]
        public void TestSelectNull()
        {
            AddStep("select item 1", () => testDropdown.Current.Value = testDropdown.Items.ElementAt(1));
            AddAssert("item 1 is selected", () => testDropdown.Current.Value.Equals(testDropdown.Items.ElementAt(1)));
            AddStep("select item null", () => testDropdown.Current.Value = null);
            AddAssert("null is selected", () => testDropdown.Current.Value == null);
        }

        [Test]
        public void TestDisabledCurrent()
        {
            TestModel originalValue = null;

            AddStep("store original value", () => originalValue = disabledDropdown.Current.Value);

            toggleDropdownViaClick(disabledDropdown);
            assertDropdownIsClosed(disabledDropdown);

            AddStep("attempt to select next", () => InputManager.Key(Key.Down));
            valueIsUnchanged();

            AddStep("attempt to select previous", () => InputManager.Key(Key.Up));
            valueIsUnchanged();

            AddStep("attempt to select first", () => InputManager.Keys(PlatformAction.MoveToListStart));
            valueIsUnchanged();

            AddStep("attempt to select last", () => InputManager.Keys(PlatformAction.MoveToListEnd));
            valueIsUnchanged();

            AddStep("enable current", () => disabledDropdown.Current.Disabled = false);
            toggleDropdownViaClick(disabledDropdown);
            assertDropdownIsOpen(disabledDropdown);

            AddStep("disable current", () => disabledDropdown.Current.Disabled = true);
            assertDropdownIsClosed(disabledDropdown);

            void valueIsUnchanged() => AddAssert("value is unchanged", () => disabledDropdown.Current.Value.Equals(originalValue));
        }

        /// <summary>
        /// Basic test for a <see cref="Dropdown{T}"/> that has it's <see cref="Dropdown{T}.ItemSource"/> bound to a <see cref="BindableList{T}"/>.
        /// </summary>
        [Test]
        public void TestItemSource()
        {
            AddStep("clear bindable list", () => bindableList.Clear());
            toggleDropdownViaClick(bindableDropdown, "dropdown3");
            AddAssert("no elements in bindable dropdown", () => !bindableDropdown.Items.Any());

            AddStep("add items to bindable", () => bindableList.AddRange(new[] { "one", "two", "three" }.Select(s => new TestModel(s))));
            AddStep("select three", () => bindableDropdown.Current.Value = "three");
            AddStep("remove first item from bindable", () => bindableList.RemoveAt(0));
            AddAssert("two items in dropdown", () => bindableDropdown.Items.Count() == 2);
            AddAssert("current value still three", () => bindableDropdown.Current.Value.Identifier == "three");

            AddStep("remove three", () => bindableList.Remove("three"));
            AddAssert("current value should be two", () => bindableDropdown.Current.Value.Identifier == "two");

            AddStep("close dropdown", () => InputManager.Key(Key.Escape));
        }

        [Test]
        public void TestReplaceItemsInItemSource()
        {
            AddStep("clear bindable list", () => bindableList.Clear());
            toggleDropdownViaClick(bindableDropdown, "dropdown3");
            AddAssert("no elements in bindable dropdown", () => !bindableDropdown.Items.Any());

            AddStep("add items to bindable", () => bindableList.AddRange(new[] { "one", "two", "three" }.Select(s => new TestModel(s))));
            AddStep("select three", () => bindableDropdown.Current.Value = "three");

            AddStep("remove and then add items to bindable", () =>
            {
                bindableList.Clear();
                bindableList.AddRange(new[] { "four", "three" }.Select(s => new TestModel(s)));
            });

            AddAssert("current value still three", () => bindableDropdown.Current.Value.Identifier, () => Is.EqualTo("three"));
        }

        [Test]
        public void TestAccessBdlInGenerateItemText()
        {
            AddStep("add dropdown that uses BDL", () => Add(new BdlDropdown
            {
                Width = 150,
                Position = new Vector2(250, 350),
                Items = new TestModel("test").Yield()
            }));
        }

        private void toggleDropdownViaClick(TestDropdown dropdown, string dropdownName = null) => AddStep($"click {dropdownName ?? "dropdown"}", () =>
        {
            InputManager.MoveMouseTo(dropdown.Header);
            InputManager.Click(MouseButton.Left);
        });

        private void assertDropdownIsOpen(TestDropdown dropdown) => AddAssert("dropdown is open", () => dropdown.Menu.State == MenuState.Open);

        private void assertDropdownIsClosed(TestDropdown dropdown) => AddAssert("dropdown is closed", () => dropdown.Menu.State == MenuState.Closed);

        private class TestModel : IEquatable<TestModel>
        {
            public readonly string Identifier;

            public TestModel(string identifier)
            {
                Identifier = identifier;
            }

            public bool Equals(TestModel other)
            {
                if (other == null)
                    return false;

                return other.Identifier == Identifier;
            }

            public override string ToString() => Identifier;

            public static implicit operator TestModel(string str) => new TestModel(str);
        }

        private class TestDropdown : BasicDropdown<TestModel>
        {
            internal new DropdownMenuItem<TestModel> SelectedItem => base.SelectedItem;

            public int SelectedIndex => Menu.DrawableMenuItems.Select(d => d.Item).ToList().IndexOf(SelectedItem);
            public int PreselectedIndex => Menu.DrawableMenuItems.ToList().IndexOf(Menu.PreselectedItem);
        }

        /// <summary>
        /// Dropdown that will access <see cref="ResolvedAttribute"/> properties in <see cref="GenerateItemText"/>.
        /// </summary>
        private class BdlDropdown : TestDropdown
        {
            [Resolved]
            private GameHost host { get; set; }

            protected override LocalisableString GenerateItemText(TestModel item) => $"{host.Name}: {base.GenerateItemText(item)}";
        }
    }
}
