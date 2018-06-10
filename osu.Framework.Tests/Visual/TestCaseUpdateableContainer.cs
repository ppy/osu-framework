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
    public class TestCaseUpdateableContainer : TestCase
    {
        public TestCaseUpdateableContainer()
        {
            TestUpdateableContainer updateContainer;
            PlaceholderTestUpdateableContainer placeholderContainer;

            AddRange(new Drawable[]
            {
                updateContainer = new TestUpdateableContainer
                {
                    Position = new Vector2(50, 50),
                    Size = new Vector2(250, 250)
                },
                placeholderContainer = new PlaceholderTestUpdateableContainer
                {
                    Position = new Vector2(50, 350),
                    Size = new Vector2(250, 250)
                }
            });

            AddStep("Set item 0 no PH", () => updateContainer.Item = new TestItem(0));
            AddUntilStep(() => updateContainer.DisplayedDrawable?.IsLoaded ?? false, "wait until loaded");
            AddStep("Set item 1 no PH", () => updateContainer.Item = new TestItem(1));
            AddUntilStep(() => updateContainer.DisplayedDrawable?.IsLoaded ?? false, "wait until loaded");
            AddStep("Set null no PH", () => updateContainer.Item = null);
            AddAssert("Check no placeholder", () => updateContainer.PlaceholderDrawable == null);

            AddStep("Set item 0 with PH", () => placeholderContainer.Item = new TestItem(0));
            AddUntilStep(() => placeholderContainer.DisplayedDrawable?.IsLoaded ?? false, "wait until loaded");
            AddStep("Set item 1 with PH", () => placeholderContainer.Item = new TestItem(1));
            AddUntilStep(() => placeholderContainer.DisplayedDrawable?.IsLoaded ?? false, "wait until loaded");
            AddStep("Set null with PH", () => placeholderContainer.Item = null);
            AddAssert("Check placeholder is visible", () => (placeholderContainer.PlaceholderDrawable?.Alpha ?? 0) > 0);
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
            public TestItemDrawable(TestItem item)
            {
                Position = new Vector2(50, 50);
                Text = item == null ? "No Item" : $"Item {item.ItemId}";
            }

            [BackgroundDependencyLoader]
            private void load()
            {
                // delay
                Thread.Sleep(500);
            }
        }

        private class TestUpdateableContainer : UpdateableContainer<TestItem>
        {
            public TestItem Item { get => Source; set => Source = value; }

            protected override bool CompareItems(TestItem lhs, TestItem rhs) => lhs?.ItemId == rhs?.ItemId;

            protected override Drawable CreateDrawable(TestItem item) => new TestItemDrawable(item);
        }

        private class PlaceholderTestUpdateableContainer : UpdateableContainer<TestItem>
        {
            public TestItem Item { get => Source; set => Source = value; }

            protected override bool CompareItems(TestItem lhs, TestItem rhs) => lhs?.ItemId == rhs?.ItemId;

            protected override Drawable CreateDrawable(TestItem item) => item == null ? null : new TestItemDrawable(item);

            protected override Drawable CreatePlaceholder() => new Box { Colour = Color4.Blue };
        }
    }
}
