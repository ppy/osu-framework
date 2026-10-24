// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using NUnit.Framework;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Input.Events;
using osu.Framework.Utils;
using osu.Framework.Timing;
using osuTK;
using osuTK.Graphics;

namespace osu.Framework.Tests.Visual.Layout
{
    public partial class TestSceneLayoutDurations : FrameworkTestScene
    {
        private ManualClock manualClock;
        private FillFlowContainer fillFlowContainer;

        private Box box;

        private const float duration = 1000;

        private const float changed_value = 100;

        [SetUp]
        public void SetUp() => Schedule(() =>
        {
            manualClock = new ManualClock();

            Children = new Drawable[]
            {
                fillFlowContainer = new FillFlowContainer
                {
                    Clock = new FramedClock(manualClock),
                    Position = new Vector2(0, 200),
                    LayoutEasing = Easing.None,
                    Children = new Drawable[]
                    {
                        new Box { Colour = Color4.Red, Size = new Vector2(100) },
                        box = new Box { Colour = Color4.Blue, Size = new Vector2(100) },
                    }
                }
            };

            paused = false;
            fillFlowContainer.FinishTransforms();

            fillFlowContainer.LayoutDuration = 0;
            fillFlowContainer.Size = new Vector2(200, 200);
        });

        private void check(float ratio) =>
            AddAssert($"Check @{ratio}", () => Precision.AlmostEquals(box.Position, new Vector2(changed_value * (1 - ratio), changed_value * ratio)));

        private void skipTo(float ratio) => AddStep($"skip to {ratio}", () => { manualClock.CurrentTime = duration * ratio; });

        [Test]
        public void TestChangeAfterDuration()
        {
            AddStep("Start transformation", () =>
            {
                paused = true;
                manualClock.CurrentTime = 0;
                fillFlowContainer.FinishTransforms();

                fillFlowContainer.LayoutDuration = duration;
                fillFlowContainer.Width = 100;
            });

            foreach (float ratio in new[] { .25f, .5f, .75f, 1 })
            {
                skipTo(ratio);
                check(ratio);
            }
        }

        [Test]
        public void TestInterruptExistingDuration()
        {
            AddStep("Start transformation", () =>
            {
                paused = true;
                manualClock.CurrentTime = 0;
                fillFlowContainer.FinishTransforms();

                fillFlowContainer.LayoutDuration = duration;

                box.Size = new Vector2(changed_value);
                fillFlowContainer.Width = changed_value;
            });

            skipTo(0.5f);
            check(0.5f);

            AddStep("set duration 0", () =>
            {
                fillFlowContainer.LayoutDuration = 0;
            });

            // transform should still be playing
            skipTo(0.75f);
            check(0.75f);

            // check rewind works just for fun
            skipTo(0.5f);
            check(0.5f);

            AddStep("alter values", () =>
            {
                fillFlowContainer.Width = 200;
            });

            // fully complete
            check(0);

            // no remaining transform
            skipTo(1);
            check(0);
        }

        private bool paused;

        protected override void Update()
        {
            if (fillFlowContainer != null)
            {
                if (!paused) manualClock.CurrentTime = Clock.CurrentTime;

                fillFlowContainer.Invalidate();
            }

            base.Update();
        }

        protected override bool OnClick(ClickEvent e)
        {
            paused = !paused;
            return base.OnClick(e);
        }
    }
}
