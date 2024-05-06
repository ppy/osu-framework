// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Extensions.IEnumerableExtensions;
using osu.Framework.Extensions.ObjectExtensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Input;
using osu.Framework.Input.Bindings;
using osu.Framework.Input.Events;
using osu.Framework.Localisation;
using osu.Framework.Testing;
using osu.Framework.Testing.Input;
using osuTK;
using osuTK.Input;

namespace osu.Framework.Tests.Visual.UserInterface
{
    public partial class TestSceneDropdown : ManualInputManagerTestScene
    {
        private const int items_to_add = 10;

        [Test]
        public void TestBasic()
        {
            AddStep("setup dropdowns", () =>
            {
                TestDropdown[] dropdowns = createDropdowns(2);
                dropdowns[1].AlwaysShowSearchBar = true;
            });
        }

        [Test]
        public void TestSelectByUserInteraction()
        {
            TestDropdown testDropdown = null!;

            AddStep("setup dropdown", () => testDropdown = createDropdown());

            toggleDropdownViaClick(() => testDropdown);
            assertDropdownIsOpen(() => testDropdown);

            AddStep("click item 2", () =>
            {
                InputManager.MoveMouseTo(testDropdown.Menu.Children[2]);
                InputManager.Click(MouseButton.Left);
            });

            assertDropdownIsClosed(() => testDropdown);

            AddAssert("item 2 is selected", () => testDropdown.Current.Value?.Equals(testDropdown.Items.ElementAt(2)) == true);
            AddAssert("item 2 is selected item", () => testDropdown.SelectedItem.Value?.Identifier == "test 2");
            AddAssert("item 2 is visually selected", () => (testDropdown.ChildrenOfType<Dropdown<TestModel?>.DropdownMenu.DrawableDropdownMenuItem>()
                                                                        .SingleOrDefault(i => i.IsSelected)?
                                                                        .Item as DropdownMenuItem<TestModel?>)?.Value?.Identifier == "test 2");
        }

        [Test]
        public void TestSelectByCurrent()
        {
            TestDropdown testDropdown = null!;

            AddStep("setup dropdown", () => testDropdown = createDropdown());

            assertDropdownIsClosed(() => testDropdown);

            AddStep("update current to item 3", () => testDropdown.Current.Value = testDropdown.Items.ElementAt(3));

            AddAssert("item 3 is selected", () => testDropdown.Current.Value?.Equals(testDropdown.Items.ElementAt(3)) == true);
            AddAssert("item 3 is selected item", () => testDropdown.SelectedItem.Value?.Identifier == "test 3");
            AddAssert("item 3 is visually selected", () => (testDropdown.ChildrenOfType<Dropdown<TestModel?>.DropdownMenu.DrawableDropdownMenuItem>()
                                                                        .SingleOrDefault(i => i.IsSelected)?
                                                                        .Item as DropdownMenuItem<TestModel?>)?.Value?.Identifier == "test 3");
        }

        [Test]
        public void TestClickingDropdownClosesOthers()
        {
            TestDropdown[] dropdowns = null!;

            AddStep("create two dropdowns", () => dropdowns = createDropdowns(2));

            toggleDropdownViaClick(() => dropdowns[0], "dropdown 1");
            AddAssert("dropdown 1 is open", () => dropdowns[0].Menu.State == MenuState.Open);

            toggleDropdownViaClick(() => dropdowns[1], "dropdown 2");
            AddAssert("dropdown 1 is closed", () => dropdowns[0].Menu.State == MenuState.Closed);
            AddAssert("dropdown 2 is open", () => dropdowns[1].Menu.State == MenuState.Open);
        }

        [Test]
        public void TestDropdownHeight()
        {
            const float explicit_height = 100;
            float calculatedHeight = 0;

            TestDropdown testDropdown = null!;

            AddStep("setup dropdown", () => testDropdown = createDropdown());

            toggleDropdownViaClick(() => testDropdown);

            AddStep("add items", () =>
            {
                for (int i = 0; i < 10; i++)
                    testDropdown.AddDropdownItem("test " + (items_to_add + i));
            });

            AddAssert("item count is correct", () => testDropdown.Items.Count() == items_to_add * 2);

            AddStep($"Set dropdown1 height to {explicit_height}", () =>
            {
                calculatedHeight = testDropdown.Menu.Height;
                testDropdown.Menu.MaxHeight = explicit_height;
            });
            AddAssert($"dropdown1 height is {explicit_height}", () => testDropdown.Menu.Height == explicit_height);

            AddStep($"Set dropdown1 height to {float.PositiveInfinity}", () => testDropdown.Menu.MaxHeight = float.PositiveInfinity);
            AddAssert("dropdown1 height is calculated automatically", () => testDropdown.Menu.Height == calculatedHeight);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void TestKeyboardSelection(bool cleanSelection)
        {
            int previousIndex = 0;

            TestDropdown testDropdown = null!;

            AddStep("setup dropdown", () => testDropdown = createDropdown());

            AddStep("hover dropdown", () => InputManager.MoveMouseTo(testDropdown.Header));

            if (cleanSelection)
                AddStep("clean selection", () => testDropdown.Current.Value = null);

            AddStep("select next item", () =>
            {
                previousIndex = testDropdown.SelectedIndex;
                InputManager.Key(Key.Down);
            });
            AddAssert("next item is selected", () => testDropdown.SelectedIndex == previousIndex + 1);

            AddStep("select previous item", () =>
            {
                previousIndex = testDropdown.SelectedIndex;
                InputManager.Key(Key.Up);
            });
            AddAssert("previous item is selected", () => testDropdown.SelectedIndex == Math.Max(0, previousIndex - 1));

            AddStep("select last item", () => InputManager.Keys(PlatformAction.MoveToListEnd));
            AddAssert("last item selected", () => testDropdown.SelectedItem == testDropdown.Menu.VisibleMenuItems.Last().Item);

            AddStep("select last item", () => InputManager.Keys(PlatformAction.MoveToListStart));
            AddAssert("first item selected", () => testDropdown.SelectedItem == testDropdown.Menu.VisibleMenuItems.First().Item);

            AddStep("select next item when empty", () => InputManager.Key(Key.Up));
            AddStep("select previous item when empty", () => InputManager.Key(Key.Down));
            AddStep("select last item when empty", () => InputManager.Key(Key.PageUp));
            AddStep("select first item when empty", () => InputManager.Key(Key.PageDown));
        }

        [Test]
        public void TestReplaceItems()
        {
            TestDropdown testDropdown = null!;

            AddStep("setup dropdown", () => testDropdown = createDropdown());

            toggleDropdownViaClick(() => testDropdown);

            AddStep("click item 4", () =>
            {
                InputManager.MoveMouseTo(testDropdown.Menu.Children[4]);
                InputManager.Click(MouseButton.Left);
            });

            AddAssert("item 4 is selected", () => testDropdown.Current.Value?.Identifier == "test 4");

            AddStep("replace items", () =>
            {
                testDropdown.Items = testDropdown.Items.Select(i => new TestModel(i.AsNonNull().ToString())).ToArray();
            });

            AddAssert("item 4 is selected", () => testDropdown.Current.Value?.Identifier == "test 4");
            AddAssert("item 4 is selected item", () => testDropdown.SelectedItem.Value?.Identifier == "test 4");
            AddAssert("item 4 is visually selected", () => (testDropdown.ChildrenOfType<Dropdown<TestModel?>.DropdownMenu.DrawableDropdownMenuItem>()
                                                                        .SingleOrDefault(i => i.IsSelected)?
                                                                        .Item as DropdownMenuItem<TestModel?>)?.Value?.Identifier == "test 4");
        }

        [Test]
        public void TestInvalidCurrent()
        {
            TestDropdown testDropdown = null!;

            AddStep("setup dropdown", () => testDropdown = createDropdown());

            toggleDropdownViaClick(() => testDropdown);
            AddStep("select 'invalid'", () => testDropdown.Current.Value = "invalid");

            AddAssert("'invalid' is selected", () => testDropdown.Current.Value?.Identifier == "invalid");
            AddAssert("label shows 'invalid'", () => testDropdown.Header.Label.ToString() == "invalid");

            AddStep("select item 2", () => testDropdown.Current.Value = testDropdown.Items.ElementAt(2));
            AddAssert("item 2 is selected", () => testDropdown.Current.Value?.Equals(testDropdown.Items.ElementAt(2)) == true);
        }

        [Test]
        public void TestNullCurrent()
        {
            TestDropdown testDropdown = null!;

            AddStep("setup dropdown", () => testDropdown = createDropdown());

            AddStep("select item 1", () => testDropdown.Current.Value = testDropdown.Items.ElementAt(1).AsNonNull());
            AddAssert("item 1 is selected", () => testDropdown.Current.Value?.Equals(testDropdown.Items.ElementAt(1)) == true);

            AddStep("select item null", () => testDropdown.Current.Value = null);
            AddAssert("null is selected", () => testDropdown.Current.Value == null);
            AddAssert("label shows nothing", () => string.IsNullOrEmpty(testDropdown.Header.Label.ToString()));
        }

        [Test]
        public void TestDisabledCurrent()
        {
            TestDropdown testDropdown = null!;
            TestModel originalValue = null!;

            AddStep("setup dropdown", () => testDropdown = createDropdown());

            AddStep("disable current", () => testDropdown.Current.Disabled = true);
            AddStep("store original value", () => originalValue = testDropdown.Current.Value.AsNonNull());

            toggleDropdownViaClick(() => testDropdown);
            assertDropdownIsClosed(() => testDropdown);

            AddStep("attempt to select next", () => InputManager.Key(Key.Down));
            valueIsUnchanged();

            AddStep("attempt to select previous", () => InputManager.Key(Key.Up));
            valueIsUnchanged();

            AddStep("attempt to select first", () => InputManager.Keys(PlatformAction.MoveToListStart));
            valueIsUnchanged();

            AddStep("attempt to select last", () => InputManager.Keys(PlatformAction.MoveToListEnd));
            valueIsUnchanged();

            AddStep("enable current", () => testDropdown.Current.Disabled = false);
            toggleDropdownViaClick(() => testDropdown);
            assertDropdownIsOpen(() => testDropdown);

            AddStep("disable current", () => testDropdown.Current.Disabled = true);
            assertDropdownIsClosed(() => testDropdown);

            void valueIsUnchanged() => AddAssert("value is unchanged", () => testDropdown.Current.Value?.Equals(originalValue) == true);
        }

        [Test]
        public void TestItemSource()
        {
            TestDropdown testDropdown = null!;
            BindableList<TestModel?> bindableList = null!;

            AddStep("setup dropdown", () => testDropdown = createDropdown());

            AddStep("bind source", () => testDropdown.ItemSource = bindableList = new BindableList<TestModel?>());

            toggleDropdownViaClick(() => testDropdown);

            AddAssert("no elements in dropdown", () => !testDropdown.Items.Any());

            AddStep("add items to bindable", () => bindableList.AddRange(new[] { "one", "2", "three" }.Select(s => new TestModel(s))));
            AddStep("select 'three'", () => testDropdown.Current.Value = "three");

            AddStep("replace '2' with 'two'", () => bindableList.ReplaceRange(1, 1, new TestModel[] { "two" }));
            checkOrder(1, "two");

            AddStep("remove 'one' from bindable", () => bindableList.RemoveAt(0));
            AddAssert("two items in dropdown", () => testDropdown.Items.Count() == 2);
            AddAssert("current value is still 'three'", () => testDropdown.Current.Value?.Identifier == "three");

            AddStep("remove 'three'", () => bindableList.Remove("three"));
            AddAssert("current value is 'two'", () => testDropdown.Current.Value?.Identifier == "two");

            AddStep("add 'one' and 'three'", () =>
            {
                bindableList.Insert(0, "one");
                bindableList.Add("three");
            });

            checkOrder(0, "one");
            checkOrder(2, "three");

            AddStep("add 'one-half'", () => bindableList.Add("one-half"));
            AddStep("move 'one-half'", () => bindableList.Move(3, 1));
            checkOrder(1, "one-half");
            checkOrder(2, "two");

            void checkOrder(int index, string item) => AddAssert($"item #{index + 1} is '{item}'",
                () => testDropdown.ChildrenOfType<FillFlowContainer<Menu.DrawableMenuItem>>().Single().FlowingChildren.Cast<Menu.DrawableMenuItem>().ElementAt(index).Item.Text.Value.ToString(),
                () => Is.EqualTo(item));
        }

        [Test]
        public void TestItemReplacementDoesNotAffectScroll()
        {
            TestDropdown testDropdown = null!;
            BindableList<TestModel?> bindableList = null!;

            AddStep("setup dropdown", () => testDropdown = createDropdown());

            AddStep("bind source", () => testDropdown.ItemSource = bindableList = new BindableList<TestModel?>());
            AddStep("add many items", () => bindableList.AddRange(Enumerable.Range(0, 20).Select(i => (TestModel)$"test {i}")));
            AddStep("set max height", () => testDropdown.Menu.MaxHeight = 100);

            toggleDropdownViaClick(() => testDropdown);

            AddStep("scroll to middle", () => testDropdown.ChildrenOfType<BasicScrollContainer>().Single().ScrollTo(200));
            AddStep("replace item in middle", () => bindableList.ReplaceRange(10, 1, new TestModel[] { "test ten" }));
            AddAssert("scroll is unchanged", () => testDropdown.ChildrenOfType<BasicScrollContainer>().Single().Target == 200);
        }

        [Test]
        public void TestClearItemsInBindableWhileNotPresent()
        {
            TestDropdown testDropdown = null!;
            BindableList<TestModel?> bindableList = null!;

            AddStep("setup dropdown", () => testDropdown = createDropdown());

            AddStep("bind source", () => testDropdown.ItemSource = bindableList = new BindableList<TestModel?>());
            AddStep("add many items", () => bindableList.AddRange(Enumerable.Range(0, 20).Select(i => (TestModel)$"test {i}")));

            AddStep("hide dropdown", () => testDropdown.Hide());
            AddStep("clear items", () => bindableList.Clear());
            AddStep("show dropdown", () => testDropdown.Show());
            AddAssert("dropdown menu empty", () => !testDropdown.Menu.Children.Any());
        }

        /// <summary>
        /// Adds an item before a dropdown is loaded, and ensures item labels are assigned correctly.
        /// </summary>
        /// <remarks>
        /// Ensures item labels are assigned after the dropdown finishes loading (reaches <see cref="LoadState.Ready"/> state),
        /// so any dependency from BDL can be retrieved first before calling <see cref="Dropdown{T}.GenerateItemText"/>.
        /// </remarks>
        [Test]
        public void TestAddItemBeforeDropdownLoad()
        {
            BdlDropdown dropdown = null!;

            AddStep("setup dropdown", () =>
            {
                Child = dropdown = new BdlDropdown
                {
                    Position = new Vector2(50f, 50f),
                    Width = 150f,
                    Items = new TestModel("test").Yield(),
                };
            });

            AddAssert("text is expected", () => dropdown.Menu.VisibleMenuItems.First().ChildrenOfType<SpriteText>().First().Text.ToString(), () => Is.EqualTo("loaded: test"));
        }

        /// <summary>
        /// Adds an item after the dropdown is in <see cref="LoadState.Ready"/> state, and ensures item labels are assigned correctly and not ignored by <see cref="Dropdown{T}"/>.
        /// </summary>
        [Test]
        public void TestAddItemWhileDropdownIsInReadyState()
        {
            BdlDropdown dropdown = null!;

            AddStep("setup dropdown", () =>
            {
                Child = dropdown = new BdlDropdown
                {
                    Position = new Vector2(50f, 50f),
                    Width = 150f,
                };

                dropdown.Items = new TestModel("test").Yield();
            });

            AddAssert("text is expected", () => dropdown.Menu.VisibleMenuItems.First(d => d.IsSelected).ChildrenOfType<SpriteText>().First().Text.ToString(), () => Is.EqualTo("loaded: test"));
        }

        /// <summary>
        /// Sets a non-existent item dropdown and ensures its label is assigned correctly.
        /// </summary>
        /// <param name="afterBdl">Whether the non-existent item should be set before or after the dropdown's BDL has run.</param>
        [Test]
        public void TestSetNonExistentItem([Values] bool afterBdl)
        {
            BdlDropdown dropdown = null!;
            BindableList<TestModel?> bindableList = null!;

            AddStep("setup bindables", () =>
            {
                bindableList = new BindableList<TestModel?>();
                bindableList.AddRange(new[] { "one", "two", "three" }.Select(s => new TestModel(s)));
            });

            AddStep("setup dropdown", () =>
            {
                var bindable = new Bindable<TestModel?>();

                if (!afterBdl)
                    bindable.Value = new TestModel("non-existent item");

                Child = dropdown = new BdlDropdown
                {
                    Position = new Vector2(50f, 50f),
                    Width = 150f,
                    ItemSource = bindableList,
                    Current = bindable,
                };

                if (afterBdl)
                    bindable.Value = new TestModel("non-existent item");
            });

            AddAssert("text is expected", () => dropdown.SelectedItem.Text.Value.ToString(), () => Is.EqualTo("loaded: non-existent item"));
        }

        #region Searching

        [Test]
        public void TestSearching()
        {
            ManualTextDropdown dropdown = null!;

            AddStep("setup dropdown", () => dropdown = createDropdowns<ManualTextDropdown>(1)[0]);
            AddAssert("search bar hidden", () => dropdown.ChildrenOfType<DropdownSearchBar>().Single().State.Value, () => Is.EqualTo(Visibility.Hidden));

            toggleDropdownViaClick(() => dropdown);

            AddAssert("search bar still hidden", () => dropdown.ChildrenOfType<DropdownSearchBar>().Single().State.Value, () => Is.EqualTo(Visibility.Hidden));

            AddStep("trigger text", () => dropdown.TextInput.Text("test 4"));
            AddAssert("search bar visible", () => dropdown.ChildrenOfType<DropdownSearchBar>().Single().State.Value, () => Is.EqualTo(Visibility.Visible));
            AddAssert("items filtered", () =>
            {
                var drawableItem = dropdown.Menu.VisibleMenuItems.Single(i => i.IsPresent);
                return drawableItem.Item.Text.Value == "test 4";
            });
            AddAssert("item preselected", () => dropdown.Menu.VisibleMenuItems.Single().IsPreSelected);

            AddStep("press enter", () => InputManager.Key(Key.Enter));
            AddAssert("item selected", () => dropdown.SelectedItem.Text.Value == "test 4");
        }

        [Test]
        public void TestReleaseFocusAfterSearching()
        {
            ManualTextDropdown dropdown = null!;

            AddStep("setup dropdown", () => dropdown = createDropdowns<ManualTextDropdown>(1)[0]);
            toggleDropdownViaClick(() => dropdown);

            AddStep("trigger text", () => dropdown.TextInput.Text("test 4"));
            AddAssert("search bar visible", () => dropdown.ChildrenOfType<DropdownSearchBar>().Single().State.Value, () => Is.EqualTo(Visibility.Visible));

            AddStep("press escape", () => InputManager.Key(Key.Escape));
            AddAssert("search bar hidden", () => dropdown.ChildrenOfType<DropdownSearchBar>().Single().State.Value, () => Is.EqualTo(Visibility.Hidden));
            AddAssert("dropdown still open", () => dropdown.Menu.State == MenuState.Open);

            AddStep("press escape again", () => InputManager.Key(Key.Escape));
            AddAssert("dropdown closed", () => dropdown.Menu.State == MenuState.Closed);

            toggleDropdownViaClick(() => dropdown);
            AddStep("trigger text", () => dropdown.TextInput.Text("test 4"));
            AddAssert("search bar visible", () => dropdown.ChildrenOfType<DropdownSearchBar>().Single().State.Value, () => Is.EqualTo(Visibility.Visible));

            AddStep("click away", () =>
            {
                InputManager.MoveMouseTo(Vector2.Zero);
                InputManager.Click(MouseButton.Left);
            });

            AddAssert("search bar hidden", () => dropdown.ChildrenOfType<DropdownSearchBar>().Single().State.Value, () => Is.EqualTo(Visibility.Hidden));
        }

        [Test]
        public void TestAlwaysShowSearchBar()
        {
            ManualTextDropdown dropdown = null!;

            AddStep("setup dropdown", () =>
            {
                dropdown = createDropdowns<ManualTextDropdown>(1)[0];
                dropdown.AlwaysShowSearchBar = true;
            });

            AddAssert("search bar hidden", () => dropdown.ChildrenOfType<DropdownSearchBar>().Single().State.Value, () => Is.EqualTo(Visibility.Hidden));
            toggleDropdownViaClick(() => dropdown);

            AddAssert("search bar visible", () => dropdown.ChildrenOfType<DropdownSearchBar>().Single().State.Value, () => Is.EqualTo(Visibility.Visible));

            AddStep("trigger text", () => dropdown.TextInput.Text("test 4"));
            AddAssert("search bar still visible", () => dropdown.ChildrenOfType<DropdownSearchBar>().Single().State.Value, () => Is.EqualTo(Visibility.Visible));

            AddStep("press escape", () => InputManager.Key(Key.Escape));
            AddAssert("search bar still visible", () => dropdown.ChildrenOfType<DropdownSearchBar>().Single().State.Value, () => Is.EqualTo(Visibility.Visible));
            AddAssert("dropdown still open", () => dropdown.Menu.State == MenuState.Open);

            AddStep("press escape again", () => InputManager.Key(Key.Escape));
            AddAssert("dropdown closed", () => dropdown.Menu.State == MenuState.Closed);
            AddAssert("search bar hidden", () => dropdown.ChildrenOfType<DropdownSearchBar>().Single().State.Value, () => Is.EqualTo(Visibility.Hidden));

            toggleDropdownViaClick(() => dropdown);
            AddStep("trigger text", () => dropdown.TextInput.Text("test 4"));
            AddAssert("search bar visible", () => dropdown.ChildrenOfType<DropdownSearchBar>().Single().State.Value, () => Is.EqualTo(Visibility.Visible));

            AddStep("click away", () =>
            {
                InputManager.MoveMouseTo(Vector2.Zero);
                InputManager.Click(MouseButton.Left);
            });

            AddAssert("search bar hidden", () => dropdown.ChildrenOfType<DropdownSearchBar>().Single().State.Value, () => Is.EqualTo(Visibility.Hidden));
        }

        [Test]
        public void TestKeyBindingIsolation()
        {
            ManualTextDropdown dropdown = null!;
            TestKeyBindingHandler keyBindingHandler = null!;

            AddStep("setup dropdown", () =>
            {
                dropdown = createDropdowns<ManualTextDropdown>(1)[0];
                dropdown.AlwaysShowSearchBar = true;
            });

            AddStep("setup key binding handler", () =>
            {
                Add(new TestKeyBindingContainer
                {
                    RelativeSizeAxes = Axes.Both,
                    Child = keyBindingHandler = new TestKeyBindingHandler
                    {
                        RelativeSizeAxes = Axes.Both,
                    },
                });
            });

            AddAssert("search bar hidden", () => dropdown.ChildrenOfType<DropdownSearchBar>().Single().State.Value, () => Is.EqualTo(Visibility.Hidden));
            toggleDropdownViaClick(() => dropdown);

            AddStep("press space", () =>
            {
                InputManager.Key(Key.Space);

                // we must send something via the text input path for TextBox to block the space key press above,
                // we're not supposed to do this here, but we don't have a good way of simulating text input from ManualInputManager so let's just do this for now.
                // todo: add support for simulating text typing at a ManualInputManager level for more realistic results.
                dropdown.TextInput.Text(" ");
            });
            AddAssert("handler did not receive press", () => !keyBindingHandler.ReceivedPress);

            toggleDropdownViaClick(() => dropdown);

            AddStep("press space", () =>
            {
                InputManager.Key(Key.Space);

                // we must send something via the text input path for TextBox to block the space key press above,
                // we're not supposed to do this here, but we don't have a good way of simulating text input from ManualInputManager so let's just do this for now.
                dropdown.TextInput.Text(" ");
            });
            AddAssert("handler received press", () => keyBindingHandler.ReceivedPress);
        }

        [Test]
        public void TestMouseFromTouch()
        {
            ManualTextDropdown dropdown = null!;
            TestClickHandler clickHandler = null!;

            AddStep("setup dropdown", () =>
            {
                dropdown = createDropdowns<ManualTextDropdown>(1)[0];
                dropdown.AlwaysShowSearchBar = true;
            });

            AddStep("setup click handler", () => Add(clickHandler = new TestClickHandler
            {
                RelativeSizeAxes = Axes.Both
            }));

            AddAssert("search bar hidden", () => dropdown.ChildrenOfType<DropdownSearchBar>().Single().State.Value, () => Is.EqualTo(Visibility.Hidden));
            AddStep("begin touch", () => InputManager.BeginTouch(new Touch(TouchSource.Touch1, dropdown.Header.ScreenSpaceDrawQuad.Centre)));
            AddStep("end touch", () => InputManager.EndTouch(new Touch(TouchSource.Touch1, dropdown.Header.ScreenSpaceDrawQuad.Centre)));

            AddAssert("search bar still hidden", () => dropdown.ChildrenOfType<DropdownSearchBar>().Single().State.Value, () => Is.EqualTo(Visibility.Hidden));
            AddAssert("handler received click", () => clickHandler.ReceivedClick);

            AddStep("type something", () => dropdown.TextInput.Text("something"));
            AddAssert("search bar still hidden", () => dropdown.ChildrenOfType<DropdownSearchBar>().Single().State.Value, () => Is.EqualTo(Visibility.Hidden));
            AddAssert("search bar empty", () => dropdown.Header.SearchTerm.Value, () => Is.Null.Or.Empty);

            AddStep("hide click handler", () => clickHandler.Hide());
            AddStep("begin touch", () => InputManager.BeginTouch(new Touch(TouchSource.Touch1, dropdown.Header.ScreenSpaceDrawQuad.Centre)));
            AddStep("end touch", () => InputManager.EndTouch(new Touch(TouchSource.Touch1, dropdown.Header.ScreenSpaceDrawQuad.Centre)));

            AddAssert("search bar visible", () => dropdown.ChildrenOfType<DropdownSearchBar>().Single().State.Value, () => Is.EqualTo(Visibility.Visible));
        }

        #endregion

        private TestDropdown createDropdown() => createDropdowns(1).Single();

        private TestDropdown[] createDropdowns(int count) => createDropdowns<TestDropdown>(count);

        private TDropdown[] createDropdowns<TDropdown>(int count)
            where TDropdown : TestDropdown, new()
        {
            TDropdown[] dropdowns = new TDropdown[count];

            for (int dropdownIndex = 0; dropdownIndex < count; dropdownIndex++)
            {
                var testItems = new TestModel[10];
                for (int itemIndex = 0; itemIndex < items_to_add; itemIndex++)
                    testItems[itemIndex] = "test " + itemIndex;

                dropdowns[dropdownIndex] = new TDropdown
                {
                    Position = new Vector2(50f, 50f),
                    Width = 150,
                    Items = testItems,
                };
            }

            Child = new FillFlowContainer
            {
                RelativeSizeAxes = Axes.Both,
                Padding = new MarginPadding(50),
                Spacing = new Vector2(20f, 20f),
                Direction = FillDirection.Horizontal,
                Children = dropdowns,
            };

            return dropdowns;
        }

        private void toggleDropdownViaClick(Func<TestDropdown> dropdown, string? dropdownName = null) => AddStep($"click {dropdownName ?? "dropdown"}", () =>
        {
            InputManager.MoveMouseTo(dropdown().Header);
            InputManager.Click(MouseButton.Left);
        });

        private void assertDropdownIsOpen(Func<TestDropdown> dropdown) => AddAssert("dropdown is open", () => dropdown().Menu.State == MenuState.Open);

        private void assertDropdownIsClosed(Func<TestDropdown> dropdown) => AddAssert("dropdown is closed", () => dropdown().Menu.State == MenuState.Closed);

        private class TestModel : IEquatable<TestModel>
        {
            public readonly string Identifier;

            public TestModel(string identifier)
            {
                Identifier = identifier;
            }

            public bool Equals(TestModel? other)
            {
                if (other == null)
                    return false;

                return other.Identifier == Identifier;
            }

            public override int GetHashCode() => Identifier.GetHashCode();

            public override string ToString() => Identifier;

            public static implicit operator TestModel(string str) => new TestModel(str);
        }

        private partial class TestDropdown : BasicDropdown<TestModel?>
        {
            internal new DropdownMenuItem<TestModel?> SelectedItem => base.SelectedItem;

            public int SelectedIndex => Menu.VisibleMenuItems.Select(d => d.Item).ToList().IndexOf(SelectedItem);
            public int PreselectedIndex => Menu.VisibleMenuItems.ToList().IndexOf(Menu.PreselectedItem);
        }

        private partial class ManualTextDropdown : TestDropdown
        {
            [Cached(typeof(TextInputSource))]
            public readonly ManualTextInputSource TextInput = new ManualTextInputSource();
        }

        /// <summary>
        /// Dropdown that will access state set by BDL load in <see cref="GenerateItemText"/>.
        /// </summary>
        private partial class BdlDropdown : TestDropdown
        {
            private string text = null!;

            [BackgroundDependencyLoader]
            private void load()
            {
                text = "loaded";
            }

            protected override LocalisableString GenerateItemText(TestModel? item)
            {
                Assert.That(text, Is.Not.Null);
                return $"{text}: {base.GenerateItemText(item)}";
            }
        }

        private partial class TestKeyBindingContainer : KeyBindingContainer<TestAction>
        {
            public override IEnumerable<IKeyBinding> DefaultKeyBindings => new[]
            {
                new KeyBinding(InputKey.Space, TestAction.SpaceAction)
            };
        }

        private partial class TestKeyBindingHandler : Drawable, IKeyBindingHandler<TestAction>
        {
            public bool ReceivedPress;

            public bool OnPressed(KeyBindingPressEvent<TestAction> e)
            {
                ReceivedPress = true;
                return true;
            }

            public void OnReleased(KeyBindingReleaseEvent<TestAction> e)
            {
            }
        }

        private partial class TestClickHandler : Drawable
        {
            public bool ReceivedClick;

            protected override bool OnClick(ClickEvent e)
            {
                ReceivedClick = true;
                return true;
            }
        }

        private enum TestAction
        {
            SpaceAction,
        }
    }
}
