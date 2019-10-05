// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
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
        public readonly TestDropdown Dropdown, TestDropdownMenu, BindableDropdown;
        public readonly BindableList<string> BindableList = new BindableList<string>();
        public readonly string[] TestItems = new string[10];

        public TestSceneDropdown()
        {
            Add(Dropdown = new TestDropdown
            {
                Width = 150,
                Position = new Vector2(200, 70),
                Items = TestItems
            });

            Add(TestDropdownMenu = new TestDropdown
            {
                Width = 150,
                Position = new Vector2(400, 70),
                Items = TestItems
            });

            Add(BindableDropdown = new TestDropdown
            {
                Width = 150,
                Position = new Vector2(600, 70),
                ItemSource = BindableList
            });
        }

        public class TestDropdown : BasicDropdown<string>
        {
            public new DropdownMenu Menu => base.Menu;

            protected override DropdownMenu CreateMenu() => new TestDropdownMenu();

            protected override DropdownHeader CreateHeader() => new BasicDropdownHeader();

            public void SelectItem(MenuItem item) => ((TestDropdownMenu)Menu).SelectItem(item);

            public class TestDropdownMenu : BasicDropdownMenu
            {
                public void SelectItem(MenuItem item) => Children.FirstOrDefault(c => c.Item == item)?
                    .TriggerEvent(new ClickEvent(GetContainingInputManager().CurrentState, MouseButton.Left));
            }
        }
    }
}
