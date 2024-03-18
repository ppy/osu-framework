// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using osu.Framework.Graphics;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Input;
using osu.Framework.Testing;
using osuTK;
using osuTK.Input;

namespace osu.Framework.Tests.Visual.UserInterface
{
    public partial class TestSceneTabControlEvents : ManualInputManagerTestScene
    {
        private EventQueuesTabControl tabControl = null!;

        [SetUpSteps]
        public void SetUpSteps()
        {
            AddStep("add tab control", () => Child = tabControl = new EventQueuesTabControl
            {
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                Size = new Vector2(150, 40),
                Items = Enum.GetValues<TestEnum>(),
            });

            AddAssert("selected tab queue empty", () => tabControl.UserTabSelectionChangedQueue.Count == 0);
        }

        [Test]
        public void TestClickSendsEvent()
        {
            AddStep("click second tab", () =>
            {
                InputManager.MoveMouseTo(this.ChildrenOfType<TabItem<TestEnum>>().ElementAt(1));
                InputManager.Click(MouseButton.Left);
            });

            AddAssert("selected tab = second", () => tabControl.Current.Value == TestEnum.Second);
            AddAssert("selected tab queue has \"second\"", () => tabControl.UserTabSelectionChangedQueue.Dequeue().Value == TestEnum.Second);
        }

        [Test]
        public void TestClickSameTabDoesNotSendEvent()
        {
            AddAssert("first tab selected", () => tabControl.Current.Value == TestEnum.First);
            AddStep("click first tab", () =>
            {
                InputManager.MoveMouseTo(this.ChildrenOfType<TabItem<TestEnum>>().First());
                InputManager.Click(MouseButton.Left);
            });

            AddAssert("first tab still selected", () => tabControl.Current.Value == TestEnum.First);
            AddAssert("selected tab queue empty", () => tabControl.UserTabSelectionChangedQueue.Count == 0);
        }

        [Test]
        public void TestSelectItemMethodSendsEvent()
        {
            AddStep("call select item", () => tabControl.SelectItem(TestEnum.Second));
            AddAssert("selected tab queue has \"second\"", () => tabControl.UserTabSelectionChangedQueue.Dequeue().Value == TestEnum.Second);
        }

        [Test]
        public void TestSwitchTabMethodSendsEvent()
        {
            AddStep("set switchable", () => tabControl.IsSwitchable = true);
            AddStep("call switch tab", () => tabControl.SwitchTab(1));
            AddAssert("selected tab = second", () => tabControl.Current.Value == TestEnum.Second);
            AddAssert("selected tab queue has \"second\"", () => tabControl.UserTabSelectionChangedQueue.Dequeue().Value == TestEnum.Second);
            AddStep("call switch tab", () => tabControl.SwitchTab(-1));
            AddAssert("selected tab = second", () => tabControl.Current.Value == TestEnum.First);
            AddAssert("selected tab queue has \"second\"", () => tabControl.UserTabSelectionChangedQueue.Dequeue().Value == TestEnum.First);
        }

        [Test]
        public void TestSwitchUsingKeyBindingSendsEvent()
        {
            AddStep("set switchable", () => tabControl.IsSwitchable = true);
            AddStep("switch forward", () => InputManager.Keys(PlatformAction.DocumentNext));
            AddAssert("selected tab = second", () => tabControl.Current.Value == TestEnum.Second);
            AddAssert("selected tab queue has \"second\"", () => tabControl.UserTabSelectionChangedQueue.Dequeue().Value == TestEnum.Second);
            AddStep("switch backward", () => InputManager.Keys(PlatformAction.DocumentPrevious));
            AddAssert("selected tab = second", () => tabControl.Current.Value == TestEnum.First);
            AddAssert("selected tab queue has \"second\"", () => tabControl.UserTabSelectionChangedQueue.Dequeue().Value == TestEnum.First);
        }

        [Test]
        public void TestSwitchOnRemovalDoesNotSendEvent()
        {
            AddStep("set switchable", () => tabControl.IsSwitchable = true);
            AddStep("remove first tab", () => tabControl.RemoveItem(TestEnum.First));

            AddAssert("selected tab = second", () => tabControl.Current.Value == TestEnum.Second);
            AddAssert("selected tab queue still empty", () => tabControl.UserTabSelectionChangedQueue.Count == 0);
        }

        [Test]
        public void TestBindableChangeDoesNotSendEvent()
        {
            AddStep("set selected tab = second", () => tabControl.Current.Value = TestEnum.Second);
            AddAssert("selected tab queue still empty", () => tabControl.UserTabSelectionChangedQueue.Count == 0);
        }

        [TearDownSteps]
        public void TearDownSteps()
        {
            AddAssert("selected tab queue empty", () => tabControl.UserTabSelectionChangedQueue.Count == 0);
        }

        private partial class EventQueuesTabControl : BasicTabControl<TestEnum>
        {
            public readonly Queue<TabItem<TestEnum>> UserTabSelectionChangedQueue = new Queue<TabItem<TestEnum>>();

            protected override void OnUserTabSelectionChanged(TabItem<TestEnum> item) => UserTabSelectionChangedQueue.Enqueue(item);
        }

        private enum TestEnum
        {
            First,
            Second,
        }
    }
}
