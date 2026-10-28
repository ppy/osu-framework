// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;
using NUnit.Framework;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Testing;
using osu.Framework.Utils;
using osuTK;

namespace osu.Framework.Tests.Visual.Drawables
{
    [HeadlessTest]
    public partial class TestSceneAutoSize : TestScene
    {
        private static readonly object[][] scales = Enumerable.Range(0, 10).Select(i => new object[] { MathF.Pow(10, -i) }).ToArray();

        [TestCaseSource(nameof(scales))]
        public void TestAlmostZeroXScale(float scale)
        {
            Container autoSizeContainer = null!;
            Container outerContainer;

            AddStep("add box", () =>
            {
                Add(outerContainer = new Container
                {
                    Name = "outer container",
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    RelativeSizeAxes = Axes.Both,
                    RelativePositionAxes = Axes.Both,
                    Child = autoSizeContainer = new Container
                    {
                        Name = "autosize container",
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                        AutoSizeAxes = Axes.Both,
                        Child = new Box
                        {
                            Anchor = Anchor.Centre,
                            Origin = Anchor.Centre,
                            Size = new Vector2(128f),
                        }
                    }
                });

                outerContainer.Scale = new Vector2(scale, 2);
            });

            AddAssert("autosize container preserves width", () => autoSizeContainer.Width, () => Is.EqualTo(128).Within(Precision.FLOAT_EPSILON));
        }

        [TestCaseSource(nameof(scales))]
        public void TestAlmostZeroYScale(float scale)
        {
            Container autoSizeContainer = null!;
            Container outerContainer;

            AddStep("add box", () =>
            {
                Add(outerContainer = new Container
                {
                    Name = "outer container",
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    RelativeSizeAxes = Axes.Both,
                    RelativePositionAxes = Axes.Both,
                    Child = autoSizeContainer = new Container
                    {
                        Name = "autosize container",
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                        AutoSizeAxes = Axes.Both,
                        Child = new Box
                        {
                            Anchor = Anchor.Centre,
                            Origin = Anchor.Centre,
                            Size = new Vector2(128f),
                        }
                    }
                });

                outerContainer.Scale = new Vector2(2, scale);
            });

            AddAssert("autosize container preserves height", () => autoSizeContainer.Height, () => Is.EqualTo(128).Within(Precision.FLOAT_EPSILON));
        }
    }
}
