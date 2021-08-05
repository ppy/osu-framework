// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using NUnit.Framework;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Utils;
using osuTK.Graphics;

namespace osu.Framework.Tests.Visual.Drawables
{
    public class TestSceneColourInterpolation : FrameworkTestScene
    {
        [Test]
        public void TestColourInterpolatesInLinearSpace()
        {
            FillFlowContainer lines = null;

            AddStep("load interpolated colours", () => Child = lines = new FillFlowContainer
            {
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                AutoSizeAxes = Axes.X,
                Height = 50f,
                ChildrenEnumerable = Enumerable.Range(0, 750).Select(i => new Box
                {
                    Width = 1f,
                    RelativeSizeAxes = Axes.Y,
                    Colour = Interpolation.ValueAt(i, Color4.Blue, Color4.Red, 0, 750),
                }),
            });

            AddAssert("interpolation in linear space", () => lines.Children[lines.Children.Count / 2].Colour.AverageColour.Linear == new Color4(0.5f, 0f, 0.5f, 1f));
        }
    }
}
