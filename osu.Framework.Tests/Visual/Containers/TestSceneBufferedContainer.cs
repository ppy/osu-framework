// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osuTK;

namespace osu.Framework.Tests.Visual.Containers
{
    public class TestSceneBufferedContainer : TestSceneMasking
    {
        public TestSceneBufferedContainer()
        {
            Remove(TestContainer);

            BufferedContainer buffer;
            Add(buffer = new BufferedContainer
            {
                RelativeSizeAxes = Axes.Both,
                Children = new[] { TestContainer }
            });

            AddSliderStep("blur", 0f, 20f, 0f, blur =>
            {
                buffer.BlurTo(new Vector2(blur));
            });

            AddSliderStep("fbo scale (x)", 0.01f, 4f, 1f, scale =>
            {
                buffer.FrameBufferScale = new Vector2(scale, buffer.FrameBufferScale.Y);
            });

            AddSliderStep("fbo scale (y)", 0.01f, 4f, 1f, scale =>
            {
                buffer.FrameBufferScale = new Vector2(buffer.FrameBufferScale.X, scale);
            });
        }
    }
}
