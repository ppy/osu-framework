// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using NUnit.Framework;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Colour;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osuTK;

namespace osu.Framework.Tests.Visual.Drawables
{
    public class TestSceneBorderColour : FrameworkTestScene
    {
        [Test]
        public void TestSolidBorder() => createBorderTest(Colour4.Blue, Colour4.Red);

        [Test]
        public void TestVerticalGradientBorder() => createBorderTest(Colour4.Green, ColourInfo.GradientVertical(Colour4.Black, Colour4.White));

        [Test]
        public void TestHorizontalGradientBorder() => createBorderTest(
            ColourInfo.GradientVertical(Colour4.White, Colour4.Black),
            ColourInfo.GradientHorizontal(Colour4.Red, Colour4.Blue));

        [Test]
        public void TestAllFourCorners() => createBorderTest(
            Colour4.Aquamarine,
            new ColourInfo
            {
                TopLeft = Colour4.Red,
                TopRight = Colour4.Yellow,
                BottomLeft = Colour4.Magenta,
                BottomRight = Colour4.Blue
            });

        private void createBorderTest(ColourInfo fillColour, ColourInfo borderColour)
        {
            Container container = null;
            Box box = null;

            AddStep("create box with solid border", () => Child = container = new Container
            {
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                Size = new Vector2(200),
                Masking = true,
                BorderThickness = 5,
                BorderColour = borderColour,
                Child = box = new Box
                {
                    RelativeSizeAxes = Axes.Both,
                    Colour = fillColour
                }
            });

            AddToggleStep("switch fill to border colour", useBorderColour => box.Colour = useBorderColour ? borderColour : fillColour);

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
