// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osuTK;

namespace osu.Framework.Tests.Visual.Performance
{
    public partial class TestSceneBufferingPerformance : TestSceneTexturePerformance
    {
        private bool cachedFrameBuffer;
        private bool pixelSnapping;

        private readonly BindableFloat blurSigma = new BindableFloat();

        protected override void LoadComplete()
        {
            base.LoadComplete();

            AddLabel("Frame Buffers");
            AddToggleStep("cache frame buffer", v =>
            {
                cachedFrameBuffer = v;
                Recreate();
            });
            AddToggleStep("pixel snapping", v =>
            {
                pixelSnapping = v;
                Recreate();
            });
            AddSliderStep("blur sigma", 0f, 100f, 0f, v => blurSigma.Value = v);
        }

        protected override Drawable CreateDrawable() => new TestBufferedContainer(base.CreateDrawable(), cachedFrameBuffer, pixelSnapping)
        {
            BlurSigmaBindable = { BindTarget = blurSigma },
        };

        private partial class TestBufferedContainer : BufferedContainer
        {
            public readonly Bindable<float> BlurSigmaBindable = new BindableFloat();

            private readonly Drawable child;

            public TestBufferedContainer(Drawable child, bool cachedFramebuffer, bool pixelSnapping)
                : base(cachedFrameBuffer: cachedFramebuffer, pixelSnapping: pixelSnapping)
            {
                this.child = child;
            }

            [BackgroundDependencyLoader]
            private void load()
            {
                RelativeSizeAxes = Axes.Both;
                BlurSigmaBindable.BindValueChanged(v => BlurSigma = new Vector2(v.NewValue), true);

                Child = child;
            }

            protected override void Update()
            {
                base.Update();

                if (child.Width < 1f)
                {
                    Width = child.Width;
                    child.Width = 1f;
                }

                if (child.Height < 1f)
                {
                    Height = child.Height;
                    child.Height = 1f;
                }

                EdgeEffect = EdgeEffect with { Colour = child.Colour.AverageColour };
            }
        }
    }
}
