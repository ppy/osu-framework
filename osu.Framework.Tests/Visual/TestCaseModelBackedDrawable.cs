// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using OpenTK;
using OpenTK.Graphics;
using osu.Framework.Testing;
using System.Threading;

namespace osu.Framework.Tests.Visual
{
    public class TestCaseModelBackedDrawable : TestCase
    {
        public TestCaseModelBackedDrawable()
        {
            TestModelBackedDrawable updateContainer;
            PlaceholderTestModelBackedDrawable placeholderContainer;
            DelayedTestModelBackedDrawable delayedContainer;

            AddRange(new Drawable[]
            {
                updateContainer = new TestModelBackedDrawable
                {
                    Position = new Vector2(50, 50),
                    Size = new Vector2(100, 100)
                },
                placeholderContainer = new PlaceholderTestModelBackedDrawable
                {
                    Position = new Vector2(50, 250),
                    Size = new Vector2(100, 100)
                },
                delayedContainer = new DelayedTestModelBackedDrawable
                {
                    Position = new Vector2(50, 450),
                    Size = new Vector2(100, 100)
                }
            });


            addNullTest("No PH", updateContainer, false);
            addItemTest("No PH", updateContainer, 0);
            addItemTest("No PH", updateContainer, 1);
            addNullTest("No PH", updateContainer, false);

            addNullTest("PH", placeholderContainer, true);
            addItemTest("PH", placeholderContainer, 0);
            addItemTest("PH", placeholderContainer, 1);
            addNullTest("PH", placeholderContainer, true);

            AddStep("D: Set item null", () => delayedContainer.Item = null);
            AddStep("D: Set item with delay", () => delayedContainer.Item = new TestItem(0));
            AddAssert("D: Test load not triggered", () => !delayedContainer.LoadTriggered);
            AddUntilStep(() => delayedContainer.LoadTriggered, "D: Wait until load triggered");
        }

        private void addNullTest(string prefix, TestModelBackedDrawable container, bool expectPlaceholder)
        {
            AddStep($"{prefix}: Set null", () => container.Item = null);
            if (expectPlaceholder)
                AddAssert($"{prefix}: Check null with PH", () => container.DisplayedDrawable == null && (container.PlaceholderDrawable?.Alpha ?? 0) > 0);
            else
            {
                AddAssert($"{prefix}: Test load triggered", () => container.LoadTriggered);
                AddUntilStep(() => container.NextDrawable == null, $"{prefix}: Wait until loaded");
                AddAssert($"{prefix}: Check non-null no PH", () => container.VisibleItemId == -1 && container.PlaceholderDrawable == null);
            }
        }

        private void addItemTest(string prefix, TestModelBackedDrawable container, int itemNumber)
        {
            AddStep($"{prefix} Set item {itemNumber}", () => container.Item = new TestItem(itemNumber));
            AddUntilStep(() => container.NextDrawable == null, $"{prefix} wait until loaded");
            AddAssert($"{prefix} Check item {itemNumber}", () => container.VisibleItemId == itemNumber);
        }

        private class TestItem
        {
            public readonly int ItemId;

            public TestItem(int itemId)
            {
                ItemId = itemId;
            }
        }

        private class TestItemDrawable : SpriteText
        {
            public readonly int ItemId;

            public TestItemDrawable(TestItem item)
            {
                ItemId = item?.ItemId ?? -1;
                Position = new Vector2(10, 10);
                Text = item == null ? "No Item" : $"Item {item.ItemId}";
            }

            [BackgroundDependencyLoader]
            private void load()
            {
                // delay
                Thread.Sleep((int)(500 / Clock.Rate));
            }
        }

        private class TestModelBackedDrawable : ModelBackedDrawable<TestItem>
        {
            public TestItem Item { get => Source; set => Source = value; }

            public int VisibleItemId => (DisplayedDrawable as TestItemDrawable)?.ItemId ?? -1;

            public TestModelBackedDrawable()
                : base((lhs, rhs) => lhs?.ItemId == rhs?.ItemId ? 0 : -1)
            {
                Add(new Box
                {
                    RelativeSizeAxes = Axes.Both,
                    Alpha = 0,
                    AlwaysPresent = true
                });
                BorderColour = Color4.White;
                BorderThickness = 2;
                Masking = true;
            }

            protected override Drawable CreateDrawable(TestItem item) => new TestItemDrawable(item);
        }

        private class PlaceholderTestModelBackedDrawable : TestModelBackedDrawable
        {
            protected override Drawable CreateDrawable(TestItem item) => item == null ? null : new TestItemDrawable(item);

            protected override Drawable CreatePlaceholder() => new Box { Colour = Color4.Blue };
        }

        private class DelayedTestModelBackedDrawable : PlaceholderTestModelBackedDrawable
        {
            protected override double LoadDelay => 1000 / Clock.Rate;
        }
    }
}
