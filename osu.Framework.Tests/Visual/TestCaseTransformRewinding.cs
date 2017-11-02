// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
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

        public TestCaseTransformRewinding()
        {
            base.Content.Add(content = new FillFlowContainer
            {
                RelativeSizeAxes = Axes.Both,
                Spacing = new Vector2(10, 10)
            });

            AddStep("Basic scale", () => loadTest(0));
            AddStep("Scale sequence", () => loadTest(1));
            AddStep("Basic movement", () => loadTest(2));
            AddStep("Move sequence", () => loadTest(3));
            AddStep("Multiple sequence", () => loadTest(4));
        }

        private void loadTest(int testCase)
        {
            Clear();

            switch (testCase)
            {
                case 0:
                    {
                        Box box;
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

                        box.ScaleTo(0.75f, 300);
                        break;
                    }
                case 1:
                    {
                        Box box;
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

                        box.ScaleTo(0.75f, 100).Then().ScaleTo(0.5f, 100).Then().ScaleTo(0.25f, 100);
                        break;
                    }
                case 2:
                    {
                        Box box;
                        Add(new AnimationContainer
                        {
                            Size = new Vector2(200),
                            Child = box = new Box { Size = new Vector2(50) }
                        });

                        box.MoveTo(new Vector2(150, 150), 300);
                        break;
                    }
                case 3:
                    {
                        Box box;
                        Add(new AnimationContainer
                        {
                            Size = new Vector2(200),
                            Child = box = new Box { Size = new Vector2(50) }
                        });

                        box.MoveTo(new Vector2(150, 0), 100).Then().MoveTo(new Vector2(150, 150), 100).Then().MoveTo(new Vector2(0, 150), 100).Then().MoveTo(new Vector2(0), 100);
                        break;
                    }
                case 4:
                    {
                        Box box;
                        Add(new AnimationContainer
                        {
                            Size = new Vector2(200),
                            Child = box = new Box
                            {
                                RelativeSizeAxes = Axes.Both,
                                Scale = new Vector2(0.25f)
                            }
                        });

                        box.ScaleTo(0.5f, 300).MoveTo(new Vector2(100), 300)
                           .Then()
                           .ScaleTo(0.1f, 300).MoveTo(new Vector2(0, 180), 300)
                           .Then()
                           .ScaleTo(1f, 300).MoveTo(new Vector2(0, 0), 300)
                           .Then()
                           .FadeTo(0, 300);

                        break;
                    }
            }
        }

        private class AnimationContainer : Container
        {
            public override bool RemoveCompletedTransforms => false;

            protected override Container<Drawable> Content => content;
            private readonly WrappingTimeContainer content;

            private readonly SpriteText minTimeText;
            private readonly SpriteText currentTimeText;
            private readonly SpriteText maxTimeText;

            public AnimationContainer()
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
                            content = new WrappingTimeContainer
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
                                    minTimeText = new SpriteText { Colour = Color4.Blue},
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
            private const double time_padding = 500;

            public double MinTime => clock.MinTime;
            public double MaxTime => clock.MaxTime;

            private readonly ReversibleClock clock = new ReversibleClock();

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
                public double MinTime;
                public double MaxTime = 1000;

                private IFrameBasedClock trackingClock;

                public void SetSource(IFrameBasedClock trackingClock)
                {
                    this.trackingClock = trackingClock;
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
