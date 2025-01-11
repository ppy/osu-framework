// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Diagnostics;
using System.Linq;
using NUnit.Framework;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Testing;
using osu.Framework.Utils;
using osuTK;
using osuTK.Graphics;

namespace osu.Framework.Tests.Visual.Containers
{
    public partial class TestSceneScrollContainerDoublePrecision : ManualInputManagerTestScene
    {
        private const float item_height = 5000;
        private const int item_count = 8000;

        private ScrollContainer<Drawable> scrollContainer = null!;

        [SetUp]
        public void Setup() => Schedule(Clear);

        [Test]
        public void TestStandard()
        {
            AddStep("Create scroll container", () =>
            {
                Add(scrollContainer = new BasicScrollContainer
                {
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    ScrollbarVisible = true,
                    RelativeSizeAxes = Axes.Both,
                    Size = new Vector2(0.7f, 0.9f),
                });

                for (int i = 0; i < item_count; i++)
                {
                    scrollContainer.Add(new BoxWithDouble
                    {
                        Colour = new Color4(RNG.NextSingle(1), RNG.NextSingle(1), RNG.NextSingle(1), 1),
                        RelativeSizeAxes = Axes.X,
                        Height = item_height,
                        Y = i * item_height,
                    });
                }
            });

            scrollIntoView(item_count - 2);
            scrollIntoView(item_count - 1);
        }

        [Test]
        public void TestDoublePrecision()
        {
            AddStep("Create scroll container", () =>
            {
                Add(scrollContainer = new DoubleScrollContainer
                {
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    ScrollbarVisible = true,
                    RelativeSizeAxes = Axes.Both,
                    Size = new Vector2(0.7f, 0.9f),
                });

                for (int i = 0; i < item_count; i++)
                {
                    scrollContainer.Add(new BoxWithDouble
                    {
                        Colour = new Color4(RNG.NextSingle(1), RNG.NextSingle(1), RNG.NextSingle(1), 1),
                        RelativeSizeAxes = Axes.X,
                        Height = item_height,
                        DoubleLocation = i * item_height,
                    });
                }
            });

            scrollIntoView(item_count - 2);
            scrollIntoView(item_count - 1);
        }

        private void scrollIntoView(int index)
        {
            AddStep($"scroll {index} into view", () => scrollContainer.ScrollIntoView(scrollContainer.ChildrenOfType<BoxWithDouble>().Skip(index).First()));
            AddUntilStep($"{index} is visible", () => !scrollContainer.ChildrenOfType<BoxWithDouble>().Skip(index).First().IsMaskedAway);
        }

        public partial class DoubleScrollContainer : BasicScrollContainer
        {
            private readonly Container<BoxWithDouble> layoutContent;

            public override void Add(Drawable drawable)
            {
                if (drawable is not BoxWithDouble boxWithDouble)
                    throw new InvalidOperationException();

                Add(boxWithDouble);
            }

            public void Add(BoxWithDouble drawable)
            {
                if (drawable is not BoxWithDouble boxWithDouble)
                    throw new InvalidOperationException();

                layoutContent.Height = (float)Math.Max(layoutContent.Height, boxWithDouble.DoubleLocation + boxWithDouble.DrawHeight);
                layoutContent.Add(drawable);
            }

            public DoubleScrollContainer()
            {
                // Managing our own custom layout within ScrollContent causes feedback with internal ScrollContainer calculations,
                // so we must maintain one level of separation from ScrollContent.
                base.Add(layoutContent = new Container<BoxWithDouble>
                {
                    RelativeSizeAxes = Axes.X,
                });
            }

            public override double GetChildPosInContent(Drawable d, Vector2 offset)
            {
                if (d is not BoxWithDouble boxWithDouble)
                    return base.GetChildPosInContent(d, offset);

                return boxWithDouble.DoubleLocation + offset.X;
            }

            protected override void ApplyCurrentToContent()
            {
                Debug.Assert(ScrollDirection == Direction.Vertical);

                double scrollableExtent = -Current + ScrollableExtent * ScrollContent.RelativeAnchorPosition.Y;

                foreach (var d in layoutContent)
                    d.Y = (float)(d.DoubleLocation + scrollableExtent);
            }
        }

        public partial class BoxWithDouble : Box
        {
            public double DoubleLocation { get; set; }
        }
    }
}
