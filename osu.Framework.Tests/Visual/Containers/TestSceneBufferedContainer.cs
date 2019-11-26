// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osuTK;

namespace osu.Framework.Tests.Visual.Containers
{
    public class TestSceneBufferedContainer : TestSceneMasking
    {
        private readonly BufferedContainer buffer;

        public TestSceneBufferedContainer()
        {
            Remove(TestContainer);

            Add(buffer = new BufferedContainer
            {
                RelativeSizeAxes = Axes.Both,
                Children = new[] { TestContainer }
            });

            AddSliderStep("Blur quality", 0.01f, 1f, 1f, quality =>
            {
                buffer.BlurQuality = quality;
            });
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            buffer.BlurTo(new Vector2(0.5f));
        }
    }
}
