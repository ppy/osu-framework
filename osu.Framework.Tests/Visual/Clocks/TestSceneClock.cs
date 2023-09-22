// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Input.Events;
using osu.Framework.Testing;
using osu.Framework.Timing;
using osuTK;
using osuTK.Graphics;

namespace osu.Framework.Tests.Visual.Clocks
{
    public abstract partial class TestSceneClock : FrameworkTestScene
    {
        private readonly FillFlowContainer fill;

        protected TestSceneClock()
        {
            Child = fill = new FillFlowContainer
            {
                Spacing = new Vector2(5),
                Direction = FillDirection.Full,
                RelativeSizeAxes = Axes.Both,
            };
        }

        [SetUpSteps]
        public void SetUpSteps()
        {
            AddStep("clear all", () =>
            {
                fill.Clear();
                lastClock = null;
            });
        }

        private IClock? lastClock;

        protected IClock AddClock(IClock clock, string? name = null)
        {
            if (lastClock != null && clock is ISourceChangeableClock framed)
                framed.ChangeSource(lastClock);

            fill.Add(new VisualClock(lastClock = clock, name));

            return clock;
        }

        public partial class VisualClock : CompositeDrawable
        {
            public IClock TrackingClock { get; }

            private readonly SpriteText time;
            private readonly SpriteText elapsed;
            private readonly SpriteText rate;

            private bool zeroed = true;

            private const float width = 200;

            private readonly Box bg;
            private readonly Box hand;

            public VisualClock(IClock trackingClock, string? name = null)
            {
                TrackingClock = trackingClock;

                Size = new Vector2(width);
                CornerRadius = width / 2;
                Masking = true;

                BorderColour = Color4.White;
                BorderThickness = 5;

                InternalChildren = new Drawable[]
                {
                    bg = new Box
                    {
                        Colour = trackingClock is IAdjustableClock ? Color4.Tomato : Color4.Navy,
                        RelativeSizeAxes = Axes.Both,
                    },
                    new SpriteText
                    {
                        Text = trackingClock.GetType().Name + (!string.IsNullOrEmpty(name) ? $" ({name})" : string.Empty),
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                        Y = -25,
                    },
                    time = new SpriteText
                    {
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                        Y = 25,
                    },
                    rate = new SpriteText
                    {
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                        Font = new FontUsage(size: 14),
                        Y = 40,
                    },
                    elapsed = new SpriteText
                    {
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                        Font = new FontUsage(size: 14),
                        Y = 60,
                    },
                    hand = new Box
                    {
                        Colour = Color4.White,
                        Anchor = Anchor.Centre,
                        Origin = Anchor.BottomCentre,
                        Size = new Vector2(2, width / 2)
                    },
                };
            }

            protected override bool OnClick(ClickEvent e)
            {
                if (TrackingClock is IAdjustableClock adjustable)
                {
                    if (adjustable.IsRunning)
                        adjustable.Stop();
                    else
                        adjustable.Start();
                }

                return true;
            }

            protected override bool OnScroll(ScrollEvent e)
            {
                if (TrackingClock is IAdjustableClock adjustable)
                    adjustable.Rate += e.ScrollDelta.Y / 1000;

                return base.OnScroll(e);
            }

            protected override void Update()
            {
                base.Update();

                double lastTime = TrackingClock.CurrentTime;

                if (TrackingClock is IFrameBasedClock framedClock)
                {
                    framedClock.ProcessFrame();
                    elapsed.Text = $"{framedClock.ElapsedFrameTime:+0.00;-0.00} ms";
                }

                var timespan = TimeSpan.FromMilliseconds(TrackingClock.CurrentTime);
                time.Text = $"{timespan.Minutes:00}:{timespan.Seconds:00}:{timespan.Milliseconds:00}";
                rate.Text = $"{TrackingClock.Rate:N2}x";

                if (TrackingClock.CurrentTime != lastTime)
                    BorderColour = TrackingClock.CurrentTime >= lastTime ? Color4.White : Color4.Red;

                Colour = TrackingClock.IsRunning ? Color4.White : Color4.Gray;

                hand.Rotation = (float)(TrackingClock.CurrentTime / 1000) * 360 % 360;

                if (hand.Rotation < 180)
                {
                    if (!zeroed)
                    {
                        zeroed = true;
                        bg.FlashColour(Color4.White, 500, Easing.OutQuint);
                    }
                }
                else
                {
                    zeroed = false;
                }
            }
        }
    }
}
