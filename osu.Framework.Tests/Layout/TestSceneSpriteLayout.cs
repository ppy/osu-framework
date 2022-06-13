// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using NUnit.Framework;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Testing;
using osu.Framework.Tests.Visual;
using osuTK;

namespace osu.Framework.Tests.Layout
{
    [HeadlessTest]
    public class TestSceneSpriteLayout : FrameworkTestScene
    {
        [Test]
        public void TestChangeEdgeSmoothnessAfterDraw()
        {
            Box box = null;

            AddStep("create test", () =>
            {
                Child = box = new Box
                {
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    Size = new Vector2(100),
                    Rotation = 30
                };
            });

            AddAssert("zero inflation", () => box.InflationAmount == Vector2.Zero);
            AddStep("change edge smoothness", () => box.EdgeSmoothness = new Vector2(2));
            AddAssert("has inflation", () => box.InflationAmount.X > 0 && box.InflationAmount.Y > 0);
        }

        [Test]
        public void TestParentScaleChangesInflationAmount()
        {
            Drawable parent = null;
            Box box = null;

            AddStep("create test", () =>
            {
                Child = parent = new Container
                {
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    Size = new Vector2(100),
                    Rotation = 30,
                    Child = box = new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        EdgeSmoothness = new Vector2(2)
                    }
                };
            });

            Vector2 capturedInflation = Vector2.Zero;

            AddStep("capture inflation", () => capturedInflation = box.InflationAmount);
            AddStep("change parent scale", () => parent.Scale = new Vector2(2));
            AddAssert("inflation changed", () => box.InflationAmount != capturedInflation);
        }
    }
}
