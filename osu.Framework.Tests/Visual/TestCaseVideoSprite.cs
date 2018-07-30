// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Video;
using osu.Framework.IO.Network;
using osu.Framework.Testing;
using osu.Framework.Timing;
using System;

namespace osu.Framework.Tests.Visual
{
    public class TestCaseVideoSprite : TestCase
    {
        private ManualClock clock;
        private VideoSprite videoSprite;
        private SpriteText timeText;

        public TestCaseVideoSprite()
        {
            loadVideo();
            Add(new SpriteText { Text = "Video is loading... " });
        }

        private async void loadVideo()
        {
            var wr = new WebRequest("https://assets.ppy.sh/media/landing.mp4");
            await wr.PerformAsync();

            Schedule(() =>
            {
                Clear();

                videoSprite = new VideoSprite(wr.ResponseStream);
                Add(videoSprite);
                videoSprite.Loop = true;
                videoSprite.ShowLastFrameDuringHideCutoff = true;
                videoSprite.NumberOfPreloadedFrames = 100;

                clock = new ManualClock();
                videoSprite.Clock = new FramedClock(clock);

                Add(timeText = new SpriteText { Text = "" });

                AddStep("Jump ahead by 10 seconds", () => clock.CurrentTime += 10_000.0);
                AddStep("Jump back by 10 seconds", () => clock.CurrentTime = Math.Max(0, clock.CurrentTime - 10_000.0));
                AddToggleStep("Toggle looping", (newState) =>
                {
                    videoSprite.Loop = newState;
                    clock.CurrentTime = 0;
                });
            });
        }

        protected override void Update()
        {
            base.Update();

            if (clock != null)
                clock.CurrentTime += Clock.ElapsedFrameTime;

            if (timeText != null)
                timeText.Text = $"{videoSprite.PlaybackPosition:N2} / {videoSprite.Duration}, Buffer-Frame {videoSprite.CurrentFrameIndex} / {videoSprite.AvailableFrames}, Current Frame Time: {videoSprite.CurrentFrameTime}";
        }
    }
}
