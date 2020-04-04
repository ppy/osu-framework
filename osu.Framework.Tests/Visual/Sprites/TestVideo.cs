// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.IO;
using System.Threading;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Video;
using osu.Framework.IO.Network;

namespace osu.Framework.Tests.Visual.Sprites
{
    internal class TestVideo : Video
    {
        private static MemoryStream consumeVideoStream()
        {
            var wr = new WebRequest("https://assets.ppy.sh/media/landing.mp4");
            wr.PerformAsync();

            while (!wr.Completed)
                Thread.Sleep(100);

            var videoStream = new MemoryStream();
            wr.ResponseStream.CopyTo(videoStream);
            videoStream.Position = 0;
            return videoStream;
        }

        public TestVideo(bool startAtCurrentTime = true)
            : base(consumeVideoStream(), startAtCurrentTime)
        {
        }

        private bool? useRoundedShader;

        public bool? UseRoundedShader
        {
            get => useRoundedShader;
            set
            {
                useRoundedShader = value;
                Invalidate(Invalidation.DrawNode);
            }
        }

        protected override DrawNode CreateDrawNode() => new TestVideoSpriteDrawNode(this);

        private class TestVideoSpriteDrawNode : VideoSpriteDrawNode
        {
            private readonly TestVideo source;

            protected override bool RequiresRoundedShader => useRoundedShader ?? base.RequiresRoundedShader;

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
