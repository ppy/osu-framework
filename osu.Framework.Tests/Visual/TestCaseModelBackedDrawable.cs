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
            DelayedTestModelBackedDrawable delayedModelBackedDrawable;
            FadeImmediateTestModelBackedDrawable fadeImmediateModelBackedDrawable;

            AddRange(new Drawable[]
            {
                modelBackedDrawable = new TestModelBackedDrawable
                {
                    Position = new Vector2(50, 50),
                    Size = new Vector2(100, 100)
                },
                delayedModelBackedDrawable = new DelayedTestModelBackedDrawable
                {
                    Position = new Vector2(50, 200),
                    Size = new Vector2(100, 100)
                },
                fadeImmediateModelBackedDrawable = new FadeImmediateTestModelBackedDrawable
                {
                    Position = new Vector2(50, 350),
                    Size = new Vector2(100, 100)
                }
            });

            // make sure the items are null before we begin the tests
            AddStep("Set all null", () =>
            {
                modelBackedDrawable.Item = null;
                delayedModelBackedDrawable.Item = null;
                fadeImmediateModelBackedDrawable.Item = null;
            });

            AddUntilStep(() => modelBackedDrawable.VisibleItemId == -1 &&
                               delayedModelBackedDrawable.VisibleItemId == -1 &&
                               fadeImmediateModelBackedDrawable.VisibleItemId == -1, "Wait until all null");

            // try setting items and null for a regular model backed drawable
            addItemTest("Simple", modelBackedDrawable, 0);
            addItemTest("Simple", modelBackedDrawable, 1);
            addItemTest("Simple", modelBackedDrawable, -1);

            // try setting items and null for a model backed drawable with a loading delay
            addItemTest("Delay", delayedModelBackedDrawable, 0);
            addItemTest("Delay", delayedModelBackedDrawable, 1);
            addItemTest("Delay", delayedModelBackedDrawable, -1);

            // try setting an item and checking that the placeholder is visible during the transition
            addItemTest("Fade Imm.", fadeImmediateModelBackedDrawable, 0, false);
            addItemTest("Fade Imm.", fadeImmediateModelBackedDrawable, 1, false);
            addItemTest("Fade Imm.", fadeImmediateModelBackedDrawable, -1, false);
        }

        private void addItemTest(string prefix, TestModelBackedDrawable drawable, int itemNumber, bool testNotChanged = true)
        {
            if (itemNumber < 0)
                AddStep($"{prefix}: Set null", () => drawable.Item = null);
            else
                AddStep($"{prefix}: Set item {itemNumber}", () => drawable.Item = new TestItem(itemNumber));

            if (testNotChanged)
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

        private class TestItemDrawable : CompositeDrawable
        {
            public readonly int ItemId;
            private readonly bool delay;

            public TestItemDrawable(TestItem item, bool delay = true)
            {
                this.delay = delay && item != null;
                ItemId = item?.ItemId ?? -1;

                RelativeSizeAxes = Axes.Both;

                InternalChildren = new Drawable[]
                {
                    new Box
                    {
                        Colour = Color4.DarkGoldenrod,
                        RelativeSizeAxes = Axes.Both
                    },
                    new SpriteText
                    {
                        Text = item == null ? "No Item" : $"Item {item.ItemId}",
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre
                    }
                };
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
            public TestItem Item
            {
                get => Model;
                set => Model = value;
            }

            public int VisibleItemId => (DisplayedDrawable as TestItemDrawable)?.ItemId ?? -1;

            public new Drawable DisplayedDrawable => base.DisplayedDrawable;

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

        private class FadeImmediateTestModelBackedDrawable : TestModelBackedDrawable
        {
            protected override bool FadeOutImmediately => true;

            protected override double FadeDuration => 0;
        }

        private class DelayedTestModelBackedDrawable : TestModelBackedDrawable
        {
            protected override double LoadDelay => 1000 / Clock.Rate;

            protected override Drawable CreateDrawable(TestItem model) => new TestItemDrawable(model, false);
        }
    }
}
