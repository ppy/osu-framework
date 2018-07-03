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
            TestModelBackedDrawable modelBackedDrawable;
            PlaceholderTestModelBackedDrawable placeholderModelBackedDrawable;
            DelayedTestModelBackedDrawable delayedModelBackedDrawable;

            AddRange(new Drawable[]
            {
                modelBackedDrawable = new TestModelBackedDrawable
                {
                    Position = new Vector2(50, 50),
                    Size = new Vector2(100, 100)
                },
                placeholderModelBackedDrawable = new PlaceholderTestModelBackedDrawable
                {
                    Position = new Vector2(50, 250),
                    Size = new Vector2(100, 100)
                },
                delayedModelBackedDrawable = new DelayedTestModelBackedDrawable
                {
                    Position = new Vector2(50, 450),
                    Size = new Vector2(100, 100)
                }
            });

            addNullTest("No PH", modelBackedDrawable, false);
            addItemTest("No PH", modelBackedDrawable, 0);
            addItemTest("No PH", modelBackedDrawable, 1);
            addNullTest("No PH", modelBackedDrawable, false);

            addNullTest("PH", placeholderModelBackedDrawable, true);
            addItemTest("PH", placeholderModelBackedDrawable, 0);
            addItemTest("PH", placeholderModelBackedDrawable, 1);
            addNullTest("PH", placeholderModelBackedDrawable, true);

            AddStep("D: Set item null", () => delayedModelBackedDrawable.Item = null);
            AddStep("D: Set item with delay", () => delayedModelBackedDrawable.Item = new TestItem(0));
            AddAssert("D: Test load not triggered", () => !delayedModelBackedDrawable.LoadTriggered);
            AddUntilStep(() => delayedModelBackedDrawable.LoadTriggered, "D: Wait until load triggered");
        }

        private void addNullTest(string prefix, TestModelBackedDrawable drawable, bool expectPlaceholder)
        {
            AddStep($"{prefix}: Set null", () => drawable.Item = null);
            if (expectPlaceholder)
                AddAssert($"{prefix}: Check null with PH", () => drawable.DisplayedDrawable == null && (drawable.PlaceholderDrawable?.Alpha ?? 0) > 0);
            else
            {
                AddAssert($"{prefix}: Test load triggered", () => drawable.LoadTriggered);
                AddUntilStep(() => drawable.NextDrawable == null, $"{prefix}: Wait until loaded");
                AddAssert($"{prefix}: Check non-null no PH", () => drawable.VisibleItemId == -1 && drawable.PlaceholderDrawable == null);
            }
        }

        private void addItemTest(string prefix, TestModelBackedDrawable drawable, int itemNumber)
        {
            AddStep($"{prefix} Set item {itemNumber}", () => drawable.Item = new TestItem(itemNumber));
            AddUntilStep(() => drawable.NextDrawable == null, $"{prefix} wait until loaded");
            AddAssert($"{prefix} Check item {itemNumber}", () => drawable.VisibleItemId == itemNumber);
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
            public TestItem Item { get => Model; set => Model = value; }

            public int VisibleItemId => (DisplayedDrawable as TestItemDrawable)?.ItemId ?? -1;

            public TestModelBackedDrawable()
                : base((lhs, rhs) => lhs?.ItemId == rhs?.ItemId)
            {
                AddInternal(new Box
                {
                    RelativeSizeAxes = Axes.Both,
                    Alpha = 0,
                    AlwaysPresent = true
                });
                BorderColour = Color4.White;
                BorderThickness = 2;
                Masking = true;
            }

            protected override Drawable CreateDrawable(TestItem model) => new TestItemDrawable(model);
        }

        private class PlaceholderTestModelBackedDrawable : TestModelBackedDrawable
        {
            protected override Drawable CreateDrawable(TestItem model) => model == null ? null : new TestItemDrawable(model);

            protected override Drawable CreatePlaceholder() => new Box { Colour = Color4.Blue };
        }

        private class DelayedTestModelBackedDrawable : PlaceholderTestModelBackedDrawable
        {
            protected override double LoadDelay => 1000 / Clock.Rate;
        }
    }
}
