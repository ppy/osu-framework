// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using osu.Framework.Bindables;
using osu.Framework.Extensions.IEnumerableExtensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Input;
using osuTK;

namespace osu.Framework.Tests.Visual.UserInterface
{
    public class TestSceneTabControl : FrameworkTestScene
    {
        private readonly TestEnum[] items;

        private StyledTabControl pinnedAndAutoSort;
        private StyledTabControl switchingTabControl;
        private PlatformActionContainer platformActionContainer;
        private StyledTabControlWithoutDropdown withoutDropdownTabControl;
        private StyledTabControl removeAllTabControl;
        private StyledMultilineTabControl multilineTabControl;
        private StyledTabControl simpleTabcontrol;

        public TestSceneTabControl()
        {
            items = (TestEnum[])Enum.GetValues(typeof(TestEnum));
        }

        [SetUp]
        public void Setup() => Schedule(() =>
        {
            Clear();

            Add(new FillFlowContainer
            {
                RelativeSizeAxes = Axes.Both,
                Direction = FillDirection.Full,
                Spacing = new Vector2(50),
                Children = new Drawable[]
                {
                    simpleTabcontrol = new StyledTabControl
                    {
                        Size = new Vector2(200, 30),
                    },
                    multilineTabControl = new StyledMultilineTabControl
                    {
                        Size = new Vector2(200, 60),
                    },
                    pinnedAndAutoSort = new StyledTabControl
                    {
                        Size = new Vector2(200, 30),
                        AutoSort = true
                    },
                    platformActionContainer = new PlatformActionContainer
                    {
                        RelativeSizeAxes = Axes.None,
                        Size = new Vector2(200, 30),
                        Child = switchingTabControl = new StyledTabControl
                        {
                            RelativeSizeAxes = Axes.Both
                        }
                    },
                    removeAllTabControl = new StyledTabControl
                    {
                        Size = new Vector2(200, 30)
                    },
                    withoutDropdownTabControl = new StyledTabControlWithoutDropdown
                    {
                        Size = new Vector2(200, 30)
                    },
                }
            });

            foreach (var item in items)
            {
                simpleTabcontrol.AddItem(item);
                multilineTabControl.AddItem(item);
                switchingTabControl.AddItem(item);
                withoutDropdownTabControl.AddItem(item);
            }

            items.Take(7).ForEach(item => pinnedAndAutoSort.AddItem(item));
            pinnedAndAutoSort.PinItem(TestEnum.Test5);
        });

        [Test]
        public void Basic()
        {
            var nextTest = new Func<TestEnum>(() => items.FirstOrDefault(test => !pinnedAndAutoSort.Items.Contains(test)));

            Stack<TestEnum> pinned = new Stack<TestEnum>();

            Steps.AddStep("AddItem", () =>
            {
                var item = nextTest.Invoke();
                if (!pinnedAndAutoSort.Items.Contains(item))
                    pinnedAndAutoSort.AddItem(item);
            });

            Steps.AddStep("RemoveItem", () =>
            {
                if (pinnedAndAutoSort.Items.Any())
                {
                    pinnedAndAutoSort.RemoveItem(pinnedAndAutoSort.Items.First());
                }
            });

            Steps.AddStep("PinItem", () =>
            {
                var item = nextTest.Invoke();

                if (!pinnedAndAutoSort.Items.Contains(item))
                {
                    pinned.Push(item);
                    pinnedAndAutoSort.AddItem(item);
                    pinnedAndAutoSort.PinItem(item);
                }
            });

            Steps.AddStep("UnpinItem", () =>
            {
                if (pinned.Count > 0) pinnedAndAutoSort.UnpinItem(pinned.Pop());
            });

            Steps.AddStep("Set first tab", () => switchingTabControl.Current.Value = switchingTabControl.VisibleItems.First());
            Steps.AddStep("Switch forward", () => platformActionContainer.TriggerPressed(new PlatformAction(PlatformActionType.DocumentNext)));
            Steps.AddAssert("Ensure second tab", () => switchingTabControl.Current.Value == switchingTabControl.VisibleItems.ElementAt(1));

            Steps.AddStep("Switch backward", () => platformActionContainer.TriggerPressed(new PlatformAction(PlatformActionType.DocumentPrevious)));
            Steps.AddAssert("Ensure first Tab", () => switchingTabControl.Current.Value == switchingTabControl.VisibleItems.First());

            Steps.AddStep("Switch backward", () => platformActionContainer.TriggerPressed(new PlatformAction(PlatformActionType.DocumentPrevious)));
            Steps.AddAssert("Ensure last tab", () => switchingTabControl.Current.Value == switchingTabControl.VisibleItems.Last());

            Steps.AddStep("Switch forward", () => platformActionContainer.TriggerPressed(new PlatformAction(PlatformActionType.DocumentNext)));
            Steps.AddAssert("Ensure first tab", () => switchingTabControl.Current.Value == switchingTabControl.VisibleItems.First());

            Steps.AddStep("Add all items", () => items.ForEach(item => removeAllTabControl.AddItem(item)));
            Steps.AddAssert("Ensure all items", () => removeAllTabControl.Items.Count() == items.Length);

            Steps.AddStep("Remove all items", () => removeAllTabControl.Clear());
            Steps.AddAssert("Ensure no items", () => !removeAllTabControl.Items.Any());

            Steps.AddAssert("Ensure any items", () => withoutDropdownTabControl.Items.Any());
            Steps.AddStep("Remove all items", () => withoutDropdownTabControl.Clear());
            Steps.AddAssert("Ensure no items", () => !withoutDropdownTabControl.Items.Any());

            Steps.AddAssert("Ensure not all items visible on singleline", () => simpleTabcontrol.VisibleItems.Count() < items.Length);
            Steps.AddAssert("Ensure all items visible on multiline", () => multilineTabControl.VisibleItems.Count() == items.Length);
        }

        [Test]
        public void TestLeasedBindable()
        {
            LeasedBindable<TestEnum?> leased = null;

            Steps.AddStep("change value to test0", () => simpleTabcontrol.Current.Value = TestEnum.Test0);
            Steps.AddStep("lease bindable", () => leased = simpleTabcontrol.Current.BeginLease(true));
            Steps.AddStep("change value to test1", () => leased.Value = TestEnum.Test1);
            Steps.AddAssert("value changed", () => simpleTabcontrol.Current.Value == TestEnum.Test1);
            Steps.AddAssert("tab changed", () => simpleTabcontrol.SelectedTab.Value == TestEnum.Test1);
            Steps.AddStep("end lease", () => leased.UnbindAll());
        }

        [Test]
        public void TestDisabledBindable()
        {
            Bindable<TestEnum?> bindable;

            Steps.AddStep("add tabcontrol", () =>
            {
                bindable = new Bindable<TestEnum?> { Value = TestEnum.Test2 };

                simpleTabcontrol = new StyledTabControl
                {
                    Size = new Vector2(200, 30)
                };

                foreach (var item in items)
                    simpleTabcontrol.AddItem(item);

                bindable.Disabled = true;
                simpleTabcontrol.Current = bindable;

                Child = simpleTabcontrol;
            });

            Steps.AddAssert("test2 selected", () => simpleTabcontrol.SelectedTab.Value == TestEnum.Test2);

            // Todo: Should not fail
            // Steps.AddStep("click a tab", () => simpleTabcontrol.TabMap[TestEnum.Test0].Click());
        }

        [TestCase(true)]
        [TestCase(false)]
        public void SelectNull(bool autoSort)
        {
            Steps.AddStep($"Set autosort to {autoSort}", () => simpleTabcontrol.AutoSort = autoSort);
            Steps.AddStep("select item 1", () => simpleTabcontrol.Current.Value = simpleTabcontrol.Items.ElementAt(1));
            Steps.AddAssert("item 1 is selected", () => simpleTabcontrol.Current.Value == simpleTabcontrol.Items.ElementAt(1));
            Steps.AddStep("select item null", () => simpleTabcontrol.Current.Value = null);
            Steps.AddAssert("null is selected", () => simpleTabcontrol.Current.Value == null);
        }

        [Test]
        public void TestRemovingTabMovesOutFromDropdown()
        {
            Steps.AddStep("Remove test3", () => simpleTabcontrol.RemoveItem(TestEnum.Test3));
            Steps.AddAssert("Test 4 is visible", () => simpleTabcontrol.TabMap[TestEnum.Test4].IsPresent);

            Steps.AddUntilStep("Remove all visible items", () =>
            {
                simpleTabcontrol.RemoveItem(simpleTabcontrol.Items.First(d => simpleTabcontrol.TabMap[d].IsPresent));
                return !simpleTabcontrol.Dropdown.Items.Any();
            });
        }

        private class StyledTabControlWithoutDropdown : TabControl<TestEnum>
        {
            protected override Dropdown<TestEnum> CreateDropdown() => null;

            protected override TabItem<TestEnum> CreateTabItem(TestEnum value)
                => new BasicTabControl<TestEnum>.BasicTabItem(value);
        }

        private class StyledMultilineTabControl : TabControl<TestEnum>
        {
            protected override Dropdown<TestEnum> CreateDropdown() => null;

            protected override TabItem<TestEnum> CreateTabItem(TestEnum value)
                => new BasicTabControl<TestEnum>.BasicTabItem(value);

            protected override TabFillFlowContainer CreateTabFlow() => base.CreateTabFlow().With(f => { f.AllowMultiline = true; });
        }

        private class StyledTabControl : TabControl<TestEnum?>
        {
            public new IReadOnlyDictionary<TestEnum?, TabItem<TestEnum?>> TabMap => base.TabMap;

            public new TabItem<TestEnum?> SelectedTab => base.SelectedTab;

            public new Dropdown<TestEnum?> Dropdown => base.Dropdown;

            protected override Dropdown<TestEnum?> CreateDropdown() => new StyledDropdown();

            protected override TabItem<TestEnum?> CreateTabItem(TestEnum? value)
                => new BasicTabControl<TestEnum?>.BasicTabItem(value);
        }

        private class StyledDropdown : Dropdown<TestEnum?>
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

            private class StyledDropdownMenu : BasicDropdown<TestEnum?>.BasicDropdownMenu
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
