// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Configuration;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Video;
using osu.Framework.IO.Stores;
using osu.Framework.Timing;

namespace osu.Framework.Tests.Visual.Sprites
{
    public class TestSceneVideo : FrameworkTestScene
    {
        private ResourceStore<byte[]> videoStore;

        private Container videoContainer;
        private TextFlowContainer timeText;

        private ManualClock clock;

        private TestVideo video;

        private bool didDecode;

        [Resolved]
        private FrameworkConfigManager config { get; set; }

        [BackgroundDependencyLoader]
        private void load(Game game)
        {
            videoStore = new NamespacedResourceStore<byte[]>(game.Resources, @"Videos");

            Children = new Drawable[]
            {
                videoContainer = new Container
                {
                    RelativeSizeAxes = Axes.Both,
                    Clock = new FramedClock(clock = new ManualClock()),
                },
                timeText = new TextFlowContainer(f => f.Font = FrameworkFont.Condensed)
                {
                    RelativeSizeAxes = Axes.Both,
                    Text = "Video is loading...",
                }
            };
        }

        private void loadNewVideo(string format = "mp4")
        {
            AddStep("Reset clock", () =>
            {
                clock.CurrentTime = 0;
                didDecode = false;
            });
            AddStep($"load .{format} video", () =>
            {
                videoContainer.Child = video = new TestVideo(videoStore.GetStream($"sample-video.{format}"))
                {
                    Loop = false,
                    Origin = Anchor.Centre,
                    Anchor = Anchor.Centre,
                };
            });
            AddUntilStep("Wait for video to load", () => video.IsLoaded);
            AddStep("Reset clock", () => clock.CurrentTime = 0);

            AddUntilStep("Wait for decode start", () => video.CurrentFrameTime > 0);
        }

        [Test]
        public void TestHardwareDecode()
        {
            loadNewVideo();

            AddStep("disable hardware decoding", () => config.SetValue(FrameworkSetting.HardwareVideoDecoder, HardwareVideoDecoder.None));
            AddWaitStep("Wait some", 20);
            AddStep("enable hardware decoding", () => config.SetValue(FrameworkSetting.HardwareVideoDecoder, HardwareVideoDecoder.Any));
        }

        [Test]
        public void TestStartFromCurrentTime()
        {
            loadNewVideo();

            AddAssert("Video is near start", () => video.PlaybackPosition < 1000);

            AddWaitStep("Wait some", 20);

            loadNewVideo();

            AddAssert("Video is near start", () => video.PlaybackPosition < 1000);
        }

        [Test]
        public void TestDecodingStopsWhenNotPresent()
        {
            loadNewVideo();

            AddStep("make video hidden", () => video.Hide());

            AddWaitStep("wait a bit", 10);
            AddUntilStep("decoding stopped", () => video.State == VideoDecoder.DecoderState.Ready);

            AddStep("reset decode state", () => didDecode = false);

            AddWaitStep("wait a bit", 10);
            AddAssert("decoding didn't run", () => !didDecode);

            AddStep("make video visible", () => video.Show());
            AddUntilStep("decoding ran", () => didDecode);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void TestDecodingStopsBeforeStartTime(bool looping)
        {
            loadNewVideo();

            AddStep("Set looping", () => video.Loop = looping);

            AddStep("Jump back to before start time", () => clock.CurrentTime = -30000);

            AddWaitStep("wait a bit", 10);
            AddUntilStep("decoding stopped", () => video.State == VideoDecoder.DecoderState.Ready);

            AddStep("reset decode state", () => didDecode = false);

            AddWaitStep("wait a bit", 10);
            AddAssert("decoding didn't run", () => !didDecode);

            AddStep("seek close to start", () => clock.CurrentTime = -500);
            AddUntilStep("decoding ran", () => didDecode);
        }

        [TestCase("mp4")]
        [TestCase("avi")]
        [TestCase("webm")]
        public void TestJumpForward(string videoFormat)
        {
            loadNewVideo(videoFormat);

            AddStep("Jump ahead by 10 seconds", () => clock.CurrentTime += 10000);
            AddUntilStep("Video seeked", () => video.CurrentFrameTime >= 10000);
        }

        [TestCase("mp4")]
        [TestCase("avi")]
        [TestCase("webm")]
        public void TestJumpBack(string videoFormat)
        {
            loadNewVideo(videoFormat);

            AddStep("Jump ahead by 30 seconds", () => clock.CurrentTime += 30000);
            AddUntilStep("Video seeked", () => video.CurrentFrameTime >= 30000);
            AddStep("Jump back by 10 seconds", () => clock.CurrentTime -= 10000);
            AddUntilStep("Video seeked", () => video.CurrentFrameTime < 30000);
        }

        [TestCase("mp4")]
        [TestCase("avi")]
        [TestCase("webm")]
        public void TestJumpBackAfterEndOfPlayback(string videoFormat)
        {
            loadNewVideo(videoFormat);

            AddStep("Jump close to end", () => clock.CurrentTime = video.Duration - 1000);
            AddUntilStep("Video seeked", () => video.CurrentFrameTime >= video.Duration - 1500);

            AddUntilStep("Reached end", () => video.State == VideoDecoder.DecoderState.EndOfStream);
            AddStep("reset decode state", () => didDecode = false);

            AddStep("Jump back to valid time", () => clock.CurrentTime = 20000);
            AddUntilStep("decoding ran", () => didDecode);
        }

        [Test]
        public void TestVideoDoesNotLoopIfDisabled()
        {
            loadNewVideo();

            AddStep("Seek to end", () => clock.CurrentTime = video.Duration);
            AddUntilStep("Video seeked", () => video.PlaybackPosition >= video.Duration - 1000);
            AddWaitStep("Wait for playback", 10);
            AddAssert("Not looped", () => video.PlaybackPosition >= video.Duration - 1000);
        }

        [Test]
        public void TestVideoLoopsIfEnabled()
        {
            loadNewVideo();

            AddStep("Set looping", () => video.Loop = true);
            AddStep("Seek to end", () => clock.CurrentTime = video.Duration);
            AddWaitStep("Wait for playback", 10);
            AddUntilStep("Looped", () => video.PlaybackPosition < video.Duration - 1000);
        }

        [Test]
        public void TestShader()
        {
            loadNewVideo();

            AddStep("Set colour", () => video.Colour = Color4Extensions.FromHex("#ea7948").Opacity(0.75f));
            AddStep("Use normal shader", () => video.UseRoundedShader = false);
            AddStep("Use rounded shader", () => video.UseRoundedShader = true);
        }

        private int currentSecond;
        private int fps;
        private int lastFramesProcessed;

        private readonly FramedClock realtimeClock = new FramedClock();

        protected override void Update()
        {
            base.Update();

            realtimeClock.ProcessFrame();

            if (clock != null)
                clock.CurrentTime += realtimeClock.ElapsedFrameTime;

            if (video != null)
            {
                int newSecond = (int)(video.PlaybackPosition / 1000.0);

                if (newSecond != currentSecond)
                {
                    currentSecond = newSecond;
                    fps = video.FramesProcessed - lastFramesProcessed;
                    lastFramesProcessed = video.FramesProcessed;
                }

                if (timeText != null)
                {
                    timeText.Text = $"aim time: {video.PlaybackPosition:N2}\n"
                                    + $"video time: {video.CurrentFrameTime:N2}\n"
                                    + $"duration: {video.Duration:N2}\n"
                                    + $"buffered {video.AvailableFrames}\n"
                                    + $"FPS: {fps}\n"
                                    + $"State: {video.State}";
                }

                didDecode |= video.State == VideoDecoder.DecoderState.Running;
            }
        }
    }
}
