// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Video;
using osu.Framework.Testing;
using osu.Framework.Timing;

namespace osu.Framework.Tests.Visual.Sprites
{
    public class TestSceneVideo : FrameworkTestScene
    {
        private Container videoContainer;
        private SpriteText timeText;
        private readonly IBindable<VideoDecoder.DecoderState> decoderState = new Bindable<VideoDecoder.DecoderState>();

        public override IReadOnlyList<Type> RequiredTypes => new[] { typeof(VideoSpriteDrawNode) };

        private ManualClock clock;

        private TestVideo video;

        [BackgroundDependencyLoader]
        private void load()
        {
            Children = new Drawable[]
            {
                videoContainer = new Container
                {
                    RelativeSizeAxes = Axes.Both,
                    Clock = new FramedClock(clock = new ManualClock()),
                },
                timeText = new SpriteText
                {
                    Text = "Video is loading...",
                    Font = FrameworkFont.Condensed.With()
                }
            };
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
                videoContainer.Child = video = new TestVideo { Loop = false, };
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
    }
}
