// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Testing;
using osuTK;

namespace osu.Framework.Tests.Visual.Drawables
{
    [HeadlessTest]
    [System.ComponentModel.Description("ensure validity of drawables when receiving certain values")]
    public class TestScenePropertyBoundaries : FrameworkTestScene
    {
        [BackgroundDependencyLoader]
        private void load()
        {
            testPositiveScale();
            testZeroScale();
            testNegativeScale();
        }

        private void testPositiveScale()
        {
            var box = new Box
            {
                Size = new Vector2(100),
                Scale = new Vector2(2)
            };

            Add(box);

            AddAssert("Box is loaded", () => box.LoadState >= LoadState.Ready);
            AddAssert("Box is present", () => box.IsPresent);
            AddAssert("Box has valid draw matrix", () => checkDrawInfo(box.DrawInfo));
        }

        private void testZeroScale()
        {
            var box = new Box
            {
                Size = new Vector2(100),
                Scale = new Vector2(0)
            };

            Add(box);

            AddAssert("Box is loaded", () => box.LoadState >= LoadState.Ready);
            AddAssert("Box is present", () => !box.IsPresent);
            AddAssert("Box has valid draw matrix", () => checkDrawInfo(box.DrawInfo));
        }

        private void testNegativeScale()
        {
            var box = new Box
            {
                Size = new Vector2(100),
                Scale = new Vector2(-2)
            };

            Add(box);

            AddAssert("Box is loaded", () => box.LoadState >= LoadState.Ready);
            AddAssert("Box is present", () => box.IsPresent);
            AddAssert("Box has valid draw matrix", () => checkDrawInfo(box.DrawInfo));
        }

        private bool checkDrawInfo(DrawInfo drawInfo) =>
            checkFloat(drawInfo.Matrix.M11)
            && checkFloat(drawInfo.Matrix.M12)
            && checkFloat(drawInfo.Matrix.M13)
            && checkFloat(drawInfo.Matrix.M21)
            && checkFloat(drawInfo.Matrix.M22)
            && checkFloat(drawInfo.Matrix.M23)
            && checkFloat(drawInfo.Matrix.M31)
            && checkFloat(drawInfo.Matrix.M32)
            && checkFloat(drawInfo.Matrix.M33)
            && checkFloat(drawInfo.MatrixInverse.M11)
            && checkFloat(drawInfo.MatrixInverse.M12)
            && checkFloat(drawInfo.MatrixInverse.M13)
            && checkFloat(drawInfo.MatrixInverse.M21)
            && checkFloat(drawInfo.MatrixInverse.M22)
            && checkFloat(drawInfo.MatrixInverse.M23)
            && checkFloat(drawInfo.MatrixInverse.M31)
            && checkFloat(drawInfo.MatrixInverse.M32)
            && checkFloat(drawInfo.MatrixInverse.M33);

        private bool checkFloat(float value) => !float.IsNaN(value) && !float.IsInfinity(value) && !float.IsNegativeInfinity(value);
    }
}
