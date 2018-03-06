// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Linq;
using OpenTK;
using OpenTK.Graphics;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Testing;
using osu.Framework.Timing;

namespace osu.Framework.Tests.Visual
{
    public class TestCaseTransformRewinding : TestCase
    {
        protected override Container<Drawable> Content => content;
        private readonly FillFlowContainer content;

        private const double interval = 250;

        private double intervalAt(int sequence) => interval * sequence;

        public TestCaseTransformRewinding()
        {
            base.Content.Add(content = new FillFlowContainer
            {
                RelativeSizeAxes = Axes.Both,
                Spacing = new Vector2(10, 10)
            });

            AddStep("Basic scale", () => boxTest(box =>
            {
                box.Scale = new Vector2(0.25f);
                box.ScaleTo(0.75f, interval);
            }));

            AddStep("Scale sequence", () => boxTest(box =>
            {
                box.Scale = new Vector2(0.25f);
                box.ScaleTo(0.75f, interval).Then()
                   .ScaleTo(0.5f, interval).Then()
                   .ScaleTo(0.25f, interval);
            }));

            AddStep("Basic movement", () => boxTest(box =>
            {
                box.Scale = new Vector2(0.25f);
                box.Anchor = Anchor.TopLeft;
                box.Origin = Anchor.TopLeft;

                box.MoveTo(new Vector2(150, 0), interval).Then()
                   .MoveTo(new Vector2(150, 150), interval).Then()
                   .MoveTo(new Vector2(0, 150), interval).Then()
                   .MoveTo(new Vector2(0), interval);
            }));

            AddStep("Move sequence", () => boxTest(box =>
            {
                box.Scale = new Vector2(0.25f);
                box.Anchor = Anchor.TopLeft;
                box.Origin = Anchor.TopLeft;

                box.ScaleTo(0.5f, interval).MoveTo(new Vector2(100), interval)
                   .Then()
                   .ScaleTo(0.1f, interval).MoveTo(new Vector2(0, 180), interval)
                   .Then()
                   .ScaleTo(1f, interval).MoveTo(new Vector2(0, 0), interval)
                   .Then()
                   .FadeTo(0, interval);
            }));

            AddStep("Same type in type", () => boxTest(box =>
            {
                box.ScaleTo(0.5f, interval * 4);
                box.Delay(interval * 2).ScaleTo(1, interval * 2);
            }));

            AddStep("Same type partial overlap", () => boxTest(box =>
            {
                box.ScaleTo(0.5f, interval * 4);
                box.Delay(interval * 2).ScaleTo(1, interval);
            }));

            AddStep("Start in middle of sequence", () => boxTest(box =>
            {
                box.Alpha = 0;
                box.Delay(interval * 2).FadeInFromZero(interval);
                box.ScaleTo(0.9f, interval * 4);
            }));
        }

        private Box box;

        private void boxTest(Action<Box> action)
        {
            Clear();

            Add(new AnimationContainer
            {
                Size = new Vector2(200),
                Child = box = new Box
                {
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    RelativeSizeAxes = Axes.Both,
                    Scale = new Vector2(0.25f),
                }
            });

            action(box);
        }

        private class AnimationContainer : Container
        {
            public override bool RemoveCompletedTransforms => false;

            protected override Container<Drawable> Content => content;
            private readonly WrappingTimeContainer content;

            private readonly SpriteText minTimeText;
            private readonly SpriteText currentTimeText;
            private readonly SpriteText maxTimeText;

            public AnimationContainer(int startTime = 0)
            {
                InternalChildren = new Drawable[]
                {
                    new FillFlowContainer
                    {
                        RelativeSizeAxes = Axes.Both,
                        Direction = FillDirection.Vertical,
                        Spacing = new Vector2(0, 5),
                        Children = new Drawable[]
                        {
                            content = new WrappingTimeContainer(startTime)
                            {
                                Anchor = Anchor.TopCentre,
                                Origin = Anchor.TopCentre,
                                RelativeSizeAxes = Axes.Both,
                                FillMode = FillMode.Fit,
                                Masking = true
                            },
                            new FillFlowContainer
                            {
                                Anchor = Anchor.TopCentre,
                                Origin = Anchor.TopCentre,
                                AutoSizeAxes = Axes.Both,
                                Direction = FillDirection.Horizontal,
                                Spacing = new Vector2(5, 0),
                                Children = new[]
                                {
                                    minTimeText = new SpriteText { Colour = Color4.Blue },
                                    currentTimeText = new SpriteText(),
                                    maxTimeText = new SpriteText { Colour = Color4.Blue },
                                }
                            }
                        }
                    }
                };
            }

            protected override void Update()
            {
                base.Update();

                minTimeText.Text = content.MinTime.ToString("n0");
                currentTimeText.Text = content.Time.Current.ToString("n0");
                maxTimeText.Text = content.MaxTime.ToString("n0");
            }
        }

        private class WrappingTimeContainer : Container
        {
            // Padding, in milliseconds, at each end of maxima of the clock time
            private const double time_padding = 50;

            public double MinTime => clock.MinTime;
            public double MaxTime => clock.MaxTime;

            private readonly ReversibleClock clock;

            public WrappingTimeContainer(double startTime)
            {
                clock = new ReversibleClock(startTime);
            }

            [BackgroundDependencyLoader]
            private void load()
            {
                // Replace the game clock, but keep it as a reference
                clock.SetSource(Clock);
                Clock = clock;
            }

            protected override void LoadComplete()
            {
                base.LoadComplete();

                double minTime = double.MaxValue;
                double maxTime = double.MinValue;

                foreach (var child in Children)
                {
                    if (child.Transforms.Count == 0)
                    {
                        minTime = Math.Min(minTime, 0);
                        maxTime = Math.Max(maxTime, 0);
                        continue;
                    }

                    minTime = Math.Min(minTime, child.Transforms.Min(t => t.StartTime) - time_padding);
                    maxTime = Math.Max(maxTime, child.Transforms.Max(t => t.EndTime) + time_padding);
                }

                clock.MinTime = minTime;
                clock.MaxTime = maxTime;
            }

            private class ReversibleClock : IFrameBasedClock
            {
                private readonly double startTime;
                public double MinTime;
                public double MaxTime = 1000;

                private IFrameBasedClock trackingClock;

                public ReversibleClock(double startTime)
                {
                    this.startTime = startTime;
                }

                public void SetSource(IFrameBasedClock trackingClock)
                {
                    this.trackingClock = new FramedOffsetClock(trackingClock) { Offset = -trackingClock.CurrentTime + startTime };
                }

                public double CurrentTime { get; private set; }

                public double Rate => trackingClock.Rate;

                public bool IsRunning => trackingClock.IsRunning;

                public double ElapsedFrameTime => trackingClock.ElapsedFrameTime;

                public double AverageFrameTime => trackingClock.AverageFrameTime;

                public double FramesPerSecond => trackingClock.FramesPerSecond;

                public FrameTimeInfo TimeInfo => new FrameTimeInfo { Current = CurrentTime, Elapsed = ElapsedFrameTime };

                public void ProcessFrame()
                {
                    trackingClock.ProcessFrame();

                    // There are two iterations, when iteration % 2 == 0 : not reversed
                    int iteration = (int)(trackingClock.CurrentTime / (MaxTime - MinTime));
                    bool reversed = iteration % 2 == 1;

                    double iterationTime = trackingClock.CurrentTime % (MaxTime - MinTime);

                    if (reversed)
                        CurrentTime = MaxTime - iterationTime;
                    else
                        CurrentTime = MinTime + iterationTime;
                }
            }
        }
    }
}
