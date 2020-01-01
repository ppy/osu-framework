// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osuTK.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.MathUtils;
using osu.Framework.Threading;
using osu.Framework.Timing;
using osuTK;

namespace osu.Framework.Graphics.Performance
{
    internal class FrameTimeDisplay : Container
    {
        private readonly SpriteText counter;

        private readonly ThrottledFrameClock clock;
        private readonly GameThread thread;

        public bool Counting = true;

        public FrameTimeDisplay(ThrottledFrameClock clock, GameThread thread)
        {
            this.clock = clock;
            this.thread = thread;

            Masking = true;
            CornerRadius = 5;

            AddRange(new Drawable[]
            {
                new Box
                {
                    RelativeSizeAxes = Axes.Both,
                    Colour = Color4.Black,
                    Alpha = 0.75f
                },
                counter = new CounterText
                {
                    Anchor = Anchor.TopRight,
                    Origin = Anchor.TopRight,
                    Spacing = new Vector2(-1, 0),
                    Text = @"...",
                }
            });
        }

        private float aimWidth;
        private double displayFps;
        private double displayFrameTime;

        private const int updates_per_second = 10;

        protected override void LoadComplete()
        {
            base.LoadComplete();

            double lastUpdate = 0;

            thread.Scheduler.AddDelayed(() =>
                {
                    if (!Counting) return;

                    double clockFps = clock.FramesPerSecond;
                    double actualElapsed = clock.ElapsedFrameTime - clock.TimeSlept;
                    double updateHz = clock.MaximumUpdateHz;

                    Schedule(() =>
                    {
                        if (!Precision.AlmostEquals(counter.DrawWidth, aimWidth))
                        {
                            ClearTransforms();

                            if (aimWidth == 0)
                                Size = counter.DrawSize;
                            else if (Precision.AlmostBigger(counter.DrawWidth, aimWidth))
                                this.ResizeTo(counter.DrawSize, 200, Easing.InOutSine);
                            else
                                this.Delay(1500).ResizeTo(counter.DrawSize, 500, Easing.InOutSine);

                            aimWidth = counter.DrawWidth;
                        }

                        double dampRate = Math.Max(Clock.CurrentTime - lastUpdate, 0) / 1000;
                        lastUpdate = Clock.CurrentTime;

                        displayFps = Interpolation.Damp(displayFps, clockFps, 0.01, dampRate);
                        displayFrameTime = Interpolation.Damp(displayFrameTime, actualElapsed, 0.01, dampRate);

                        counter.Text = $"{displayFps:0}fps({displayFrameTime:0.00}ms)"
                                       + $"{(updateHz < 10000 ? updateHz.ToString("0") : "∞").PadLeft(4)}hz";
                    });
                }, 1000.0 / updates_per_second, true);
        }

        private class CounterText : SpriteText
        {
            public CounterText()
            {
                Font = new FontUsage(fixedWidth: true);
            }

            protected override char[] FixedWidthExcludeCharacters { get; } = { ',', '.', ' ' };
        }
    }
}
