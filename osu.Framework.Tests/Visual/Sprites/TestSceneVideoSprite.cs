// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Video;
using osu.Framework.IO.Network;
using osu.Framework.Platform;
using osu.Framework.Testing;
using osu.Framework.Timing;
using osuTK;

namespace osu.Framework.Tests.Visual.Sprites
{
    public class TestSceneVideoSprite : FrameworkTestScene
    {
        private readonly Container videoContainer;
        private readonly SpriteText timeText;
        private readonly IBindable<VideoDecoder.DecoderState> decoderState = new Bindable<VideoDecoder.DecoderState>();

        public override IReadOnlyList<Type> RequiredTypes => new[] { typeof(VideoSpriteDrawNode) };

        [Resolved]
        private GameHost host { get; set; }

        private ManualClock clock;
        private VideoSprite videoSprite;
        private MemoryStream videoStream;

        public TestSceneVideoSprite()
        {
            Children = new Drawable[]
            {
                videoContainer = new Container { RelativeSizeAxes = Axes.Both },
                timeText = new SpriteText { Text = "Video is loading..." }
            };
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            host.Window.WindowState = WindowState.Minimized;

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
            AddStep("load video", () =>
            {
                videoStream.Seek(0, SeekOrigin.Begin);

                // Gets disposed when the video decoder/sprite is disposed
                var localStream = new MemoryStream();
                videoStream.CopyTo(localStream);

                localStream.Seek(0, SeekOrigin.Begin);

                videoContainer.Child = videoSprite = new VideoSprite(localStream, false)
                {
                    Loop = false,
                    Clock = new FramedClock(clock = new ManualClock()),
                };
            });

            AddUntilStep("wait for video to load", () => videoSprite.IsLoaded);
            AddStep("reset clock", () => clock.CurrentTime = 0);
        }

        [Test]
        public void TestJumpForward()
        {
            AddStep("Jump ahead by 10 seconds", () => clock.CurrentTime += 10000);
            AddUntilStep("Video seeked", () => videoSprite.PlaybackPosition >= 10000);
        }

        [Test]
        public void TestJumpBack()
        {
            AddStep("Jump ahead by 30 seconds", () => clock.CurrentTime += 30000);
            AddUntilStep("Video seeked", () => videoSprite.PlaybackPosition >= 30000);

            AddStep("Jump back by 10 seconds", () => clock.CurrentTime -= 10000);
            AddUntilStep("Video seeked", () => videoSprite.PlaybackPosition < 30000);
        }

        [Test]
        public void TestVideoDoesNotLoopIfDisabled()
        {
            AddStep("Seek to end", () => clock.CurrentTime = videoSprite.Duration);
            AddUntilStep("Video seeked", () => videoSprite.PlaybackPosition >= videoSprite.Duration - 1000);

            AddWaitStep("Wait for playback", 10);
            AddAssert("Not looped", () => videoSprite.PlaybackPosition >= videoSprite.Duration - 1000);
        }

        [Test]
        public void TestVideoLoopsIfEnabled()
        {
            AddStep("Set looping", () => videoSprite.Loop = true);
            AddStep("Seek to end", () => clock.CurrentTime = videoSprite.Duration);

            AddWaitStep("Wait for playback", 10);
            AddUntilStep("Looped", () => videoSprite.PlaybackPosition < videoSprite.Duration - 1000);
        }

        private int currentSecond;
        private int fps;
        private int lastFramesProcessed;

        protected override void Update()
        {
            base.Update();

            if (clock != null)
                clock.CurrentTime += Clock.ElapsedFrameTime;

            if (videoSprite != null)
            {
                var newSecond = (int)(videoSprite.PlaybackPosition / 1000.0);

                if (newSecond != currentSecond)
                {
                    currentSecond = newSecond;
                    fps = videoSprite.FramesProcessed - lastFramesProcessed;
                    lastFramesProcessed = videoSprite.FramesProcessed;
                }

                if (timeText != null)
                {
                    timeText.Text = $"aim time: {videoSprite.PlaybackPosition:N2} | "
                                    + $"video time: {videoSprite.CurrentFrameTime:N2} | "
                                    + $"duration: {videoSprite.Duration:N2} | "
                                    + $"buffered {videoSprite.AvailableFrames} | "
                                    + $"FPS: {fps} | "
                                    + $"State: {decoderState.Value}";
                }
            }
        }
    }
}
