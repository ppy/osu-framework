// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Input.States;
using osu.Framework.MathUtils;
using osu.Framework.Testing;
using osu.Framework.Timing;
using OpenTK;
using OpenTK.Graphics;

namespace osu.Framework.Tests.Visual
{
    public class TestCaseLayoutIdempotence : TestCase
    {
        private readonly ManualClock manualClock;
        private readonly Container autoSizeContainer;
        private readonly FillFlowContainer fillFlowContainer;
        public TestCaseLayoutIdempotence()
        {
            Box box1, box2;
            const float duration = 1000;

            manualClock = new ManualClock();
            Add(autoSizeContainer = new Container
            {
                Clock = new FramedClock(manualClock),
                AutoSizeEasing = Easing.None,
                Children = new [] {
                    new Box
                    {
                        Colour = Color4.Red,
                        RelativeSizeAxes = Axes.Both
                    },
                    box1 = new Box
                    {
                        Colour = Color4.Transparent,
                        Size = Vector2.Zero,
                    },
                }
            });

            Add(fillFlowContainer = new FillFlowContainer
            {
                Clock = new FramedClock(manualClock),
                Position = new Vector2(0, 200),
                LayoutEasing = Easing.None,
                Children = new Drawable[]
                {
                    new Box {Colour = Color4.Red, Size = new Vector2(100)},
                    box2 = new Box {Colour = Color4.Blue, Size = new Vector2(100)},
                }
            });

            AddStep("init", () =>
            {
                timeStopped = false;
                autoSizeContainer.FinishTransforms();
                fillFlowContainer.FinishTransforms();

                autoSizeContainer.AutoSizeAxes = Axes.None;
                autoSizeContainer.AutoSizeDuration = 0;
                autoSizeContainer.Size = Vector2.Zero;
                box1.Size = Vector2.Zero;

                fillFlowContainer.LayoutDuration = 0;
                fillFlowContainer.Size = new Vector2(200, 200);
            });
            AddStep("Start transformation", () =>
            {
                timeStopped = true;
                manualClock.CurrentTime = 0;
                autoSizeContainer.FinishTransforms();
                fillFlowContainer.FinishTransforms();

                autoSizeContainer.AutoSizeAxes = Axes.Both;
                autoSizeContainer.AutoSizeDuration = duration;
                box1.Size = new Vector2(100);

                fillFlowContainer.LayoutDuration = duration;
                fillFlowContainer.Width = 100;
            });
            foreach (var x in new[] { .25, .5, .75, 1 })
            {
                var ratio = (float)x;
                var time = ratio * duration;
                AddStep($"Time = {time}", () =>
                {
                    manualClock.CurrentTime = time;
                });
                AddAssert("Check", () => Precision.AlmostEquals(autoSizeContainer.Size, new Vector2(100 * ratio)) &&
                                         Precision.AlmostEquals(box2.Position, new Vector2(100 * (1 - ratio), 100 * ratio)));
            }
        }

        private bool timeStopped;
        protected override void Update()
        {
            if (!timeStopped) manualClock.CurrentTime = Clock.CurrentTime;
            autoSizeContainer.Children[0].Invalidate();
            fillFlowContainer.Invalidate();
            base.Update();
        }

        protected override bool OnClick(InputState state)
        {
            timeStopped = !timeStopped;
            return base.OnClick(state);
        }
    }
}
