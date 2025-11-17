// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using NUnit.Framework;
using osu.Framework.Graphics;

namespace osu.Framework.Tests.Visual.Platform
{
    public partial class TestSceneDisplayBoundsWindowBorder : FrameworkTestScene
    {
        [Test]
        public void TestDisplayUsableBounds()
        {
            AddStep("Set up scene", () => Child = new WindowDisplaysPreview(true)
            {
                RelativeSizeAxes = Axes.Both,
            });
        }

        [Test]
        public void TestWindowBorder()
        {
            AddStep("Set up scene", () => Child = new WindowDisplaysPreview(false, true)
            {
                RelativeSizeAxes = Axes.Both,
            });
        }

        [Test]
        public void TestBoth()
        {
            AddStep("Set up scene", () => Child = new WindowDisplaysPreview(true, true)
            {
                RelativeSizeAxes = Axes.Both,
            });
        }
    }
}
