// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

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
using osu.Framework.Localisation;
using osuTK;

namespace osu.Framework.Tests.Visual.UserInterface
{
    public class TestSceneTabControl : FrameworkTestScene
    {
        private readonly TestEnum[] items;

        private FillFlowContainer tabControlContainer;

        private StyledTabControl pinnedAndAutoSort;
        private StyledTabControl switchingTabControl;
        private PlatformActionContainer platformActionContainer;
        private StyledTabControlWithoutDropdown withoutDropdownTabControl;
        private StyledTabControl removeAllTabControl;
        private StyledMultilineTabControl multilineTabControl;
        private StyledTabControl simpleTabcontrol;
        private StyledTabControl simpleTabcontrolNoSwitchOnRemove;
        private BasicTabControl<TestEnum?> basicTabControl;

        public TestSceneTabControl()
        {
            items = (TestEnum[])Enum.GetValues(typeof(TestEnum));
        }

        [SetUp]
        public void Setup() => Schedule(() =>
        {
            Clear();

            Add(tabControlContainer = new FillFlowContainer
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
                    simpleTabcontrolNoSwitchOnRemove = new StyledTabControl
                    {
                        Size = new Vector2(200, 30),
                        SwitchTabOnRemove = false
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
                            RelativeSizeAxes = Axes.Both,
                            IsSwitchable = true,
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
                    basicTabControl = new BasicTabControl<TestEnum?>
                    {
                        Size = new Vector2(200, 20)
                    }
                }
            });

            foreach (var item in items)
            {
                simpleTabcontrol.AddItem(item);
                simpleTabcontrolNoSwitchOnRemove.AddItem(item);
                multilineTabControl.AddItem(item);
                switchingTabControl.AddItem(item);
                withoutDropdownTabControl.AddItem(item);
                basicTabControl.AddItem(item);
            }

            items.Take(7).ForEach(item => pinnedAndAutoSort.AddItem(item));
            pinnedAndAutoSort.PinItem(TestEnum.Test5);
        });

        [Test]
        public void Basic()
        {
            var nextTest = new Func<TestEnum>(() => items.FirstOrDefault(test => !pinnedAndAutoSort.Items.Contains(test)));

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

            AddStep("Set first tab", () => switchingTabControl.Current.Value = switchingTabControl.Items.First());
            AddStep("Switch forward", () => platformActionContainer.TriggerPressed(PlatformAction.DocumentNext));
            AddAssert("Ensure second tab", () => switchingTabControl.Current.Value == switchingTabControl.Items.ElementAt(1));

            AddStep("Switch backward", () => platformActionContainer.TriggerPressed(PlatformAction.DocumentPrevious));
            AddAssert("Ensure first Tab", () => switchingTabControl.Current.Value == switchingTabControl.Items.First());

            AddStep("Switch backward", () => platformActionContainer.TriggerPressed(PlatformAction.DocumentPrevious));
            AddAssert("Ensure last tab", () => switchingTabControl.Current.Value == switchingTabControl.Items.Last());

            AddStep("Switch forward", () => platformActionContainer.TriggerPressed(PlatformAction.DocumentNext));
            AddAssert("Ensure first tab", () => switchingTabControl.Current.Value == switchingTabControl.Items.First());

            AddStep("Add all items", () => items.ForEach(item => removeAllTabControl.AddItem(item)));
            AddAssert("Ensure all items", () => removeAllTabControl.Items.Count == items.Length);

            AddStep("Remove all items", () => removeAllTabControl.Clear());
            AddAssert("Ensure no items", () => !removeAllTabControl.Items.Any());

            AddAssert("Ensure any items", () => withoutDropdownTabControl.Items.Any());
            AddStep("Remove all items", () => withoutDropdownTabControl.Clear());
            AddAssert("Ensure no items", () => !withoutDropdownTabControl.Items.Any());

            AddAssert("Ensure not all items visible on singleline", () => simpleTabcontrol.VisibleItems.Count() < items.Length);
            AddAssert("Ensure all items visible on multiline", () => multilineTabControl.VisibleItems.Count() == items.Length);
        }

        [Test]
        public void TestLeasedBindable()
        {
            LeasedBindable<TestEnum?> leased = null;

            AddStep("change value to test0", () => simpleTabcontrol.Current.Value = TestEnum.Test0);
            AddStep("lease bindable", () => leased = simpleTabcontrol.Current.BeginLease(true));
            AddStep("change value to test1", () => leased.Value = TestEnum.Test1);
            AddAssert("value changed", () => simpleTabcontrol.Current.Value == TestEnum.Test1);
            AddAssert("tab changed", () => simpleTabcontrol.SelectedTab.Value == TestEnum.Test1);
            AddStep("end lease", () => leased.UnbindAll());
        }

        [Test]
        public void TestTabSelectedWhenDisabledBindableIsBound()
        {
            Bindable<TestEnum?> bindable;

            AddStep("add tabcontrol", () =>
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

            AddAssert("test2 selected", () => simpleTabcontrol.SelectedTab.Value == TestEnum.Test2);
        }

        [Test]
        public void TestClicksBlockedWhenBindableDisabled()
        {
            AddStep("add tabcontrol", () =>
            {
                Child = simpleTabcontrol = new StyledTabControl { Size = new Vector2(200, 30) };

                foreach (var item in items)
                    simpleTabcontrol.AddItem(item);

                simpleTabcontrol.Current = new Bindable<TestEnum?>
                {
                    Value = TestEnum.Test0,
                    Disabled = true
                };
            });

            AddStep("click a tab", () => simpleTabcontrol.TabMap[TestEnum.Test2].TriggerClick());
            AddAssert("test0 still selected", () => simpleTabcontrol.SelectedTab.Value == TestEnum.Test0);
        }

        [Test]
        public void TestSelectNotPresentItem()
        {
            AddStep("remove item 6", () => simpleTabcontrol.RemoveItem(TestEnum.Test6));
            AddStep("select item 6", () => simpleTabcontrol.Current.Value = TestEnum.Test6);

            AddAssert("current is 6", () => simpleTabcontrol.Current.Value == TestEnum.Test6);
            AddAssert("no tab selected", () => simpleTabcontrol.SelectedTab == null);
        }

        [TestCase(true)]
        [TestCase(false)]
        public void SelectNull(bool autoSort)
        {
            AddStep($"Set autosort to {autoSort}", () => simpleTabcontrol.AutoSort = autoSort);
            AddStep("select item 1", () => simpleTabcontrol.Current.Value = simpleTabcontrol.Items.ElementAt(1));
            AddAssert("item 1 is selected", () => simpleTabcontrol.Current.Value == simpleTabcontrol.Items.ElementAt(1));
            AddStep("select item null", () => simpleTabcontrol.Current.Value = null);
            AddAssert("null is selected", () => simpleTabcontrol.Current.Value == null);
        }

        [Test]
        public void TestRemovingTabMovesOutFromDropdown()
        {
            AddStep("Remove test3", () => simpleTabcontrol.RemoveItem(TestEnum.Test3));
            AddAssert("Test 4 is visible", () => simpleTabcontrol.TabMap[TestEnum.Test4].IsPresent);

            AddUntilStep("Remove all visible items", () =>
            {
                simpleTabcontrol.RemoveItem(simpleTabcontrol.Items.First(d => simpleTabcontrol.TabMap[d].IsPresent));
                return !simpleTabcontrol.Dropdown.Items.Any();
            });
        }

        [Test]
        public void TestRemovingSelectedTabSwitchesSelection()
        {
            AddStep("Select tab 2", () => simpleTabcontrol.Current.Value = TestEnum.Test2);
            AddStep("Remove tab 2", () => simpleTabcontrol.RemoveItem(TestEnum.Test2));
            AddAssert("Ensure selection switches to next tab", () => simpleTabcontrol.SelectedTab.Value == TestEnum.Test3);

            AddStep("Select last tab", () => simpleTabcontrol.Current.Value = simpleTabcontrol.Items.Last());
            AddStep("Remove selected tab", () => simpleTabcontrol.RemoveItem(simpleTabcontrol.SelectedTab.Value));
            AddAssert("Ensure selection switches to previous tab", () => simpleTabcontrol.SelectedTab.Value == simpleTabcontrol.Items.Last());

            AddStep("Remove all tabs", () =>
            {
                var itemsForDelete = new List<TestEnum?>(simpleTabcontrol.Items);
                itemsForDelete.ForEach(item => simpleTabcontrol.RemoveItem(item));
            });
            AddAssert("Ensure selected tab is null", () => simpleTabcontrol.SelectedTab == null);
        }

        /// <summary>
        /// Tests that the selection is not switched on a <see cref="TabControl{T}"/> that has <see cref="TabControl{T}.SwitchTabOnRemove"/> set to <c>false</c>.
        /// </summary>
        [Test]
        public void TestRemovingSelectedTabDoesNotSwitchSelectionIfNotSwitchTabOnRemove()
        {
            AddStep("Select tab 2", () => simpleTabcontrolNoSwitchOnRemove.Current.Value = TestEnum.Test2);
            AddStep("Remove tab 2", () => simpleTabcontrolNoSwitchOnRemove.RemoveItem(TestEnum.Test2));
            AddAssert("Ensure has not switched", () => simpleTabcontrolNoSwitchOnRemove.SelectedTab.Value == TestEnum.Test2);
        }

        [Test]
        public void TestRemovingUnswitchableTab()
        {
            AddStep("Set last tab unswitchable", () => ((StyledTabControl.TestTabItem)simpleTabcontrol.SwitchableTabs.Last()).SetSwitchable(false));

            AddStep("Select last tab", () => simpleTabcontrol.Current.Value = simpleTabcontrol.Items.Last());
            AddStep("Remove selected tab", () => simpleTabcontrol.RemoveItem(simpleTabcontrol.SelectedTab.Value));
            AddAssert("Ensure selection switches to previous tab", () => simpleTabcontrol.SelectedTab.Value == simpleTabcontrol.Items.Last());

            AddStep("Set first tab unswitchable", () => ((StyledTabControl.TestTabItem)simpleTabcontrol.SwitchableTabs.First()).SetSwitchable(false));

            AddStep("Select first tab", () => simpleTabcontrol.Current.Value = simpleTabcontrol.Items.First());
            AddStep("Remove selected tab", () => simpleTabcontrol.RemoveItem(simpleTabcontrol.SelectedTab.Value));
            AddAssert("Ensure selection switches to next tab", () => simpleTabcontrol.SelectedTab.Value == simpleTabcontrol.Items.First());
        }

        [Test]
        public void TestUnswitchableNotSelectedOnRemoveAll()
        {
            AddStep("Set middle tab unswitchable", () => ((StyledTabControl.TestTabItem)simpleTabcontrol.SwitchableTabs.Skip(5).First()).SetSwitchable(false));

            AddStep("Remove all switchable tabs", () =>
            {
                var itemsForDelete = new List<TestEnum?>();

                foreach (var kvp in simpleTabcontrol.TabMap)
                {
                    if (kvp.Value.IsSwitchable)
                        itemsForDelete.Add(kvp.Key);
                }

                itemsForDelete.ForEach(item => simpleTabcontrol.RemoveItem(item));
            });

            AddAssert("Unswitchable tab still present", () => simpleTabcontrol.Items.Count == 1);
            AddAssert("Ensure selected tab is null", () => simpleTabcontrol.SelectedTab == null);
        }

        [Test]
        public void TestItemsImmediatelyUpdatedAfterAdd()
        {
            TabControlWithNoDropdown tabControl = null;

            AddStep("create tab control", () =>
            {
                tabControl = new TabControlWithNoDropdown { Size = new Vector2(200, 30) };

                foreach (var item in items)
                    tabControl.AddItem(item);
            });

            AddAssert("contained items match added items", () => tabControl.Items.SequenceEqual(items));
        }

        [Test]
        public void TestItemsAddedWhenSet()
        {
            TabControlWithNoDropdown tabControl = null;

            AddStep("create tab control", () =>
            {
                tabControl = new TabControlWithNoDropdown
                {
                    Size = new Vector2(200, 30),
                    Items = items
                };
            });

            AddAssert("contained items match added items", () => tabControl.Items.SequenceEqual(items));
        }

        [TestCase(false, null)]
        [TestCase(true, TestEnum.Test0)]
        public void TestInitialSelection(bool selectFirstByDefault, TestEnum? expectedInitialSelection)
        {
            StyledTabControl tabControl = null;

            AddStep("create tab control", () =>
            {
                tabControlContainer.Add(tabControl = new StyledTabControl
                {
                    Size = new Vector2(200, 30),
                    Items = items.Cast<TestEnum?>().ToList(),
                    SelectFirstTabByDefault = selectFirstByDefault
                });
            });

            AddUntilStep("wait for loaded", () => tabControl.IsLoaded);
            AddAssert("initial selection is correct", () => tabControl.Current.Value == expectedInitialSelection);
        }

        [TestCase(true, TestEnum.Test1, true)]
        [TestCase(false, TestEnum.Test1, true)]
        [TestCase(true, TestEnum.Test9, true)]
        [TestCase(false, TestEnum.Test9, false)]
        public void TestInitialSort(bool autoSort, TestEnum? initialItem, bool expected)
        {
            StyledTabControl tabControlWithBindable = null;
            Bindable<TestEnum?> testBindable = new Bindable<TestEnum?> { Value = initialItem };

            AddStep("create tab control", () =>
            {
                tabControlContainer.Add(tabControlWithBindable = new StyledTabControl
                {
                    Size = new Vector2(200, 20),
                    Items = items.Cast<TestEnum?>().ToList(),
                    AutoSort = autoSort,
                    Current = { BindTarget = testBindable }
                });
            });

            AddUntilStep("wait for loaded", () => tabControlWithBindable.IsLoaded);
            AddAssert($"Current selection {(expected ? "visible" : "not visible")}", () => tabControlWithBindable.SelectedTab.IsPresent == expected);
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

        public class StyledTabControl : TabControl<TestEnum?>
        {
            public new IReadOnlyDictionary<TestEnum?, TabItem<TestEnum?>> TabMap => base.TabMap;

            public new TabItem<TestEnum?> SelectedTab => base.SelectedTab;

            public new Dropdown<TestEnum?> Dropdown => base.Dropdown;

            protected override Dropdown<TestEnum?> CreateDropdown() => new StyledDropdown();

            public TabItem<TestEnum?> CreateTabItem(TestEnum? value, bool isSwitchable)
                => new TestTabItem(value);

            protected override TabItem<TestEnum?> CreateTabItem(TestEnum? value) => CreateTabItem(value, true);

            public class TestTabItem : BasicTabControl<TestEnum?>.BasicTabItem
            {
                public TestTabItem(TestEnum? value)
                    : base(value)
                {
                }

                private bool switchable = true;

                public void SetSwitchable(bool isSwitchable) => switchable = isSwitchable;

                public override bool IsSwitchable => switchable;
            }
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
            protected internal override LocalisableString Label { get; set; }

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

        private class TabControlWithNoDropdown : BasicTabControl<TestEnum>
        {
            protected override Dropdown<TestEnum> CreateDropdown() => null;
        }

        public enum TestEnum
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
