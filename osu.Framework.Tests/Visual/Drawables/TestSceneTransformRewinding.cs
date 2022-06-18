// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Transforms;
using osu.Framework.Graphics.Visualisation;
using osu.Framework.Utils;
using osu.Framework.Timing;
using osuTK;
using osuTK.Graphics;

namespace osu.Framework.Tests.Visual.Drawables
{
    public class TestSceneTransformRewinding : FrameworkTestScene
    {
        private const double interval = 250;
        private const int interval_count = 4;

        private static double intervalAt(int sequence) => interval * sequence;

        private ManualClock manualClock;
        private FramedClock manualFramedClock;

        [SetUp]
        public void SetUp() => Schedule(() =>
        {
            Clear();
            manualClock = new ManualClock();
            manualFramedClock = new FramedClock(manualClock);
        });

        [Test]
        public void BasicScale()
        {
            boxTest(box =>
            {
                box.Scale = Vector2.One;
                box.ScaleTo(0, interval * 4);
            });

            checkAtTime(250, box => Precision.AlmostEquals(box.Scale.X, 0.75f));
            checkAtTime(500, box => Precision.AlmostEquals(box.Scale.X, 0.5f));
            checkAtTime(750, box => Precision.AlmostEquals(box.Scale.X, 0.25f));
            checkAtTime(1000, box => Precision.AlmostEquals(box.Scale.X, 0f));

            checkAtTime(500, box => Precision.AlmostEquals(box.Scale.X, 0.5f));
            checkAtTime(250, box => Precision.AlmostEquals(box.Scale.X, 0.75f));

            AddAssert("check transform count", () => box.Transforms.Count() == 1);
        }

        [Test]
        public void ScaleSequence()
        {
            boxTest(box =>
            {
                box.Scale = Vector2.One;

                box.ScaleTo(0.75f, interval).Then()
                   .ScaleTo(0.5f, interval).Then()
                   .ScaleTo(0.25f, interval).Then()
                   .ScaleTo(0, interval);
            });

            int i = 0;
            checkAtTime(interval * ++i, box => Precision.AlmostEquals(box.Scale.X, 0.75f));
            checkAtTime(interval * ++i, box => Precision.AlmostEquals(box.Scale.X, 0.5f));
            checkAtTime(interval * ++i, box => Precision.AlmostEquals(box.Scale.X, 0.25f));
            checkAtTime(interval * ++i, box => Precision.AlmostEquals(box.Scale.X, 0f));

            checkAtTime(interval * (i -= 2), box => Precision.AlmostEquals(box.Scale.X, 0.5f));
            checkAtTime(interval * --i, box => Precision.AlmostEquals(box.Scale.X, 0.75f));

            AddAssert("check transform count", () => box.Transforms.Count() == 4);
        }

        [Test]
        public void BasicMovement()
        {
            boxTest(box =>
            {
                box.Scale = new Vector2(0.25f);
                box.Anchor = Anchor.TopLeft;
                box.Origin = Anchor.TopLeft;

                box.MoveTo(new Vector2(0.75f, 0), interval).Then()
                   .MoveTo(new Vector2(0.75f, 0.75f), interval).Then()
                   .MoveTo(new Vector2(0, 0.75f), interval).Then()
                   .MoveTo(new Vector2(0), interval);
            });

            int i = 0;
            checkAtTime(interval * ++i, box => Precision.AlmostEquals(box.X, 0.75f));
            checkAtTime(interval * ++i, box => Precision.AlmostEquals(box.Y, 0.75f));
            checkAtTime(interval * ++i, box => Precision.AlmostEquals(box.X, 0f));
            checkAtTime(interval * ++i, box => Precision.AlmostEquals(box.Y, 0f));

            checkAtTime(interval * (i -= 2), box => Precision.AlmostEquals(box.Y, 0.75f));
            checkAtTime(interval * --i, box => Precision.AlmostEquals(box.X, 0.75f));

            AddAssert("check transform count", () => box.Transforms.Count() == 4);
        }

        [Test]
        public void MoveSequence()
        {
            boxTest(box =>
            {
                box.Scale = new Vector2(0.25f);
                box.Anchor = Anchor.TopLeft;
                box.Origin = Anchor.TopLeft;

                box.ScaleTo(0.5f, interval).MoveTo(new Vector2(0.5f), interval).Then()
                   .ScaleTo(0.1f, interval).MoveTo(new Vector2(0, 0.75f), interval).Then()
                   .ScaleTo(1f, interval).MoveTo(new Vector2(0, 0), interval).Then()
                   .FadeTo(0, interval);
            });

            int i = 0;
            checkAtTime(interval * ++i, box => Precision.AlmostEquals(box.X, 0.5f) && Precision.AlmostEquals(box.Scale.X, 0.5f));
            checkAtTime(interval * ++i, box => Precision.AlmostEquals(box.Y, 0.75f) && Precision.AlmostEquals(box.Scale.X, 0.1f));
            checkAtTime(interval * ++i, box => Precision.AlmostEquals(box.X, 0f));
            checkAtTime(interval * (i += 2), box => Precision.AlmostEquals(box.Alpha, 0f));

            checkAtTime(interval * (i - 2), box => Precision.AlmostEquals(box.Alpha, 1f));

            AddAssert("check transform count", () => box.Transforms.Count() == 7);
        }

        [Test]
        public void MoveCancelSequence()
        {
            boxTest(box =>
            {
                box.Scale = new Vector2(0.25f);
                box.Anchor = Anchor.TopLeft;
                box.Origin = Anchor.TopLeft;

                box.ScaleTo(0.5f, interval).Then().ScaleTo(1, interval);

                Scheduler.AddDelayed(() => { box.ScaleTo(new Vector2(0.1f), interval); }, interval / 2);
            });

            int i = 0;
            checkAtTime(interval * i, box => Precision.AlmostEquals(box.Scale.X, 0.25f));
            checkAtTime(interval * ++i, box => !Precision.AlmostEquals(box.Scale.X, 0.5f));

            checkAtTime(interval * ++i, box => Precision.AlmostEquals(box.Scale.X, 0.1f));

            AddAssert("check transform count", () => box.Transforms.Count() == 2);
        }

        [Test]
        public void SameTypeInType()
        {
            boxTest(box =>
            {
                box.ScaleTo(0.5f, interval * 4);
                box.Delay(interval * 2).ScaleTo(1, interval);
            });

            int i = 0;
            checkAtTime(interval * i, box => Precision.AlmostEquals(box.Scale.X, 0.25f));
            checkAtTime(interval * ++i, box => Precision.AlmostEquals(box.Scale.X, 0.3125f));
            checkAtTime(interval * ++i, box => Precision.AlmostEquals(box.Scale.X, 0.375f));
            checkAtTime(interval * ++i, box => Precision.AlmostEquals(box.Scale.X, 1));

            checkAtTime(interval * --i, box => Precision.AlmostEquals(box.Scale.X, 0.375f));
            checkAtTime(interval * --i, box => Precision.AlmostEquals(box.Scale.X, 0.3125f));
            checkAtTime(interval * --i, box => Precision.AlmostEquals(box.Scale.X, 0.25f));

            AddAssert("check transform count", () => box.Transforms.Count() == 2);
        }

        [Test]
        public void SameTypeInPartialOverlap()
        {
            boxTest(box =>
            {
                box.ScaleTo(0.5f, interval * 2);
                box.Delay(interval).ScaleTo(1, interval * 2);
            });

            int i = 0;
            checkAtTime(interval * i, box => Precision.AlmostEquals(box.Scale.X, 0.25f));
            checkAtTime(interval * ++i, box => Precision.AlmostEquals(box.Scale.X, 0.375f));
            checkAtTime(interval * ++i, box => Precision.AlmostEquals(box.Scale.X, 0.6875f));
            checkAtTime(interval * ++i, box => Precision.AlmostEquals(box.Scale.X, 1));
            checkAtTime(interval * ++i, box => Precision.AlmostEquals(box.Scale.X, 1));

            checkAtTime(interval * --i, box => Precision.AlmostEquals(box.Scale.X, 1));
            checkAtTime(interval * --i, box => Precision.AlmostEquals(box.Scale.X, 0.6875f));
            checkAtTime(interval * --i, box => Precision.AlmostEquals(box.Scale.X, 0.375f));

            AddAssert("check transform count", () => box.Transforms.Count() == 2);
        }

        [Test]
        public void StartInMiddleOfSequence()
        {
            boxTest(box =>
            {
                box.Alpha = 0;
                box.Delay(interval * 2).FadeInFromZero(interval);
                box.ScaleTo(0.9f, interval * 4);
            }, 750);

            checkAtTime(interval * 3, box => Precision.AlmostEquals(box.Alpha, 1));
            checkAtTime(interval * 4, box => Precision.AlmostEquals(box.Alpha, 1) && Precision.AlmostEquals(box.Scale.X, 0.9f));
            checkAtTime(interval * 2, box => Precision.AlmostEquals(box.Alpha, 0) && Precision.AlmostEquals(box.Scale.X, 0.575f));

            AddAssert("check transform count", () => box.Transforms.Count() == 3);
        }

        [Test]
        public void RewindBetweenDisparateValues()
        {
            boxTest(box =>
            {
                box.Alpha = 0;
            });

            // move forward to future point in time before adding transforms.
            checkAtTime(interval * 4, _ => true);

            AddStep("add transforms", () =>
            {
                using (box.BeginAbsoluteSequence(0))
                {
                    box.FadeOutFromOne(interval);
                    box.Delay(interval * 3).FadeOutFromOne(interval);

                    // FadeOutFromOne adds extra transforms which disallow testing this scenario, so we remove them.
                    box.RemoveTransform(box.Transforms.ElementAt(2));
                    box.RemoveTransform(box.Transforms.ElementAt(0));
                }
            });

            checkAtTime(0, box => Precision.AlmostEquals(box.Alpha, 1));
            checkAtTime(interval * 1, box => Precision.AlmostEquals(box.Alpha, 0));
            checkAtTime(interval * 2, box => Precision.AlmostEquals(box.Alpha, 0));
            checkAtTime(interval * 3, box => Precision.AlmostEquals(box.Alpha, 1));
            checkAtTime(interval * 4, box => Precision.AlmostEquals(box.Alpha, 0));

            // importantly, this should be 0 not 1, reading from the EndValue of the first FadeOutFromOne transform.
            checkAtTime(interval * 2, box => Precision.AlmostEquals(box.Alpha, 0));
        }

        [Test]
        public void AddPastTransformFromFutureWhenNotInHierarchy()
        {
            AddStep("seek clock to 1000", () => manualClock.CurrentTime = interval * 4);

            AddStep("create box", () =>
            {
                box = createBox();
                box.Clock = manualFramedClock;
                box.RemoveCompletedTransforms = false;

                manualFramedClock.ProcessFrame();
                using (box.BeginAbsoluteSequence(0))
                    box.Delay(interval * 2).FadeOut(interval);
            });

            AddStep("seek clock to 0", () => manualClock.CurrentTime = 0);

            AddStep("add box", () =>
            {
                Add(new AnimationContainer
                {
                    Child = box,
                    ExaminableDrawable = box,
                });
            });

            checkAtTime(interval * 2, box => Precision.AlmostEquals(box.Alpha, 1));
            checkAtTime(interval * 3, box => Precision.AlmostEquals(box.Alpha, 0));
        }

        [Test]
        public void AddPastTransformFromFuture()
        {
            boxTest(box =>
            {
                box.Alpha = 0;
            });

            // move forward to future point in time before adding transforms.
            checkAtTime(interval * 4, _ => true);

            AddStep("add transforms", () =>
            {
                using (box.BeginAbsoluteSequence(0))
                {
                    box.FadeOutFromOne(interval);
                    box.Delay(interval * 3).FadeInFromZero(interval);

                    // FadeOutFromOne adds extra transforms which disallow testing this scenario, so we remove them.
                    box.RemoveTransform(box.Transforms.ElementAt(2));
                    box.RemoveTransform(box.Transforms.ElementAt(0));
                }
            });

            AddStep("add one more transform in the middle", () =>
            {
                using (box.BeginAbsoluteSequence(interval * 2))
                    box.FadeIn(interval * 0.5);
            });

            checkAtTime(interval * 2, box => Precision.AlmostEquals(box.Alpha, 0));
            checkAtTime(interval * 2.5, box => Precision.AlmostEquals(box.Alpha, 1));
        }

        [Test]
        public void LoopSequence()
        {
            boxTest(box => { box.RotateTo(0).RotateTo(90, interval).Loop(); });

            const int count = 4;

            for (int i = 0; i <= count; i++)
            {
                if (i > 0) checkAtTime(interval * i - 1, box => Precision.AlmostEquals(box.Rotation, 90f, 1));
                checkAtTime(interval * i, box => Precision.AlmostEquals(box.Rotation, 0));
            }

            AddAssert("check transform count", () => box.Transforms.Count() == 10);

            for (int i = count; i >= 0; i--)
            {
                if (i > 0) checkAtTime(interval * i - 1, box => Precision.AlmostEquals(box.Rotation, 90f, 1));
                checkAtTime(interval * i, box => Precision.AlmostEquals(box.Rotation, 0));
            }
        }

        [Test]
        public void StartInMiddleOfLoopSequence()
        {
            boxTest(box => { box.RotateTo(0).RotateTo(90, interval).Loop(); }, 750);

            checkAtTime(750, box => Precision.AlmostEquals(box.Rotation, 0f));

            AddAssert("check transform count", () => box.Transforms.Count() == 8);

            const int count = 4;

            for (int i = 0; i <= count; i++)
            {
                if (i > 0) checkAtTime(interval * i - 1, box => Precision.AlmostEquals(box.Rotation, 90f, 1));
                checkAtTime(interval * i, box => Precision.AlmostEquals(box.Rotation, 0));
            }

            AddAssert("check transform count", () => box.Transforms.Count() == 10);

            for (int i = count; i >= 0; i--)
            {
                if (i > 0) checkAtTime(interval * i - 1, box => Precision.AlmostEquals(box.Rotation, 90f, 1));
                checkAtTime(interval * i, box => Precision.AlmostEquals(box.Rotation, 0));
            }
        }

        [Test]
        public void TestSimultaneousTransformsOutOfOrder()
        {
            boxTest(box =>
            {
                using (box.BeginAbsoluteSequence(0))
                {
                    box.MoveToX(0.5f, 4 * interval);
                    box.Delay(interval).MoveToY(0.5f, 2 * interval);
                }
            });

            checkAtTime(0, box => Precision.AlmostEquals(box.Position, new Vector2(0)));
            checkAtTime(interval, box => Precision.AlmostEquals(box.Position, new Vector2(0.125f, 0)));
            checkAtTime(2 * interval, box => Precision.AlmostEquals(box.Position, new Vector2(0.25f, 0.25f)));
            checkAtTime(3 * interval, box => Precision.AlmostEquals(box.Position, new Vector2(0.375f, 0.5f)));
            checkAtTime(4 * interval, box => Precision.AlmostEquals(box.Position, new Vector2(0.5f)));
            checkAtTime(3 * interval, box => Precision.AlmostEquals(box.Position, new Vector2(0.375f, 0.5f)));
            checkAtTime(2 * interval, box => Precision.AlmostEquals(box.Position, new Vector2(0.25f, 0.25f)));
            checkAtTime(interval, box => Precision.AlmostEquals(box.Position, new Vector2(0.125f, 0)));
            checkAtTime(0, box => Precision.AlmostEquals(box.Position, new Vector2(0)));
        }

        [Test]
        public void TestMultipleTransformTargets()
        {
            boxTest(box =>
            {
                box.Delay(500).MoveTo(new Vector2(0, 0.25f), 500);
                box.MoveToY(0.5f, 250);
            });

            checkAtTime(double.MinValue, box => box.Y == 0);
            checkAtTime(0, box => box.Y == 0);
            checkAtTime(250, box => box.Y == 0.5f);
            checkAtTime(750, box => box.Y == 0.375f);
            checkAtTime(1000, box => box.Y == 0.25f);
            checkAtTime(1500, box => box.Y == 0.25f);
            checkAtTime(1250, box => box.Y == 0.25f);
            checkAtTime(750, box => box.Y == 0.375f);
        }

        [Test]
        public void TestMoveToOffsetRespectsRelevantTransforms()
        {
            boxTest(box =>
            {
                box.MoveToY(0.25f, 250);
                box.Delay(500).MoveToOffset(new Vector2(0, 0.25f), 250);
            });

            checkAtTime(0, box => box.Y == 0);
            checkAtTime(250, box => box.Y == 0.25f);
            checkAtTime(500, box => box.Y == 0.25f);
            checkAtTime(750, box => box.Y == 0.5f);
        }

        [Test]
        public void TestMoveToOffsetRespectsTransformsOrder()
        {
            boxTest(box =>
            {
                box.Delay(500).MoveToOffset(new Vector2(0, 0.25f), 250);
                box.MoveToY(0.25f, 250);
            });

            checkAtTime(0, box => box.Y == 0);
            checkAtTime(250, box => box.Y == 0.25f);
            checkAtTime(500, box => box.Y == 0.25f);
            checkAtTime(750, box => box.Y == 0.5f);
        }

        private Box box;

        private void checkAtTime(double time, Func<Box, bool> assert)
        {
            AddAssert($"check at time {time}", () =>
            {
                manualClock.CurrentTime = time;

                box.Clock = manualFramedClock;
                box.UpdateSubTree();

                return assert(box);
            });
        }

        private void boxTest(Action<Box> action, int startTime = 0)
        {
            AddStep("add box", () =>
            {
                Add(new AnimationContainer(startTime)
                {
                    Child = box = createBox(),
                    ExaminableDrawable = box,
                });

                action(box);
            });
        }

        private static Box createBox()
        {
            return new Box
            {
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                RelativeSizeAxes = Axes.Both,
                RelativePositionAxes = Axes.Both,
                Scale = new Vector2(0.25f),
            };
        }

        private class AnimationContainer : Container
        {
            public override bool RemoveCompletedTransforms => false;
            protected override Container<Drawable> Content => content;
            private readonly Container content;
            private readonly SpriteText minTimeText;
            private readonly SpriteText currentTimeText;
            private readonly SpriteText maxTimeText;
            private readonly Tick seekingTick;
            private readonly WrappingTimeContainer wrapping;
            public Box ExaminableDrawable;
            private readonly FlowContainer<DrawableTransform> transforms;

            public AnimationContainer(int startTime = 0)
            {
                Anchor = Anchor.Centre;
                Origin = Anchor.Centre;
                RelativeSizeAxes = Axes.Both;
                InternalChild = wrapping = new WrappingTimeContainer(startTime)
                {
                    RelativeSizeAxes = Axes.Both,
                    Children = new Drawable[]
                    {
                        new Container
                        {
                            FillMode = FillMode.Fit,
                            RelativeSizeAxes = Axes.Both,
                            Anchor = Anchor.Centre,
                            Origin = Anchor.Centre,
                            Size = new Vector2(0.6f),
                            Children = new Drawable[]
                            {
                                new Box
                                {
                                    RelativeSizeAxes = Axes.Both,
                                    Colour = Color4.DarkGray,
                                },
                                content = new Container
                                {
                                    RelativeSizeAxes = Axes.Both,
                                    Masking = true,
                                },
                            }
                        },
                        transforms = new FillFlowContainer<DrawableTransform>
                        {
                            Anchor = Anchor.CentreLeft,
                            Origin = Anchor.CentreLeft,
                            Spacing = Vector2.One,
                            RelativeSizeAxes = Axes.X,
                            AutoSizeAxes = Axes.Y,
                            Width = 0.2f,
                        },
                        new Container
                        {
                            Anchor = Anchor.TopCentre,
                            Origin = Anchor.TopCentre,
                            RelativeSizeAxes = Axes.Both,
                            Size = new Vector2(0.8f, 0.1f),
                            Children = new Drawable[]
                            {
                                minTimeText = new SpriteText
                                {
                                    Anchor = Anchor.BottomLeft,
                                    Origin = Anchor.TopLeft,
                                },
                                currentTimeText = new SpriteText
                                {
                                    RelativePositionAxes = Axes.X,
                                    Anchor = Anchor.BottomLeft,
                                    Origin = Anchor.BottomCentre,
                                    Y = -10,
                                },
                                maxTimeText = new SpriteText
                                {
                                    Anchor = Anchor.BottomRight,
                                    Origin = Anchor.TopRight,
                                },
                                seekingTick = new Tick(0, false),
                                new Tick(0),
                                new Tick(1),
                                new Tick(2),
                                new Tick(3),
                                new Tick(4),
                            }
                        }
                    }
                };
            }

            private List<Transform> displayedTransforms;

            protected override void Update()
            {
                base.Update();
                double time = wrapping.Time.Current;
                minTimeText.Text = wrapping.MinTime.ToString("n0");
                currentTimeText.Text = time.ToString("n0");
                seekingTick.X = currentTimeText.X = (float)(time / (wrapping.MaxTime - wrapping.MinTime));
                maxTimeText.Text = wrapping.MaxTime.ToString("n0");
                maxTimeText.Colour = time > wrapping.MaxTime ? Color4.Gray : wrapping.Time.Elapsed > 0 ? Color4.Blue : Color4.Red;
                minTimeText.Colour = time < wrapping.MinTime ? Color4.Gray : content.Time.Elapsed > 0 ? Color4.Blue : Color4.Red;

                if (displayedTransforms == null || !ExaminableDrawable.Transforms.SequenceEqual(displayedTransforms))
                {
                    transforms.Clear();
                    foreach (var t in ExaminableDrawable.Transforms)
                        transforms.Add(new DrawableTransform(t, 15));
                    displayedTransforms = new List<Transform>(ExaminableDrawable.Transforms);
                }
            }

            private class Tick : Box
            {
                private readonly int tick;
                private readonly bool colouring;

                public Tick(int tick, bool colouring = true)
                {
                    this.tick = tick;
                    this.colouring = colouring;
                    Anchor = Anchor.BottomLeft;
                    Origin = Anchor.BottomCentre;
                    Size = new Vector2(1, 10);
                    Colour = Color4.White;
                    RelativePositionAxes = Axes.X;
                    X = (float)tick / interval_count;
                }

                protected override void Update()
                {
                    base.Update();
                    if (colouring)
                        Colour = Time.Current > tick * interval ? Color4.Yellow : Color4.White;
                }
            }
        }

        private class WrappingTimeContainer : Container
        {
            // Padding, in milliseconds, at each end of maxima of the clock time
            private const double time_padding = 50;
            public double MinTime => clock.MinTime + time_padding;
            public double MaxTime => clock.MaxTime - time_padding;
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
                clock.MinTime = -time_padding;
                clock.MaxTime = intervalAt(interval_count) + time_padding;
            }

            private class ReversibleClock : IFrameBasedClock
            {
                private readonly double startTime;
                public double MinTime;
                public double MaxTime = 1000;
                private OffsetClock offsetClock;
                private IFrameBasedClock trackingClock;
                private bool reversed;

                public ReversibleClock(double startTime)
                {
                    this.startTime = startTime;
                }

                public void SetSource(IFrameBasedClock trackingClock)
                {
                    this.trackingClock = trackingClock;

                    offsetClock = new OffsetClock(trackingClock) { Offset = -trackingClock.CurrentTime + startTime };
                }

                public double CurrentTime { get; private set; }
                public double Rate => offsetClock.Rate;
                public bool IsRunning => offsetClock.IsRunning;
                public double ElapsedFrameTime => (reversed ? -1 : 1) * trackingClock.ElapsedFrameTime;
                public double FramesPerSecond => trackingClock.FramesPerSecond;
                public FrameTimeInfo TimeInfo => new FrameTimeInfo { Current = CurrentTime, Elapsed = ElapsedFrameTime };

                public void ProcessFrame()
                {
                    // There are two iterations, when iteration % 2 == 0 : not reversed
                    int iteration = (int)(offsetClock.CurrentTime / (MaxTime - MinTime));
                    reversed = iteration % 2 == 1;
                    double iterationTime = offsetClock.CurrentTime % (MaxTime - MinTime);
                    if (reversed)
                        CurrentTime = MaxTime - iterationTime;
                    else
                        CurrentTime = MinTime + iterationTime;
                }
            }
        }
    }
}
