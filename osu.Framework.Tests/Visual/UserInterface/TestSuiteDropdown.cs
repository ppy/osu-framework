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

        [Test]
        public void Basic()
        {
            var i = TestSceneDropdown.ITEMS_TO_ADD;

            AddStep("click dropdown1", () => toggleDropdownViaClick(Scene.Dropdown));
            AddAssert("dropdown is open", () => Scene.Dropdown.Menu.State == MenuState.Open);

            AddRepeatStep("add item", () => Scene.Dropdown.AddDropdownItem("test " + i++), TestSceneDropdown.ITEMS_TO_ADD);
            AddAssert("item count is correct", () => Scene.Dropdown.Items.Count() == TestSceneDropdown.ITEMS_TO_ADD * 2);

            AddStep($"Set dropdown1 height to {explicit_height}", () =>
            {
                calculatedHeight = Scene.Dropdown.Menu.Height;
                Scene.Dropdown.Menu.MaxHeight = explicit_height;
            });
            AddAssert($"dropdown1 height is {explicit_height}", () => Scene.Dropdown.Menu.Height == explicit_height);

            AddStep($"Set dropdown1 height to {float.PositiveInfinity}", () => Scene.Dropdown.Menu.MaxHeight = float.PositiveInfinity);
            AddAssert("dropdown1 height is calculated automatically", () => Scene.Dropdown.Menu.Height == calculatedHeight);

            AddStep("click item 13", () => Scene.Dropdown.SelectItem(Scene.Dropdown.Menu.Items[13]));

            AddAssert("dropdown1 is closed", () => Scene.Dropdown.Menu.State == MenuState.Closed);
            AddAssert("item 13 is selected", () => Scene.Dropdown.Current.Value == Scene.Dropdown.Items.ElementAt(13));

            AddStep("select item 15", () => Scene.Dropdown.Current.Value = Scene.Dropdown.Items.ElementAt(15));
            AddAssert("item 15 is selected", () => Scene.Dropdown.Current.Value == Scene.Dropdown.Items.ElementAt(15));

            AddStep("click dropdown1", () => toggleDropdownViaClick(Scene.Dropdown));
            AddAssert("dropdown1 is open", () => Scene.Dropdown.Menu.State == MenuState.Open);

            AddStep("click dropdown2", () => toggleDropdownViaClick(Scene.TestDropdownMenu));

            AddAssert("dropdown1 is closed", () => Scene.Dropdown.Menu.State == MenuState.Closed);
            AddAssert("dropdown2 is open", () => Scene.TestDropdownMenu.Menu.State == MenuState.Open);

            AddStep("select 'invalid'", () => Scene.Dropdown.Current.Value = "invalid");

            AddAssert("'invalid' is selected", () => Scene.Dropdown.Current.Value == "invalid");
            AddAssert("label shows 'invalid'", () => Scene.Dropdown.Header.Label == "invalid");

            AddStep("select item 2", () => Scene.Dropdown.Current.Value = Scene.Dropdown.Items.ElementAt(2));
            AddAssert("item 2 is selected", () => Scene.Dropdown.Current.Value == Scene.Dropdown.Items.ElementAt(2));

            AddStep("clear bindable list", () => Scene.BindableList.Clear());
            AddStep("click dropdown3", () => toggleDropdownViaClick(Scene.BindableDropdown));
            AddAssert("no elements in bindable dropdown", () => !Scene.BindableDropdown.Items.Any());
            AddStep("add items to bindable", () => Scene.BindableList.AddRange(new[] { "one", "two", "three" }));
            AddAssert("three items in dropdown", () => Scene.BindableDropdown.Items.Count() == 3);
            AddStep("select three", () => Scene.BindableDropdown.Current.Value = "three");
            AddStep("remove first item from bindable", () => Scene.BindableList.RemoveAt(0));
            AddAssert("two items in dropdown", () => Scene.BindableDropdown.Items.Count() == 2);
            AddAssert("current value still three", () => Scene.BindableDropdown.Current.Value == "three");
            AddStep("remove three", () => Scene.BindableList.Remove("three"));
            AddAssert("current value should be two", () => Scene.BindableDropdown.Current.Value == "two");
        }

        [Test]
        public void SelectNull()
        {
            AddStep("select item 1", () => Scene.Dropdown.Current.Value = Scene.Dropdown.Items.ElementAt(1));
            AddAssert("item 1 is selected", () => Scene.Dropdown.Current.Value == Scene.Dropdown.Items.ElementAt(1));
            AddStep("select item null", () => Scene.Dropdown.Current.Value = null);
            AddAssert("null is selected", () => Scene.Dropdown.Current.Value == null);
        }

        private void toggleDropdownViaClick(TestSceneDropdown.TestDropdown dropdown)
        {
            InputManager.MoveMouseTo(dropdown.Header);
            InputManager.Click(MouseButton.Left);
        }
    }
}
