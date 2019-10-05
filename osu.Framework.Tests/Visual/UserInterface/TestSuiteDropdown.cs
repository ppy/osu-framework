// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Testing;
using osuTK.Input;

namespace osu.Framework.Tests.Visual.UserInterface
{
    public class TestSuiteDropdown : ManualInputManagerTestSuite<TestSceneDropdown>
    {
        private const int items_to_add = 10;
        private const float explicit_height = 100;
        private float calculatedHeight;

        public override IReadOnlyList<Type> RequiredTypes => new[]
        {
            typeof(Dropdown<>),
            typeof(DropdownHeader),
            typeof(DropdownMenuItem<>),
            typeof(Dropdown<>),
            typeof(BasicDropdown<>),
            typeof(BasicDropdown<>.BasicDropdownHeader),
            typeof(BasicDropdown<>.BasicDropdownMenu),
            typeof(TestSceneDropdown.TestDropdown)
        };

        public TestSuiteDropdown()
        {
            int i = 0;
            while (i < items_to_add)
                TestScene.TestItems[i] = @"test " + i++;
        }

        [Test]
        public void Basic()
        {
            var i = items_to_add;

            AddStep("click dropdown1", () => toggleDropdownViaClick(TestScene.Dropdown));
            AddAssert("dropdown is open", () => TestScene.Dropdown.Menu.State == MenuState.Open);

            AddRepeatStep("add item", () => TestScene.Dropdown.AddDropdownItem("test " + i++), items_to_add);
            AddAssert("item count is correct", () => TestScene.Dropdown.Items.Count() == items_to_add * 2);

            AddStep($"Set dropdown1 height to {explicit_height}", () =>
            {
                calculatedHeight = TestScene.Dropdown.Menu.Height;
                TestScene.Dropdown.Menu.MaxHeight = explicit_height;
            });
            AddAssert($"dropdown1 height is {explicit_height}", () => TestScene.Dropdown.Menu.Height == explicit_height);

            AddStep($"Set dropdown1 height to {float.PositiveInfinity}", () => TestScene.Dropdown.Menu.MaxHeight = float.PositiveInfinity);
            AddAssert("dropdown1 height is calculated automatically", () => TestScene.Dropdown.Menu.Height == calculatedHeight);

            AddStep("click item 13", () => TestScene.Dropdown.SelectItem(TestScene.Dropdown.Menu.Items[13]));

            AddAssert("dropdown1 is closed", () => TestScene.Dropdown.Menu.State == MenuState.Closed);
            AddAssert("item 13 is selected", () => TestScene.Dropdown.Current.Value == TestScene.Dropdown.Items.ElementAt(13));

            AddStep("select item 15", () => TestScene.Dropdown.Current.Value = TestScene.Dropdown.Items.ElementAt(15));
            AddAssert("item 15 is selected", () => TestScene.Dropdown.Current.Value == TestScene.Dropdown.Items.ElementAt(15));

            AddStep("click dropdown1", () => toggleDropdownViaClick(TestScene.Dropdown));
            AddAssert("dropdown1 is open", () => TestScene.Dropdown.Menu.State == MenuState.Open);

            AddStep("click dropdown2", () => toggleDropdownViaClick(TestScene.TestDropdownMenu));

            AddAssert("dropdown1 is closed", () => TestScene.Dropdown.Menu.State == MenuState.Closed);
            AddAssert("dropdown2 is open", () => TestScene.TestDropdownMenu.Menu.State == MenuState.Open);

            AddStep("select 'invalid'", () => TestScene.Dropdown.Current.Value = "invalid");

            AddAssert("'invalid' is selected", () => TestScene.Dropdown.Current.Value == "invalid");
            AddAssert("label shows 'invalid'", () => TestScene.Dropdown.Header.Label == "invalid");

            AddStep("select item 2", () => TestScene.Dropdown.Current.Value = TestScene.Dropdown.Items.ElementAt(2));
            AddAssert("item 2 is selected", () => TestScene.Dropdown.Current.Value == TestScene.Dropdown.Items.ElementAt(2));

            AddStep("clear bindable list", () => TestScene.BindableList.Clear());
            AddStep("click dropdown3", () => toggleDropdownViaClick(TestScene.BindableDropdown));
            AddAssert("no elements in bindable dropdown", () => !TestScene.BindableDropdown.Items.Any());
            AddStep("add items to bindable", () => TestScene.BindableList.AddRange(new[] { "one", "two", "three" }));
            AddAssert("three items in dropdown", () => TestScene.BindableDropdown.Items.Count() == 3);
            AddStep("select three", () => TestScene.BindableDropdown.Current.Value = "three");
            AddStep("remove first item from bindable", () => TestScene.BindableList.RemoveAt(0));
            AddAssert("two items in dropdown", () => TestScene.BindableDropdown.Items.Count() == 2);
            AddAssert("current value still three", () => TestScene.BindableDropdown.Current.Value == "three");
            AddStep("remove three", () => TestScene.BindableList.Remove("three"));
            AddAssert("current value should be two", () => TestScene.BindableDropdown.Current.Value == "two");
        }

        [Test]
        public void SelectNull()
        {
            AddStep("select item 1", () => TestScene.Dropdown.Current.Value = TestScene.Dropdown.Items.ElementAt(1));
            AddAssert("item 1 is selected", () => TestScene.Dropdown.Current.Value == TestScene.Dropdown.Items.ElementAt(1));
            AddStep("select item null", () => TestScene.Dropdown.Current.Value = null);
            AddAssert("null is selected", () => TestScene.Dropdown.Current.Value == null);
        }

        private void toggleDropdownViaClick(TestSceneDropdown.TestDropdown dropdown)
        {
            InputManager.MoveMouseTo(dropdown.Header);
            InputManager.Click(MouseButton.Left);
        }
    }
}
