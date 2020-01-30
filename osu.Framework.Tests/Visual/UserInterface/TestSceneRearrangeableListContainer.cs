// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Testing;
using osuTK;
using osuTK.Input;

namespace osu.Framework.Tests.Visual.UserInterface
{
    public class TestSceneRearrangeableListContainer : ManualInputManagerTestScene
    {
        public override IReadOnlyList<Type> RequiredTypes => new[]
        {
            typeof(BasicRearrangeableListContainer<>),
            typeof(RearrangeableListContainer<>)
        };

        private TestRearrangeableList list;

        [SetUp]
        public void Setup() => Schedule(() =>
        {
            Child = new Container
            {
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                Size = new Vector2(500, 300),
                Child = list = new TestRearrangeableList { RelativeSizeAxes = Axes.Both }
            };
        });

        [Test]
        public void TestAddItem()
        {
            for (int i = 0; i < 5; i++)
            {
                string itemString = i.ToString();

                AddStep($"add item \"{itemString}\"", () => list.AddItem(itemString));
                AddAssert($"last item is \"{itemString}\"", () => list.ChildrenOfType<DrawableRearrangeableListItem<string>>().Last().Model == itemString);
            }
        }

        [Test]
        public void TestRemoveItem()
        {
            AddStep("add 5 items", () =>
            {
                for (int i = 0; i < 5; i++)
                    list.AddItem(i.ToString());
            });

            for (int i = 0; i < 5; i++)
            {
                string itemString = i.ToString();

                AddStep($"remove item \"{itemString}\"", () => list.RemoveItem(itemString));
                AddAssert($"first item is not \"{itemString}\"", () => list.ChildrenOfType<DrawableRearrangeableListItem<string>>().FirstOrDefault()?.Model != itemString);
            }
        }

        [Test]
        public void TestClearItems()
        {
            AddStep("add 5 items", () =>
            {
                for (int i = 0; i < 5; i++)
                    list.AddItem(i.ToString());
            });

            AddStep("clear items", () => list.ClearItems());

            AddAssert("no items contained", () => !list.ChildrenOfType<DrawableRearrangeableListItem<string>>().Any());
        }

        [Test]
        public void TestRearrangeByDrag()
        {
            AddStep("add 5 items", () =>
            {
                for (int i = 0; i < 5; i++)
                    list.AddItem(i.ToString());
            });

            addDragSteps(1, 4, new[] { 0, 2, 3, 4, 1 });
            addDragSteps(1, 3, new[] { 0, 2, 1, 3, 4 });
            addDragSteps(0, 3, new[] { 2, 1, 3, 0, 4 });
            addDragSteps(3, 4, new[] { 2, 1, 0, 4, 3 });
            addDragSteps(4, 2, new[] { 4, 2, 1, 0, 3 });
            addDragSteps(2, 4, new[] { 2, 4, 1, 0, 3 });
        }

        [Test]
        public void TestRearrangeByDragAfterRemoval()
        {
            AddStep("add 5 items", () =>
            {
                for (int i = 0; i < 5; i++)
                    list.AddItem(i.ToString());
            });

            addDragSteps(0, 4, new[] { 1, 2, 3, 4, 0 });
            addDragSteps(1, 4, new[] { 2, 3, 4, 1, 0 });
            addDragSteps(2, 4, new[] { 3, 4, 2, 1, 0 });
            addDragSteps(3, 4, new[] { 4, 3, 2, 1, 0 });

            AddStep("remove 3 and 2", () =>
            {
                list.RemoveItem("3");
                list.RemoveItem("2");
            });

            addDragSteps(4, 0, new[] { 1, 0, 4 });
            addDragSteps(0, 1, new[] { 0, 1, 4 });
            addDragSteps(4, 0, new[] { 4, 0, 1 });
        }

        [Test]
        public void TestRemoveAfterDragScrollThenTryRearrange()
        {
            AddStep("add 5 items", () =>
            {
                for (int i = 0; i < 5; i++)
                    list.AddItem(i.ToString());
            });

            AddStep("move mouse to first item", () => InputManager.MoveMouseTo(getItem(0)));
            AddStep("begin a drag", () => InputManager.PressButton(MouseButton.Left));
            AddStep("move the mouse", () => InputManager.MoveMouseTo(getItem(0).ToScreenSpace(getItem(0).LayoutRectangle.Centre + new Vector2(0, 30))));
            AddStep("end the drag", () => InputManager.ReleaseButton(MouseButton.Left));

            AddStep("remove all but one item", () =>
            {
                for (int i = 0; i < 4; i++)
                    list.RemoveItem(getItem(i).Model);
            });

            AddStep("move mouse to first dragger", () => InputManager.MoveMouseTo(getDragger(4)));
            AddStep("begin a drag", () => InputManager.PressButton(MouseButton.Left));
            AddStep("move the mouse", () => InputManager.MoveMouseTo(getDragger(4).ToScreenSpace(getDragger(4).LayoutRectangle.Centre + new Vector2(0, 30))));
            AddStep("end the drag", () => InputManager.ReleaseButton(MouseButton.Left));
        }

        [Test]
        public void TestScrolledWhenDraggedToBoundaries()
        {
            AddStep("add 100 items", () =>
            {
                for (int i = 0; i < 100; i++)
                    list.AddItem(i.ToString());
            });

            AddStep("scroll to item 50", () => list.ScrollTo("50"));

            float scrollPosition = 0;
            AddStep("get scroll position", () => scrollPosition = list.ScrollPosition);

            AddStep("move to 52", () =>
            {
                InputManager.MoveMouseTo(getDragger(52));
                InputManager.PressButton(MouseButton.Left);
            });

            AddStep("drag to 0", () => InputManager.MoveMouseTo(getDragger(0)));
            AddUntilStep("scrolling up", () => list.ScrollPosition < scrollPosition);
            AddUntilStep("52 is the first item", () => list.ArrangedItems.First() == "52");

            AddStep("drag to 99", () => InputManager.MoveMouseTo(getDragger(99)));
            AddUntilStep("scrolling down", () => list.ScrollPosition > scrollPosition);
            AddUntilStep("52 is the last item", () => list.ArrangedItems.Last() == "52");
        }

        [Test]
        public void TestRearrangeWhileAddingItems()
        {
            int i = 0;

            AddStep("add two items", () =>
            {
                i = 0;

                list.AddItem(i++.ToString());
                list.AddItem(i++.ToString());
            });

            AddStep("grab item 0", () =>
            {
                InputManager.MoveMouseTo(getDragger(0));
                InputManager.PressButton(MouseButton.Left);
            });

            AddStep("move to bottom", () => InputManager.MoveMouseTo(list.ToScreenSpace(list.LayoutRectangle.BottomLeft)));

            AddRepeatStep("add items", () =>
            {
                list.AddItem(i++.ToString());
            }, 10);

            AddUntilStep("0 is the last item", () => list.ArrangedItems.Last() == "0");
        }

        [Test]
        public void TestRearrangeWhileRemovingItems()
        {
            int lastItem = 49;

            AddStep("add 50 items", () =>
            {
                lastItem = 49;

                for (int i = 0; i < 50; i++)
                    list.AddItem(i.ToString());
            });

            AddStep("grab item 0", () =>
            {
                InputManager.MoveMouseTo(getDragger(0));
                InputManager.PressButton(MouseButton.Left);
            });

            AddStep("move to bottom", () => InputManager.MoveMouseTo(list.ToScreenSpace(list.LayoutRectangle.BottomLeft)));

            AddRepeatStep("remove item", () =>
            {
                list.RemoveItem(lastItem--.ToString());
            }, 25);

            AddUntilStep("0 is the last item", () => list.ArrangedItems.Last() == "0");

            AddRepeatStep("remove item", () =>
            {
                list.RemoveItem(lastItem--.ToString());
            }, 25);

            AddStep("release button", () => InputManager.ReleaseButton(MouseButton.Left));
        }

        private void addDragSteps(int from, int to, int[] expectedSequence)
        {
            AddStep($"move to {from}", () =>
            {
                InputManager.MoveMouseTo(getDragger(from));
                InputManager.PressButton(MouseButton.Left);
            });

            AddStep($"drag to {to}", () => InputManager.MoveMouseTo(getDragger(to)));

            assertSequence(expectedSequence);

            AddStep("release button", () => InputManager.ReleaseButton(MouseButton.Left));
        }

        private void assertSequence(params int[] sequence)
        {
            AddAssert($"sequence is {string.Join(", ", sequence)}",
                () => list.ArrangedItems.SequenceEqual(sequence.Select(value => value.ToString())));
        }

        private DrawableRearrangeableListItem<string> getItem(int index)
            => list.ChildrenOfType<DrawableRearrangeableListItem<string>>().First(i => i.Model == index.ToString());

        private BasicDrawableRearrangeableListItem<string>.Button getDragger(int index)
            => list.ChildrenOfType<BasicDrawableRearrangeableListItem<string>>().First(i => i.Model == index.ToString())
                   .ChildrenOfType<BasicDrawableRearrangeableListItem<string>.Button>().First();

        private class TestRearrangeableList : BasicRearrangeableListContainer<string>
        {
            public float ScrollPosition => ScrollContainer.Current;

            public void ScrollTo(string item)
                => ScrollContainer.ScrollTo(this.ChildrenOfType<BasicDrawableRearrangeableListItem<string>>().First(i => i.Model == item), false);
        }
    }
}
