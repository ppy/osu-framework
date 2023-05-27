// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using NUnit.Framework;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osuTK;

namespace osu.Framework.Tests.Visual.Drawables
{
    public partial class TestSceneDrawableScale : FrameworkTestScene
    {
        [TestCase(1, true)]
        [TestCase(2, true)]
        [TestCase(-1, true)]
        [TestCase(-2, true)]
        [TestCase(0, false)]
        public void TestDrawablePresence(float scale, bool shouldBePresent)
        {
            Box box = null!;

            AddStep("set child", () => Child = box = new Box
            {
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                Size = new Vector2(100),
                Scale = new Vector2(scale)
            });

            AddAssert("box is present", () => box.IsPresent, () => Is.EqualTo(shouldBePresent));
        }

        [TestCase(1, 100)]
        [TestCase(2, 200)]
        [TestCase(-1, 100)]
        [TestCase(-2, 200)]
        [TestCase(0, 0)]
        public void TestAutoSize(float scale, float expectedSize)
        {
            Container container = null!;

            AddStep("set container", () => Child = container = new Container
            {
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                AutoSizeAxes = Axes.Both,
                Child = new Box
                {
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    Size = new Vector2(100),
                    Scale = new Vector2(scale)
                }
            });

            AddAssert("width is correct", () => container.DrawSize.X, () => Is.EqualTo(expectedSize).Within(1));
            AddAssert("height is correct", () => container.DrawSize.Y, () => Is.EqualTo(expectedSize).Within(1));
        }

        [TestCase(1)]
        [TestCase(2)]
        [TestCase(-1)]
        [TestCase(-2)]
        [TestCase(0)]
        public void TestInput(float scale)
        {
            Box box = null!;

            AddStep("set child", () => Child = box = new Box
            {
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                Size = new Vector2(100),
                Scale = new Vector2(scale)
            });

            AddAssert("passed through input X position is correct",
                () => box.ToParentSpace(box.ToLocalSpace(box.Parent.ToScreenSpace(Vector2.Zero))).X,
                () => Is.Zero.Within(1));

            AddAssert("passed through input Y position is correct",
                () => box.ToParentSpace(box.ToLocalSpace(box.Parent.ToScreenSpace(Vector2.Zero))).Y,
                () => Is.Zero.Within(1));
        }
    }
}
