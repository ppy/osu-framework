// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using NUnit.Framework;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osuTK;

namespace osu.Framework.Tests.Visual.Drawables
{
    public class TestSceneBorderColour : FrameworkTestScene
    {
        [Test]
        public void TestSolidBorder()
        {
            Container container = null;

            AddStep("create box with solid border", () => Child = container = new Container
            {
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                Size = new Vector2(200),
                Masking = true,
                BorderThickness = 5,
                BorderColour = Colour4.Red,
                Child = new Box
                {
                    RelativeSizeAxes = Axes.Both,
                    Colour = Colour4.Blue
                }
            });

            AddSliderStep("change corner radius", 0, 100, 0, radius =>
            {
                if (container != null)
                    container.CornerRadius = radius;
            });

            AddSliderStep("change corner exponent", 0.1f, 10, 1, exponent =>
            {
                if (container != null)
                    container.CornerExponent = exponent;
            });
        }
    }
}
