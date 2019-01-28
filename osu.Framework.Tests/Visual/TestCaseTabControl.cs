﻿// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Framework.Extensions;
using osu.Framework.Extensions.IEnumerableExtensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Testing;
using osu.Framework.Input;
using osuTK;

namespace osu.Framework.Tests.Visual
{
    public class TestCaseTabControl : TestCase
    {
        public TestCaseTabControl()
        {
            List<KeyValuePair<string, TestEnum>> items = new List<KeyValuePair<string, TestEnum>>();
            foreach (var val in (TestEnum[])Enum.GetValues(typeof(TestEnum)))
                items.Add(new KeyValuePair<string, TestEnum>(val.GetDescription(), val));

            StyledTabControl simpleTabcontrol = new StyledTabControl
            {
                Position = new Vector2(200, 50),
                Size = new Vector2(200, 30),
            };
            items.AsEnumerable().ForEach(item => simpleTabcontrol.AddItem(item.Value));

            StyledTabControl pinnedAndAutoSort = new StyledTabControl
            {
                Position = new Vector2(200, 150),
                Size = new Vector2(200, 30),
                AutoSort = true
            };
            items.GetRange(0, 7).AsEnumerable().ForEach(item => pinnedAndAutoSort.AddItem(item.Value));
            pinnedAndAutoSort.PinItem(TestEnum.Test5);

            StyledTabControl switchingTabControl;
            PlatformActionContainer platformActionContainer = new PlatformActionContainer
            {
                Child = switchingTabControl = new StyledTabControl
                {
                    Position = new Vector2(200, 250),
                    Size = new Vector2(200, 30),
                }
            };
            items.AsEnumerable().ForEach(item => switchingTabControl.AddItem(item.Value));

            StyledTabControl removeAllTabControl = new StyledTabControl
            {
                Position = new Vector2(200, 350),
                Size = new Vector2(200, 30)
            };

            var withoutDropdownTabControl = new StyledTabControlWithoutDropdown
            {
                Position = new Vector2(200, 450),
                Size = new Vector2(200, 30)
            };
            items.AsEnumerable().ForEach(item => withoutDropdownTabControl.AddItem(item.Value));

            Add(simpleTabcontrol);
            Add(pinnedAndAutoSort);
            Add(platformActionContainer);
            Add(removeAllTabControl);
            Add(withoutDropdownTabControl);

            var nextTest = new Func<TestEnum>(() => items.AsEnumerable()
                                                         .Select(item => item.Value)
                                                         .FirstOrDefault(test => !pinnedAndAutoSort.Items.Contains(test)));

            Stack<TestEnum> pinned = new Stack<TestEnum>();

            AddStep("AddItem", () =>
            {
                var item = nextTest.Invoke();
                if (!pinnedAndAutoSort.Items.Contains(item))
                    pinnedAndAutoSort.AddItem(item);
            });

            AddStep("RemoveItem", () =>
            {
                if (pinnedAndAutoSort.Items.Any())
                {
                    pinnedAndAutoSort.RemoveItem(pinnedAndAutoSort.Items.First());
                }
            });

            AddStep("PinItem", () =>
            {
                var item = nextTest.Invoke();

                if (!pinnedAndAutoSort.Items.Contains(item))
                {
                    pinned.Push(item);
                    pinnedAndAutoSort.AddItem(item);
                    pinnedAndAutoSort.PinItem(item);
                }
            });

            AddStep("UnpinItem", () =>
            {
                if (pinned.Count > 0) pinnedAndAutoSort.UnpinItem(pinned.Pop());
            });

            AddStep("Set first tab", () => switchingTabControl.Current.Value = switchingTabControl.VisibleItems.First());
            AddStep("Switch forward", () => platformActionContainer.TriggerPressed(new PlatformAction(PlatformActionType.DocumentNext)));
            AddAssert("Ensure second tab", () => switchingTabControl.Current.Value == switchingTabControl.VisibleItems.ElementAt(1));

            AddStep("Switch backward", () => platformActionContainer.TriggerPressed(new PlatformAction(PlatformActionType.DocumentPrevious)));
            AddAssert("Ensure first Tab", () => switchingTabControl.Current.Value == switchingTabControl.VisibleItems.First());

            AddStep("Switch backward", () => platformActionContainer.TriggerPressed(new PlatformAction(PlatformActionType.DocumentPrevious)));
            AddAssert("Ensure last tab", () => switchingTabControl.Current.Value == switchingTabControl.VisibleItems.Last());

            AddStep("Switch forward", () => platformActionContainer.TriggerPressed(new PlatformAction(PlatformActionType.DocumentNext)));
            AddAssert("Ensure first tab", () => switchingTabControl.Current.Value == switchingTabControl.VisibleItems.First());

            AddStep("Add all items", () => items.AsEnumerable().ForEach(item => removeAllTabControl.AddItem(item.Value)));
            AddAssert("Ensure all items", () => removeAllTabControl.Items.Count() == items.Count);

            AddStep("Remove all items", () => removeAllTabControl.Clear());
            AddAssert("Ensure no items", () => !removeAllTabControl.Items.Any());

            AddAssert("Ensure any items", () => withoutDropdownTabControl.Items.Any());
            AddStep("Remove all items", () => withoutDropdownTabControl.Clear());
            AddAssert("Ensure no items", () => !withoutDropdownTabControl.Items.Any());
        }

        private class StyledTabControlWithoutDropdown : TabControl<TestEnum>
        {
            protected override Dropdown<TestEnum> CreateDropdown() => null;

            protected override TabItem<TestEnum> CreateTabItem(TestEnum value)
                => new BasicTabControl<TestEnum>.BasicTabItem(value);
        }

        private class StyledTabControl : TabControl<TestEnum>
        {
            protected override Dropdown<TestEnum> CreateDropdown() => new StyledDropdown();

            protected override TabItem<TestEnum> CreateTabItem(TestEnum value)
                => new BasicTabControl<TestEnum>.BasicTabItem(value);
        }

        private class StyledDropdown : Dropdown<TestEnum>
        {
            protected override DropdownMenu CreateMenu() => new StyledDropdownMenu();

            protected override DropdownHeader CreateHeader() => new StyledDropdownHeader();

            public StyledDropdown()
            {
                Menu.Anchor = Anchor.TopRight;
                Menu.Origin = Anchor.TopRight;
                Header.Anchor = Anchor.TopRight;
                Header.Origin = Anchor.TopRight;
            }

            private class StyledDropdownMenu : DropdownMenu
            {
                public StyledDropdownMenu()
                {
                    ScrollbarVisible = false;
                    CornerRadius = 4;
                }
            }
        }

        private class StyledDropdownHeader : DropdownHeader
        {
            protected internal override string Label { get; set; }

            public StyledDropdownHeader()
            {
                Background.Hide(); // don't need a background

                RelativeSizeAxes = Axes.None;
                AutoSizeAxes = Axes.X;

                Foreground.RelativeSizeAxes = Axes.None;
                Foreground.AutoSizeAxes = Axes.Both;

                Foreground.Children = new[]
                {
                    new Box { Width = 20, Height = 20 }
                };
            }
        }

        private enum TestEnum
        {
            Test0,
            Test1,
            Test2,
            Test3,
            Test4,
            Test5,
            Test6,
            Test7,
            Test8,
            Test9,
            Test10,
            Test11,
            Test12
        }
    }
}
