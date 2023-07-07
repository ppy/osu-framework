// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osuTK;

namespace osu.Framework.Tests.Visual.Performance
{
    public partial class TestSceneFrameBuffers : TestSceneFillRate
    {
        private bool cachedFrameBuffers;
        private bool pixelSnapping;
        private float blurSigma;

        protected override void LoadComplete()
        {
            base.LoadComplete();

            AddLabel("Frame Buffers");
            AddToggleStep("cache frame buffer", v => cachedFrameBuffers = v);
            AddToggleStep("pixel snapping", v => pixelSnapping = v);
            AddSliderStep("blur sigma", 0f, 100f, 0f, v => blurSigma = v);
        }

        protected override Drawable CreateDrawable()
        {
            var sprite = base.CreateDrawable();

            var size = sprite.Size;
            sprite.Size = new Vector2(1f);

            return new BufferedContainer(cachedFrameBuffer: cachedFrameBuffers, pixelSnapping: pixelSnapping)
            {
                BlurSigma = new Vector2(blurSigma),
                RelativeSizeAxes = Axes.Both,
                Size = size,
                Child = sprite,
            };
        }
    }
}
