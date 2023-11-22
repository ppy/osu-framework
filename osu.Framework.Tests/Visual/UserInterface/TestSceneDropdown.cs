// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;
using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Extensions.IEnumerableExtensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Input;
using osu.Framework.Localisation;
using osu.Framework.Testing;
using osuTK;
using osuTK.Input;

namespace osu.Framework.Tests.Visual.UserInterface
{
    public partial class TestSceneDropdown : ManualInputManagerTestScene
    {
        private const int items_to_add = 10;
        private TestDropdown testDropdown = null!;

        [SetUp]
        public new void SetUp() => Schedule(() =>
        {
            testDropdown = setupDropdowns(1)[0];
        });

        [Test]
        public void TestSelectByUserInteraction()
        {
            toggleDropdownViaClick(() => testDropdown);
            assertDropdownIsOpen(() => testDropdown);

            AddStep("click item 2", () =>
            {
                InputManager.MoveMouseTo(testDropdown.Menu.Children[2]);
                InputManager.Click(MouseButton.Left);
            });

            assertDropdownIsClosed(() => testDropdown);
            AddAssert("item 2 is selected", () => testDropdown.Current.Value.Equals(testDropdown.Items.ElementAt(2)));
        }

        [Test]
        public void TestSelectByCurrent()
        {
            assertDropdownIsClosed(() => testDropdown);

            AddStep("update current to item 3", () => testDropdown.Current.Value = testDropdown.Items.ElementAt(3));
            AddAssert("item 3 is selected", () => testDropdown.Current.Value.Equals(testDropdown.Items.ElementAt(3)));
        }

        [Test]
        public void TestClickingDropdownClosesOthers()
        {
            TestDropdown[] dropdowns = null!;

            AddStep("create two dropdowns", () => dropdowns = setupDropdowns(2));

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

            AddStep("Select last item", () => InputManager.Keys(PlatformAction.MoveToListEnd));
            AddAssert("Last item selected", () => testDropdown.SelectedItem == testDropdown.Menu.DrawableMenuItems.Last().Item);

            AddStep("Select last item", () => InputManager.Keys(PlatformAction.MoveToListStart));
            AddAssert("First item selected", () => testDropdown.SelectedItem == testDropdown.Menu.DrawableMenuItems.First().Item);

            AddStep("Select next item when empty", () => InputManager.Key(Key.Up));
            AddStep("Select previous item when empty", () => InputManager.Key(Key.Down));
            AddStep("Select last item when empty", () => InputManager.Key(Key.PageUp));
            AddStep("Select first item when empty", () => InputManager.Key(Key.PageDown));
        }

        [Test]
        public void TestReplaceItems()
        {
            toggleDropdownViaClick(() => testDropdown);

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
        public void TestInvalidCurrent()
        {
            toggleDropdownViaClick(() => testDropdown);
            AddStep("select 'invalid'", () => testDropdown.Current.Value = "invalid");

            AddAssert("'invalid' is selected", () => testDropdown.Current.Value.Identifier == "invalid");
            AddAssert("label shows 'invalid'", () => testDropdown.Header.Label.ToString() == "invalid");

            AddStep("select item 2", () => testDropdown.Current.Value = testDropdown.Items.ElementAt(2));
            AddAssert("item 2 is selected", () => testDropdown.Current.Value.Equals(testDropdown.Items.ElementAt(2)));
        }

        [Test]
        public void TestNullCurrent()
        {
            AddStep("select item 1", () => testDropdown.Current.Value = testDropdown.Items.ElementAt(1));
            AddAssert("item 1 is selected", () => testDropdown.Current.Value.Equals(testDropdown.Items.ElementAt(1)));

            AddStep("select item null", () => testDropdown.Current.Value = null);
            AddAssert("null is selected", () => testDropdown.Current.Value == null);
            AddAssert("label shows nothing", () => string.IsNullOrEmpty(testDropdown.Header.Label.ToString()));
        }

        [Test]
        public void TestDisabledCurrent()
        {
            TestModel originalValue = null!;

            AddStep("disable current", () => testDropdown.Current.Disabled = true);
            AddStep("store original value", () => originalValue = testDropdown.Current.Value);

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

            void valueIsUnchanged() => AddAssert("value is unchanged", () => testDropdown.Current.Value.Equals(originalValue));
        }

        [Test]
        public void TestItemSource()
        {
            BindableList<TestModel> bindableList = null!;

            AddStep("bind source", () =>
            {
                // todo: perhaps binding ItemSource should clear existing items.
                testDropdown.ClearItems();
                testDropdown.ItemSource = bindableList = new BindableList<TestModel>();
            });

            toggleDropdownViaClick(() => testDropdown);

            AddAssert("no elements in dropdown", () => !testDropdown.Items.Any());

            AddStep("add items to bindable", () => bindableList.AddRange(new[] { "one", "two", "three" }.Select(s => new TestModel(s))));
            AddStep("select 'three'", () => testDropdown.Current.Value = "three");
            AddStep("remove 'one' from bindable", () => bindableList.RemoveAt(0));
            AddAssert("two items in dropdown", () => testDropdown.Items.Count() == 2);
            AddAssert("current value is still 'three'", () => testDropdown.Current.Value.Identifier == "three");

            AddStep("remove three", () => bindableList.Remove("three"));
            AddAssert("current value is 'two'", () => testDropdown.Current.Value.Identifier == "two");
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

            AddAssert("text is expected", () => dropdown.Menu.DrawableMenuItems.First().ChildrenOfType<SpriteText>().First().Text.ToString(), () => Is.EqualTo("loaded: test"));
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

            AddAssert("text is expected", () => dropdown.Menu.DrawableMenuItems.First(d => d.IsSelected).ChildrenOfType<SpriteText>().First().Text.ToString(), () => Is.EqualTo("loaded: test"));
        }

        /// <summary>
        /// Sets a non-existent item dropdown and ensures its label is assigned correctly.
        /// </summary>
        /// <param name="afterBdl">Whether the non-existent item should be set before or after the dropdown's BDL has run.</param>
        [Test]
        public void TestSetNonExistentItem([Values] bool afterBdl)
        {
            BdlDropdown dropdown = null!;
            BindableList<TestModel> bindableList = null!;

            AddStep("setup bindables", () =>
            {
                bindableList = new BindableList<TestModel>();
                bindableList.AddRange(new[] { "one", "two", "three" }.Select(s => new TestModel(s)));
            });

            AddStep("setup dropdown", () =>
            {
                var bindable = new Bindable<TestModel>();

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

        private TestDropdown[] setupDropdowns(int count)
        {
            TestDropdown[] dropdowns = new TestDropdown[count];

            for (int i = 0; i < count; i++)
            {
                var testItems = new TestModel[10];
                for (int i1 = 0; i1 < items_to_add; i1++)
                    testItems[i1] = "test " + i1;

                dropdowns[i] = new TestDropdown
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

        private partial class TestDropdown : BasicDropdown<TestModel>
        {
            internal new DropdownMenuItem<TestModel> SelectedItem => base.SelectedItem;

            public int SelectedIndex => Menu.DrawableMenuItems.Select(d => d.Item).ToList().IndexOf(SelectedItem);
            public int PreselectedIndex => Menu.DrawableMenuItems.ToList().IndexOf(Menu.PreselectedItem);
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

            protected override LocalisableString GenerateItemText(TestModel item)
            {
                Assert.That(text, Is.Not.Null);
                return $"{text}: {base.GenerateItemText(item)}";
            }
        }
    }
}
