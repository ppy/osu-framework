// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using NUnit.Framework;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osuTK;

namespace osu.Framework.Tests.Visual.Layout
{
    [TestFixture]
    public class TestSceneFitInsideFlow : FrameworkTestScene
    {
        private const float container_width = 60;

        private Box fitBox;

        [SetUp]
        public void SetUp() => Schedule(() =>
        {
            Child = new FillFlowContainer
            {
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                AutoSizeAxes = Axes.Y,
                Width = container_width,
                Direction = FillDirection.Vertical,
                Children = new Drawable[]
                {
                    fitBox = new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        FillMode = FillMode.Fit
                    },
                    // A box which forces the minimum dimension of the autosize flow container to be the horizontal dimension
                    new Box { Size = new Vector2(container_width, container_width * 2) }
                }
            };
        });

        /// <summary>
        /// Tests that using <see cref="FillMode.Fit"/> inside a <see cref="FlowContainer{T}"/> that is autosizing in one axis doesn't result in autosize feedback loops.
        /// Various sizes of the box are tested to ensure that non-one sizes also don't lead to erroneous sizes.
        /// </summary>
        /// <param name="value">The relative size of the box that is fitting.</param>
        [TestCase(0f)]
        [TestCase(0.5f)]
        [TestCase(1f)]
        public void TestFitInsideFlow(float value)
        {
            AddStep("Set size", () => fitBox.Size = new Vector2(value));

            var expectedSize = new Vector2(container_width * value);

            AddAssert("Check size before invalidate (1/2)", () => fitBox.DrawSize == expectedSize);
            AddAssert("Check size before invalidate (2/2)", () => fitBox.DrawSize == expectedSize);
            AddStep("Invalidate", () => fitBox.Invalidate());
            AddAssert("Check size after invalidate (1/2)", () => fitBox.DrawSize == expectedSize);
            AddAssert("Check size after invalidate (2/2)", () => fitBox.DrawSize == expectedSize);
        }
    }
}
