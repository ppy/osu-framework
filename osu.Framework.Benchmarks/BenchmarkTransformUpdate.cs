// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using BenchmarkDotNet.Attributes;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Timing;
using osuTK;

namespace osu.Framework.Benchmarks
{
    public class BenchmarkTransformUpdate : BenchmarkTest
    {
        private TestBox target = null!;
        private TestBox targetNoTransforms = null!;

        public override void SetUp()
        {
            base.SetUp();

            const int transforms_count = 10000;

            ManualClock clock;

            targetNoTransforms = new TestBox { Clock = new FramedClock(clock = new ManualClock()) };
            target = new TestBox { Clock = new FramedClock(clock) };

            // transform one target member over a long period
            target.RotateTo(360, transforms_count * 2);

            // transform another over the same period many times
            for (int i = 0; i < transforms_count; i++)
                target.Delay(i).MoveTo(new Vector2(0.01f), 1f);

            clock.CurrentTime = target.LatestTransformEndTime;
            target.Clock.ProcessFrame();
        }

        [Benchmark]
        public void UpdateTransformsWithNonePresent()
        {
            for (int i = 0; i < 10000; i++)
                targetNoTransforms.UpdateTransforms();
        }

        [Benchmark]
        public void UpdateTransformsWithManyPresent()
        {
            for (int i = 0; i < 10000; i++)
                target.UpdateTransforms();
        }

        private class TestBox : Box
        {
            public override bool RemoveCompletedTransforms => false;

            public new void UpdateTransforms() => base.UpdateTransforms();
        }
    }
}
