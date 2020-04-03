// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using JetBrains.Annotations;
using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Video;
using osu.Framework.IO.Network;
using osu.Framework.Testing;
using osu.Framework.Timing;

namespace osu.Framework.Tests.Visual.Sprites
{
    public class TestSceneVideo : FrameworkTestScene
    {
        private readonly Container videoContainer;
        private readonly SpriteText timeText;
        private readonly IBindable<VideoDecoder.DecoderState> decoderState = new Bindable<VideoDecoder.DecoderState>();

        public override IReadOnlyList<Type> RequiredTypes => new[] { typeof(VideoSpriteDrawNode) };

        private readonly ManualClock clock;

        private TestVideo video;
        private MemoryStream videoStream;

        public TestSceneVideo()
        {
            Children = new Drawable[]
            {
                videoContainer = new Container
                {
                    RelativeSizeAxes = Axes.Both,
                    Clock = new FramedClock(clock = new ManualClock()),
                },
                timeText = new SpriteText { Text = "Video is loading..." }
            };
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            var wr = new WebRequest("https://assets.ppy.sh/media/landing.mp4");
            wr.PerformAsync();

            while (!wr.Completed)
                Thread.Sleep(100);

            videoStream = new MemoryStream();
            wr.ResponseStream.CopyTo(videoStream);

            timeText.Font = FrameworkFont.Condensed.With(fixedWidth: true);
        }

        [SetUpSteps]
        public void SetUpSteps()
        {
            AddStep("Reset clock", () => clock.CurrentTime = 0);
            loadNewVideo();
            AddUntilStep("Wait for video to load", () => video.IsLoaded);
            AddStep("Reset clock", () => clock.CurrentTime = 0);
        }

        private void loadNewVideo()
        {
            AddStep("load video", () =>
            {
                videoStream.Seek(0, SeekOrigin.Begin);

                // Gets disposed when the video decoder/sprite is disposed
                var localStream = new MemoryStream();
                videoStream.CopyTo(localStream);

                localStream.Seek(0, SeekOrigin.Begin);

                videoContainer.Child = video = new TestVideo(localStream)
                {
                    Loop = false,
                };
            });
        }

        [Test]
        public void TestStartFromCurrentTime()
        {
            AddAssert("Video is near start", () => video.PlaybackPosition < 1000);

            AddWaitStep("Wait some", 20);

            loadNewVideo();

            AddAssert("Video is near start", () => video.PlaybackPosition < 1000);
        }

        [Test]
        public void TestJumpForward()
        {
            AddStep("Jump ahead by 10 seconds", () => clock.CurrentTime += 10000);
            AddUntilStep("Video seeked", () => video.PlaybackPosition >= 10000);
        }

        [Test]
        public void TestJumpBack()
        {
            AddStep("Jump ahead by 30 seconds", () => clock.CurrentTime += 30000);
            AddUntilStep("Video seeked", () => video.PlaybackPosition >= 30000);

            AddStep("Jump back by 10 seconds", () => clock.CurrentTime -= 10000);
            AddUntilStep("Video seeked", () => video.PlaybackPosition < 30000);
        }

        [Test]
        public void TestVideoDoesNotLoopIfDisabled()
        {
            AddStep("Seek to end", () => clock.CurrentTime = video.Duration);
            AddUntilStep("Video seeked", () => video.PlaybackPosition >= video.Duration - 1000);

            AddWaitStep("Wait for playback", 10);
            AddAssert("Not looped", () => video.PlaybackPosition >= video.Duration - 1000);
        }

        [Test]
        public void TestVideoLoopsIfEnabled()
        {
            AddStep("Set looping", () => video.Loop = true);
            AddStep("Seek to end", () => clock.CurrentTime = video.Duration);

            AddWaitStep("Wait for playback", 10);
            AddUntilStep("Looped", () => video.PlaybackPosition < video.Duration - 1000);
        }

        [Test]
        public void TestShader()
        {
            AddStep("Set colour", () => video.Colour = Color4Extensions.FromHex("#ea7948").Opacity(0.75f));
            AddStep("Use normal shader", () => video.UseRoundedShader = false);
            AddStep("Use rounded shader", () => video.UseRoundedShader = true);
        }

        private int currentSecond;
        private int fps;
        private int lastFramesProcessed;

        protected override void Update()
        {
            base.Update();

            if (clock != null)
                clock.CurrentTime += Clock.ElapsedFrameTime;

            if (video != null)
            {
                var newSecond = (int)(video.PlaybackPosition / 1000.0);

                if (newSecond != currentSecond)
                {
                    currentSecond = newSecond;
                    fps = video.FramesProcessed - lastFramesProcessed;
                    lastFramesProcessed = video.FramesProcessed;
                }

                if (timeText != null)
                {
                    timeText.Text = $"aim time: {video.PlaybackPosition:N2} | "
                                    + $"video time: {video.CurrentFrameTime:N2} | "
                                    + $"duration: {video.Duration:N2} | "
                                    + $"buffered {video.AvailableFrames} | "
                                    + $"FPS: {fps} | "
                                    + $"State: {decoderState.Value}";
                }
            }
        }

        private class TestVideo : Video
        {
            public TestVideo([NotNull] Stream stream, bool startAtCurrentTime = true)
                : base(stream, startAtCurrentTime)
            {
            }

            public new VideoSprite Sprite => base.Sprite;

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
        }

        private class TestVideoSpriteDrawNode : VideoSpriteDrawNode
        {
            private readonly TestVideo source;

            protected override bool RequiresRoundedShader => useRoundedShader ?? base.RequiresRoundedShader;

            private bool? useRoundedShader;

            public TestVideoSpriteDrawNode(TestVideo source)
                : base(source.Sprite)
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
