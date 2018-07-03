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

            // make sure the items are null before we begin the tests
            AddStep("Set all null", () =>
            {
                modelBackedDrawable.Item = null;
                placeholderModelBackedDrawable.Item = null;
                delayedModelBackedDrawable.Item = null;
            });
            AddUntilStep(() => modelBackedDrawable.VisibleItemId == -1 &&
                               placeholderModelBackedDrawable.VisibleItemId == -1 &&
                               delayedModelBackedDrawable.VisibleItemId == -1, "Wait until all null");

            // try setting items and null for a regular model backed drawable
            addItemTest("No PH", modelBackedDrawable, 0);
            addItemTest("No PH", modelBackedDrawable, 1);
            addNullTest("No PH", modelBackedDrawable, false);

            // try setting items and null for a model backed drawable with a placeholder
            addItemTest("PH", placeholderModelBackedDrawable, 0);
            addItemTest("PH", placeholderModelBackedDrawable, 1);
            addNullTest("PH", placeholderModelBackedDrawable, true);

            // try setting items and null for a model backed drawable with a loading delay (test is the same as placeholder)
            addItemTest("D", delayedModelBackedDrawable, 0);
            addItemTest("D", delayedModelBackedDrawable, 1);
            addNullTest("D", delayedModelBackedDrawable, true);
        }

        private void addNullTest(string prefix, TestModelBackedDrawable drawable, bool expectPlaceholder)
        {
            AddStep($"{prefix}: Set null", () => drawable.Item = null);
            if (expectPlaceholder)
                AddAssert($"{prefix}: Check showing PH", () => drawable.VisibleItemId == -1 && drawable.IsShowingPlaceholder);
            else
            {
                AddAssert($"{prefix}: Test drawable not changed", () => drawable.VisibleItemId != -1);
                AddUntilStep(() => drawable.VisibleItemId == -1, $"{prefix}: Wait until changed");
                AddAssert($"{prefix}: Check not showing PH", () => !drawable.IsShowingPlaceholder);
            }
        }

        private void addItemTest(string prefix, TestModelBackedDrawable drawable, int itemNumber)
        {
            AddStep($"{prefix}: Set item {itemNumber}", () => drawable.Item = new TestItem(itemNumber));
            AddAssert($"{prefix}: Test drawable not changed", () => drawable.VisibleItemId != itemNumber);
            AddUntilStep(() => drawable.VisibleItemId == itemNumber, $"{prefix}: Wait until changed");
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
            private readonly bool delay;

            public TestItemDrawable(TestItem item, bool delay = true)
            {
                this.delay = delay;
                ItemId = item?.ItemId ?? -1;
                Position = new Vector2(10, 10);
                Text = item == null ? "No Item" : $"Item {item.ItemId}";
            }

            [BackgroundDependencyLoader]
            private void load()
            {
                if (delay)
                    Thread.Sleep((int)(500 / Clock.Rate));
            }
        }

        private class TestModelBackedDrawable : ModelBackedDrawable<TestItem>
        {
            public TestItem Item { get => Model; set => Model = value; }

            public int VisibleItemId => (DisplayedDrawable as TestItemDrawable)?.ItemId ?? -1;

            public new Drawable DisplayedDrawable => base.DisplayedDrawable;

            public new bool IsShowingPlaceholder => base.IsShowingPlaceholder;

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

            protected override Drawable CreateDrawable(TestItem model) => model == null ? null : new TestItemDrawable(model, false);

            protected override Drawable CreatePlaceholder() => new Box { Colour = Color4.DarkViolet };
        }
    }
}
