// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using osu.Framework.Bindables;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Input.Events;
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
        private readonly TestDropdown testDropdown, testDropdownMenu, bindableDropdown;
        private readonly BindableList<string> bindableList = new BindableList<string>();

        public override IReadOnlyList<Type> RequiredTypes => new[]
        {
            typeof(Dropdown<>),
            typeof(DropdownHeader),
            typeof(DropdownMenuItem<>),
            typeof(Dropdown<>),
            typeof(BasicDropdown<>),
            typeof(BasicDropdown<>.BasicDropdownHeader),
            typeof(BasicDropdown<>.BasicDropdownMenu),
            typeof(TestDropdown)
        };

        public TestSceneDropdown()
        {
            var testItems = new string[10];
            int i = 0;
            while (i < items_to_add)
                testItems[i] = @"test " + i++;

            Add(testDropdown = new TestDropdown
            {
                Width = 150,
                Position = new Vector2(200, 70),
                Items = testItems
            });

            Add(testDropdownMenu = new TestDropdown
            {
                Width = 150,
                Position = new Vector2(400, 70),
                Items = testItems
            });

            Add(bindableDropdown = new TestDropdown
            {
                Width = 150,
                Position = new Vector2(600, 70),
                ItemSource = bindableList
            });
        }

        [Test]
        public void Basic()
        {
            var i = items_to_add;

            Steps.AddStep("click dropdown1", () => toggleDropdownViaClick(testDropdown));
            Steps.AddAssert("dropdown is open", () => testDropdown.Menu.State == MenuState.Open);

            Steps.AddRepeatStep("add item", () => testDropdown.AddDropdownItem("test " + i++), items_to_add);
            Steps.AddAssert("item count is correct", () => testDropdown.Items.Count() == items_to_add * 2);

            Steps.AddStep($"Set dropdown1 height to {explicit_height}", () =>
            {
                calculatedHeight = testDropdown.Menu.Height;
                testDropdown.Menu.MaxHeight = explicit_height;
            });
            Steps.AddAssert($"dropdown1 height is {explicit_height}", () => testDropdown.Menu.Height == explicit_height);

            Steps.AddStep($"Set dropdown1 height to {float.PositiveInfinity}", () => testDropdown.Menu.MaxHeight = float.PositiveInfinity);
            Steps.AddAssert("dropdown1 height is calculated automatically", () => testDropdown.Menu.Height == calculatedHeight);

            Steps.AddStep("click item 13", () => testDropdown.SelectItem(testDropdown.Menu.Items[13]));

            Steps.AddAssert("dropdown1 is closed", () => testDropdown.Menu.State == MenuState.Closed);
            Steps.AddAssert("item 13 is selected", () => testDropdown.Current.Value == testDropdown.Items.ElementAt(13));

            Steps.AddStep("select item 15", () => testDropdown.Current.Value = testDropdown.Items.ElementAt(15));
            Steps.AddAssert("item 15 is selected", () => testDropdown.Current.Value == testDropdown.Items.ElementAt(15));

            Steps.AddStep("click dropdown1", () => toggleDropdownViaClick(testDropdown));
            Steps.AddAssert("dropdown1 is open", () => testDropdown.Menu.State == MenuState.Open);

            Steps.AddStep("click dropdown2", () => toggleDropdownViaClick(testDropdownMenu));

            Steps.AddAssert("dropdown1 is closed", () => testDropdown.Menu.State == MenuState.Closed);
            Steps.AddAssert("dropdown2 is open", () => testDropdownMenu.Menu.State == MenuState.Open);

            Steps.AddStep("select 'invalid'", () => testDropdown.Current.Value = "invalid");

            Steps.AddAssert("'invalid' is selected", () => testDropdown.Current.Value == "invalid");
            Steps.AddAssert("label shows 'invalid'", () => testDropdown.Header.Label == "invalid");

            Steps.AddStep("select item 2", () => testDropdown.Current.Value = testDropdown.Items.ElementAt(2));
            Steps.AddAssert("item 2 is selected", () => testDropdown.Current.Value == testDropdown.Items.ElementAt(2));

            Steps.AddStep("clear bindable list", () => bindableList.Clear());
            Steps.AddStep("click dropdown3", () => toggleDropdownViaClick(bindableDropdown));
            Steps.AddAssert("no elements in bindable dropdown", () => !bindableDropdown.Items.Any());
            Steps.AddStep("add items to bindable", () => bindableList.AddRange(new[] { "one", "two", "three" }));
            Steps.AddAssert("three items in dropdown", () => bindableDropdown.Items.Count() == 3);
            Steps.AddStep("select three", () => bindableDropdown.Current.Value = "three");
            Steps.AddStep("remove first item from bindable", () => bindableList.RemoveAt(0));
            Steps.AddAssert("two items in dropdown", () => bindableDropdown.Items.Count() == 2);
            Steps.AddAssert("current value still three", () => bindableDropdown.Current.Value == "three");
            Steps.AddStep("remove three", () => bindableList.Remove("three"));
            Steps.AddAssert("current value should be two", () => bindableDropdown.Current.Value == "two");
        }

        [Test]
        public void SelectNull()
        {
            Steps.AddStep("select item 1", () => testDropdown.Current.Value = testDropdown.Items.ElementAt(1));
            Steps.AddAssert("item 1 is selected", () => testDropdown.Current.Value == testDropdown.Items.ElementAt(1));
            Steps.AddStep("select item null", () => testDropdown.Current.Value = null);
            Steps.AddAssert("null is selected", () => testDropdown.Current.Value == null);
        }

        private void toggleDropdownViaClick(TestDropdown dropdown)
        {
            InputManager.MoveMouseTo(dropdown.Header);
            InputManager.Click(MouseButton.Left);
        }

        private class TestDropdown : BasicDropdown<string>
        {
            public new DropdownMenu Menu => base.Menu;

            protected override DropdownMenu CreateMenu() => new TestDropdownMenu();

            protected override DropdownHeader CreateHeader() => new BasicDropdownHeader();

            public void SelectItem(MenuItem item) => ((TestDropdownMenu)Menu).SelectItem(item);

            private class TestDropdownMenu : BasicDropdownMenu
            {
                public void SelectItem(MenuItem item) => Children.FirstOrDefault(c => c.Item == item)?
                    .TriggerEvent(new ClickEvent(GetContainingInputManager().CurrentState, MouseButton.Left));
            }
        }
    }
}
