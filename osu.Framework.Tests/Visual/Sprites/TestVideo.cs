// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System.IO;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Rendering;
using osu.Framework.Graphics.Video;

namespace osu.Framework.Tests.Visual.Sprites
{
    internal class TestVideo : Video
    {
        public TestVideo(Stream videoStream, bool startAtCurrentTime = true)
            : base(videoStream, startAtCurrentTime)
        {
        }

        private bool? useRoundedShader;

        public bool? UseRoundedShader
        {
            get => useRoundedShader;
            set
            {
                useRoundedShader = value;

                if (value == true)
                {
                    Masking = true;
                    CornerRadius = 10f;
                }
                else
                {
                    Masking = false;
                    CornerRadius = 0f;
                }

                Invalidate(Invalidation.DrawNode);
            }
        }

        public override Drawable CreateContent() => Sprite = new TestVideoSprite(this) { RelativeSizeAxes = Axes.Both };

        private class TestVideoSprite : VideoSprite
        {
            private readonly TestVideo video;

            public TestVideoSprite(TestVideo video)
                : base(video)
            {
                this.video = video;
            }

            protected override DrawNode CreateDrawNode() => new TestVideoSpriteDrawNode(video);
        }

        private class TestVideoSpriteDrawNode : VideoSpriteDrawNode
        {
            private readonly TestVideo source;

            protected override bool RequiresRoundedShader(IRenderer renderer) => useRoundedShader ?? base.RequiresRoundedShader(renderer);

            private bool? useRoundedShader;

            public TestVideoSpriteDrawNode(TestVideo source)
                : base(source)
            {
                this.source = source;
            }

            public override void ApplyState()
            {
                base.ApplyState();

                useRoundedShader = source.UseRoundedShader;
            }
        }
    }
}
