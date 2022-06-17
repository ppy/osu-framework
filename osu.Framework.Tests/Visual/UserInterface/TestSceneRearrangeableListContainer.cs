// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
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
        private TestRearrangeableList list;

        private Container listContainer;

        [SetUp]
        public void Setup() => Schedule(() =>
        {
            Child = listContainer = new Container
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
                int localI = i;

                addItems(1);
                AddAssert($"last item is \"{i}\"", () => list.ChildrenOfType<RearrangeableListItem<int>>().Last().Model == localI);
            }
        }

        [Test]
        public void TestBindBeforeLoad()
        {
            AddStep("create list", () => list = new TestRearrangeableList { RelativeSizeAxes = Axes.Both });
            AddStep("bind list to items", () => list.Items.BindTo(new BindableList<int>(new[] { 1, 2, 3 })));
            AddStep("add list to hierarchy", () => listContainer.Add(list));
        }

        [Test]
        public void TestAddDuplicateItemsFails()
        {
            const int item = 1;

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
            const int item_count = 5;

            addItems(item_count);

            List<Drawable> items = null;

            AddStep("get item references", () => items = new List<Drawable>(list.ItemMap.Values.ToList()));

            for (int i = 0; i < item_count; i++)
            {
                int localI = i;

                AddStep($"remove item \"{i}\"", () => list.Items.Remove(localI));
                AddAssert($"first item is not \"{i}\"", () => list.ChildrenOfType<RearrangeableListItem<int>>().FirstOrDefault()?.Model != localI);
            }

            AddUntilStep("removed items were disposed", () => items.Count(i => i.IsDisposed) == item_count);
        }

        [Test]
        public void TestClearItems()
        {
            addItems(5);

            AddStep("clear items", () => list.Items.Clear());

            AddAssert("no items contained", () => !list.ChildrenOfType<RearrangeableListItem<string>>().Any());
        }

        [Test]
        public void TestRearrangeByDrag()
        {
            addItems(5);

            addDragSteps(1, 4, new[] { 0, 2, 3, 4, 1 });
            addDragSteps(1, 3, new[] { 0, 2, 1, 3, 4 });
            addDragSteps(0, 3, new[] { 2, 1, 3, 0, 4 });
            addDragSteps(3, 4, new[] { 2, 1, 0, 4, 3 });
            addDragSteps(4, 2, new[] { 4, 2, 1, 0, 3 });
            addDragSteps(2, 4, new[] { 2, 4, 1, 0, 3 });
        }

        [Test]
        public void TestRearrangeByDragWithHiddenItems()
        {
            addItems(6);

            AddStep("hide item zero", () => list.ListContainer.First(i => i.Model == 0).Hide());

            addDragSteps(2, 5, new[] { 0, 1, 3, 4, 5, 2 });
            addDragSteps(2, 4, new[] { 0, 1, 3, 2, 4, 5 });
            addDragSteps(1, 4, new[] { 0, 3, 2, 4, 1, 5 });
            addDragSteps(4, 5, new[] { 0, 3, 2, 1, 5, 4 });
            addDragSteps(5, 3, new[] { 0, 5, 3, 2, 1, 4 });
            addDragSteps(3, 5, new[] { 0, 3, 5, 2, 1, 4 });
        }

        [Test]
        public void TestRearrangeByDragAfterRemoval()
        {
            addItems(5);

            addDragSteps(0, 4, new[] { 1, 2, 3, 4, 0 });
            addDragSteps(1, 4, new[] { 2, 3, 4, 1, 0 });
            addDragSteps(2, 4, new[] { 3, 4, 2, 1, 0 });
            addDragSteps(3, 4, new[] { 4, 3, 2, 1, 0 });

            AddStep("remove 3 and 2", () =>
            {
                list.Items.Remove(3);
                list.Items.Remove(2);
            });

            addDragSteps(4, 0, new[] { 1, 0, 4 });
            addDragSteps(0, 1, new[] { 0, 1, 4 });
            addDragSteps(4, 0, new[] { 4, 0, 1 });
        }

        [Test]
        public void TestRemoveAfterDragScrollThenTryRearrange()
        {
            addItems(5);

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
            addItems(100);

            AddStep("scroll to item 50", () => list.ScrollTo(50));

            float scrollPosition = 0;
            AddStep("get scroll position", () => scrollPosition = list.ScrollPosition);

            AddStep("move to 52", () =>
            {
                InputManager.MoveMouseTo(getDragger(52));
                InputManager.PressButton(MouseButton.Left);
            });

            AddStep("drag to 0", () => InputManager.MoveMouseTo(getDragger(0), new Vector2(0, -1)));

            AddUntilStep("scrolling up", () => list.ScrollPosition < scrollPosition);
            AddUntilStep("52 is the first item", () => list.Items.First() == 52);

            AddStep("drag to 99", () => InputManager.MoveMouseTo(getDragger(99), new Vector2(0, 1)));

            AddUntilStep("scrolling down", () => list.ScrollPosition > scrollPosition);
            AddUntilStep("52 is the last item", () => list.Items.Last() == 52);
        }

        [Test]
        public void TestRearrangeWhileAddingItems()
        {
            addItems(2);

            AddStep("grab item 0", () =>
            {
                InputManager.MoveMouseTo(getDragger(0));
                InputManager.PressButton(MouseButton.Left);
            });

            AddStep("move to bottom", () => InputManager.MoveMouseTo(list.ToScreenSpace(list.LayoutRectangle.BottomLeft) + new Vector2(0, 10)));

            addItems(10);

            AddUntilStep("0 is the last item", () => list.Items.Last() == 0);
        }

        [Test]
        public void TestRearrangeWhileRemovingItems()
        {
            addItems(50);

            AddStep("grab item 0", () =>
            {
                InputManager.MoveMouseTo(getDragger(0));
                InputManager.PressButton(MouseButton.Left);
            });

            AddStep("move to bottom", () => InputManager.MoveMouseTo(list.ToScreenSpace(list.LayoutRectangle.BottomLeft) + new Vector2(0, 20)));

            int lastItem = 49;

            AddRepeatStep("remove item", () =>
            {
                list.Items.Remove(lastItem--);
            }, 25);

            AddUntilStep("0 is the last item", () => list.Items.Last() == 0);

            AddRepeatStep("remove item", () =>
            {
                list.Items.Remove(lastItem--);
            }, 25);

            AddStep("release button", () => InputManager.ReleaseButton(MouseButton.Left));
        }

        [Test]
        public void TestNotScrolledToTopOnRemove()
        {
            addItems(100);

            float scrollPosition = 0;
            AddStep("scroll to item 50", () =>
            {
                list.ScrollTo(50);
                scrollPosition = list.ScrollPosition;
            });

            AddStep("remove item 50", () => list.Items.Remove(50));

            AddAssert("scroll hasn't changed", () => list.ScrollPosition == scrollPosition);
        }

        [Test]
        public void TestRemoveDuringLoadAndReAdd()
        {
            TestDelayedLoadRearrangeableList delayedList = null;

            AddStep("create list", () => Child = delayedList = new TestDelayedLoadRearrangeableList());

            AddStep("add item 1", () => delayedList.Items.Add(1));
            AddStep("remove item 1", () => delayedList.Items.Remove(1));
            AddStep("add item 1", () => delayedList.Items.Add(1));
            AddStep("allow load", () => delayedList.AllowLoad.Release(100));

            AddUntilStep("only one item", () => delayedList.ChildrenOfType<BasicRearrangeableListItem<int>>().Count() == 1);
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
                () => list.Items.SequenceEqual(sequence.Select(value => value)));
        }

        private void addItems(int count)
        {
            AddStep($"add {count} item(s)", () =>
            {
                int startId = list.Items.Count == 0 ? 0 : list.Items.Max() + 1;

                for (int i = 0; i < count; i++)
                    list.Items.Add(startId + i);
            });

            AddUntilStep("wait for items to load", () => list.ItemMap.Values.All(i => i.IsLoaded));
        }

        private RearrangeableListItem<int> getItem(int index)
            => list.ChildrenOfType<RearrangeableListItem<int>>().First(i => i.Model == index);

        private BasicRearrangeableListItem<int>.Button getDragger(int index)
            => list.ChildrenOfType<BasicRearrangeableListItem<int>>().First(i => i.Model == index)
                   .ChildrenOfType<BasicRearrangeableListItem<int>.Button>().First();

        private class TestRearrangeableList : BasicRearrangeableListContainer<int>
        {
            public float ScrollPosition => ScrollContainer.Current;

            public new IReadOnlyDictionary<int, RearrangeableListItem<int>> ItemMap => base.ItemMap;

            public new FillFlowContainer<RearrangeableListItem<int>> ListContainer => base.ListContainer;

            public void ScrollTo(int item)
                => ScrollContainer.ScrollTo(this.ChildrenOfType<BasicRearrangeableListItem<int>>().First(i => i.Model == item), false);
        }

        private class TestDelayedLoadRearrangeableList : BasicRearrangeableListContainer<int>
        {
            public readonly SemaphoreSlim AllowLoad = new SemaphoreSlim(0, 100);

            protected override BasicRearrangeableListItem<int> CreateBasicItem(int item) => new TestRearrangeableListItem(item, AllowLoad);

            private class TestRearrangeableListItem : BasicRearrangeableListItem<int>
            {
                private readonly SemaphoreSlim allowLoad;

                public TestRearrangeableListItem(int item, SemaphoreSlim allowLoad)
                    : base(item, false)
                {
                    this.allowLoad = allowLoad;
                }

                [BackgroundDependencyLoader]
                private void load()
                {
                    if (!allowLoad.Wait(TimeSpan.FromSeconds(10)))
                        throw new TimeoutException();
                }
            }
        }
    }
}
