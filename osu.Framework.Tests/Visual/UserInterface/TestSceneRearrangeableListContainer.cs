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
            typeof(RearrangeableListContainer<>),
            typeof(RearrangeableListItem<>)
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

                AddStep($"add item \"{itemString}\"", () => list.Items.Add(itemString));
                AddAssert($"last item is \"{itemString}\"", () => list.ChildrenOfType<RearrangeableListItem<string>>().Last().Model == itemString);
            }
        }

        [Test]
        public void TestAddDuplicateItemsFails()
        {
            const string item = "1";

            AddStep("add item 1", () => list.Items.Add(item));

            AddAssert("add same item throws", () =>
            {
                try
                {
                    list.Items.Add(item);
                    return false;
                }
                catch (InvalidOperationException)
                {
                    return true;
                }
            });
        }

        [Test]
        public void TestRemoveItem()
        {
            AddStep("add 5 items", () =>
            {
                for (int i = 0; i < 5; i++)
                    list.Items.Add(i.ToString());
            });

            for (int i = 0; i < 5; i++)
            {
                string itemString = i.ToString();

                AddStep($"remove item \"{itemString}\"", () => list.Items.Remove(itemString));
                AddAssert($"first item is not \"{itemString}\"", () => list.ChildrenOfType<RearrangeableListItem<string>>().FirstOrDefault()?.Model != itemString);
            }
        }

        [Test]
        public void TestClearItems()
        {
            AddStep("add 5 items", () =>
            {
                for (int i = 0; i < 5; i++)
                    list.Items.Add(i.ToString());
            });

            AddStep("clear items", () => list.Items.Clear());

            AddAssert("no items contained", () => !list.ChildrenOfType<RearrangeableListItem<string>>().Any());
        }

        [Test]
        public void TestRearrangeByDrag()
        {
            AddStep("add 5 items", () =>
            {
                for (int i = 0; i < 5; i++)
                    list.Items.Add(i.ToString());
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
                    list.Items.Add(i.ToString());
            });

            addDragSteps(0, 4, new[] { 1, 2, 3, 4, 0 });
            addDragSteps(1, 4, new[] { 2, 3, 4, 1, 0 });
            addDragSteps(2, 4, new[] { 3, 4, 2, 1, 0 });
            addDragSteps(3, 4, new[] { 4, 3, 2, 1, 0 });

            AddStep("remove 3 and 2", () =>
            {
                list.Items.Remove("3");
                list.Items.Remove("2");
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
                    list.Items.Add(i.ToString());
            });

            // Scroll
            AddStep("move mouse to first item", () => InputManager.MoveMouseTo(getItem(0)));
            AddStep("begin a drag", () => InputManager.PressButton(MouseButton.Left));
            AddStep("move the mouse", () => InputManager.MoveMouseTo(getItem(0), new Vector2(0, 30)));
            AddStep("end the drag", () => InputManager.ReleaseButton(MouseButton.Left));

            AddStep("remove all but one item", () =>
            {
                for (int i = 0; i < 4; i++)
                    list.Items.Remove(getItem(i).Model);
            });

            // Drag
            AddStep("move mouse to first dragger", () => InputManager.MoveMouseTo(getDragger(4)));
            AddStep("begin a drag", () => InputManager.PressButton(MouseButton.Left));
            AddStep("move the mouse", () => InputManager.MoveMouseTo(getDragger(4), new Vector2(0, 30)));
            AddStep("end the drag", () => InputManager.ReleaseButton(MouseButton.Left));
        }

        [Test]
        public void TestScrolledWhenDraggedToBoundaries()
        {
            AddStep("add 100 items", () =>
            {
                for (int i = 0; i < 100; i++)
                    list.Items.Add(i.ToString());
            });

            AddStep("scroll to item 50", () => list.ScrollTo("50"));

            float scrollPosition = 0;
            AddStep("get scroll position", () => scrollPosition = list.ScrollPosition);

            AddStep("move to 52", () =>
            {
                InputManager.MoveMouseTo(getDragger(52));
                InputManager.PressButton(MouseButton.Left);
            });

            AddStep("drag to 0", () => InputManager.MoveMouseTo(getDragger(0), new Vector2(0, -1)));

            AddUntilStep("scrolling up", () => list.ScrollPosition < scrollPosition);
            AddUntilStep("52 is the first item", () => list.Items.First() == "52");

            AddStep("drag to 99", () => InputManager.MoveMouseTo(getDragger(99), new Vector2(0, 1)));

            AddUntilStep("scrolling down", () => list.ScrollPosition > scrollPosition);
            AddUntilStep("52 is the last item", () => list.Items.Last() == "52");
        }

        [Test]
        public void TestRearrangeWhileAddingItems()
        {
            int i = 0;

            AddStep("add two items", () =>
            {
                i = 0;

                list.Items.Add(i++.ToString());
                list.Items.Add(i++.ToString());
            });

            AddStep("grab item 0", () =>
            {
                InputManager.MoveMouseTo(getDragger(0));
                InputManager.PressButton(MouseButton.Left);
            });

            AddStep("move to bottom", () => InputManager.MoveMouseTo(list.ToScreenSpace(list.LayoutRectangle.BottomLeft) + new Vector2(0, 10)));

            AddRepeatStep("add items", () =>
            {
                list.Items.Add(i++.ToString());
            }, 10);

            AddUntilStep("0 is the last item", () => list.Items.Last() == "0");
        }

        [Test]
        public void TestRearrangeWhileRemovingItems()
        {
            int lastItem = 49;

            AddStep("add 50 items", () =>
            {
                lastItem = 49;

                for (int i = 0; i < 50; i++)
                    list.Items.Add(i.ToString());
            });

            AddStep("grab item 0", () =>
            {
                InputManager.MoveMouseTo(getDragger(0));
                InputManager.PressButton(MouseButton.Left);
            });

            AddStep("move to bottom", () => InputManager.MoveMouseTo(list.ToScreenSpace(list.LayoutRectangle.BottomLeft) + new Vector2(0, 10)));

            AddRepeatStep("remove item", () =>
            {
                list.Items.Remove(lastItem--.ToString());
            }, 25);

            AddUntilStep("0 is the last item", () => list.Items.Last() == "0");

            AddRepeatStep("remove item", () =>
            {
                list.Items.Remove(lastItem--.ToString());
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

            AddStep($"drag to {to}", () =>
            {
                var fromDragger = getDragger(from);
                var toDragger = getDragger(to);

                InputManager.MoveMouseTo(getDragger(to), fromDragger.ScreenSpaceDrawQuad.TopLeft.Y < toDragger.ScreenSpaceDrawQuad.TopLeft.Y ? new Vector2(0, 1) : new Vector2(0, -1));
            });

            assertSequence(expectedSequence);

            AddStep("release button", () => InputManager.ReleaseButton(MouseButton.Left));
        }

        private void assertSequence(params int[] sequence)
        {
            AddAssert($"sequence is {string.Join(", ", sequence)}",
                () => list.Items.SequenceEqual(sequence.Select(value => value.ToString())));
        }

        private RearrangeableListItem<string> getItem(int index)
            => list.ChildrenOfType<RearrangeableListItem<string>>().First(i => i.Model == index.ToString());

        private BasicRearrangeableListItem<string>.Button getDragger(int index)
            => list.ChildrenOfType<BasicRearrangeableListItem<string>>().First(i => i.Model == index.ToString())
                   .ChildrenOfType<BasicRearrangeableListItem<string>.Button>().First();

        private class TestRearrangeableList : BasicRearrangeableListContainer<string>
        {
            public float ScrollPosition => ScrollContainer.Current;

            public void ScrollTo(string item)
                => ScrollContainer.ScrollTo(this.ChildrenOfType<BasicRearrangeableListItem<string>>().First(i => i.Model == item), false);
        }
    }
}
