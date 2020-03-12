// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Video;
using osu.Framework.IO.Network;
using osu.Framework.Timing;

namespace osu.Framework.Tests.Visual.Sprites
{
    public class TestSceneVideoSprite : FrameworkTestScene
    {
        private ManualClock clock;
        private VideoSprite videoSprite;
        private readonly SpriteText timeText;
        private readonly IBindable<VideoDecoder.DecoderState> decoderState = new Bindable<VideoDecoder.DecoderState>();

        public override IReadOnlyList<Type> RequiredTypes => new[] { typeof(VideoSpriteDrawNode) };

        public TestSceneVideoSprite()
        {
            loadVideo();

            Add(timeText = new SpriteText
            {
                Text = "Video is loading...",
                Depth = -1
            });

            AddUntilStep("wait for video to load", () => videoSprite != null);

            AddStep("Jump ahead by 10 seconds", () => clock.CurrentTime += 10_000.0);
            AddStep("Jump back by 10 seconds", () => clock.CurrentTime = Math.Max(0, clock.CurrentTime - 10_000.0));
            AddToggleStep("Toggle looping", newState =>
            {
                videoSprite.Loop = newState;
                clock.CurrentTime = 0;
            });
        }

        private async void loadVideo()
        {
            var wr = new WebRequest("https://assets.ppy.sh/media/landing.mp4");
            await wr.PerformAsync();

            Schedule(() =>
            {
                Add(videoSprite = new VideoSprite(wr.ResponseStream, false)
                {
                    Loop = false,
                    Clock = new FramedClock(clock = new ManualClock()),
                });

                timeText.Font = FrameworkFont.Condensed.With(fixedWidth: true);
            });
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
