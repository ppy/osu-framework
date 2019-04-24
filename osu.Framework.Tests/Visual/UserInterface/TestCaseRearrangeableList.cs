// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using NUnit.Framework;
using osu.Framework.Graphics;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Testing;
using osuTK;
using osuTK.Input;

namespace osu.Framework.Tests.Visual.UserInterface
{
    public class TestCaseRearrangeableList : ManualInputManagerTestCase
    {
        private readonly TestBasicRearrangableList list;

        public override IReadOnlyList<Type> RequiredTypes => new[]
        {
            typeof(TestBasicRearrangableList),
            typeof(BasicRearrangableList),
            typeof(BasicRearrangableList.BasicDrawableRearrangeableListItem),
        };

        public TestCaseRearrangeableList()
        {
            Add(list = new TestBasicRearrangableList
            {
                Width = 0.3f,
            });
        }

        [SetUp]
        public override void SetUp() => Schedule(() =>
        {
            list.Clear();
            for (int i = 0; i < 10; i++)
                list.AddItem(new BasicRearrangeableItem($"test {i}"));
        });

        [Test]
        public void SortingTests()
        {
            // drag item down
            AddStep("Hover item", () => { InputManager.MoveMouseTo(list.GetChild(0).ToScreenSpace(new Vector2(10, list.GetChildSize(0).Y * 0.5f))); });
            AddStep("Click", () => { InputManager.PressButton(MouseButton.Left); });
            AddStep("Drag downward", () =>
            {
                // calculate the offset based on the first 3 items in the list, plus spacing
                var dragOffset = list.GetChildSize(0).Y + list.GetChildSize(1).Y + list.GetChildSize(2).Y * 0.75f + list.Spacing.Y * 2;
                InputManager.MoveMouseTo(list.GetChild(0).ToScreenSpace(new Vector2(10, dragOffset)));
            });
            AddStep("Release", () => { InputManager.ReleaseButton(MouseButton.Left); });
            AddAssert("Ensure item is now third", () => list.GetLayoutPosition(list.GetChild(0)) == 2);

            // drag item back up
            AddStep("Hover item", () => { InputManager.MoveMouseTo(list.GetChild(0).ToScreenSpace(new Vector2(10, list.GetChildSize(0).Y * 0.5f))); });
            AddStep("Click", () => { InputManager.PressButton(MouseButton.Left); });
            AddStep("Drag upward", () =>
            {
                // calculate the offset based on the 2 items above it in the list, plus spacing
                var dragOffset = list.GetChildSize(1).Y + list.GetChildSize(2).Y * 0.75f + list.Spacing.Y * 2;
                InputManager.MoveMouseTo(list.GetChild(0).ToScreenSpace(new Vector2(10, -dragOffset)));
            });
            AddStep("Release", () => { InputManager.ReleaseButton(MouseButton.Left); });
            AddAssert("Ensure item is now first again", () => list.GetLayoutPosition(list.GetChild(0)) == 0);
        }

        private class TestBasicRearrangableList : BasicRearrangableList
        {
            public IReadOnlyList<Drawable> Children => ListContainer.Children;

            public float GetLayoutPosition(BasicDrawableRearrangeableListItem d) => ListContainer.GetLayoutPosition(d);

            public BasicDrawableRearrangeableListItem GetChild(int index) => (BasicDrawableRearrangeableListItem)Children[index];

            public Vector2 GetChildSize(int index) => GetChild(index).DrawSize;
        }
    }
}
