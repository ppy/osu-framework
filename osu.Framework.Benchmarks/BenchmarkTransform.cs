// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using BenchmarkDotNet.Attributes;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Transforms;
using osu.Framework.Timing;
using osuTK;

namespace osu.Framework.Benchmarks
{
    public class BenchmarkTransform : BenchmarkTest
    {
        private Drawable target;

        public override void SetUp()
        {
            base.SetUp();

            target = new TestBox { Clock = new FramedClock() };
        }

        [Benchmark]
        public void CreateWithDefaultEasing()
        {
            target.FadeIn(1000, Easing.OutQuint)
                  .Then().FadeOut(500)
                  .Then().ScaleTo(new Vector2(2), 500, Easing.OutQuint);
            target.ClearTransforms();
        }

        [Benchmark]
        public void ApplyWithDefaultEasing()
        {
            target.FadeIn(1000, Easing.OutQuint);
            target.ApplyTransformsAt(double.MaxValue);
            target.ClearTransforms();
        }

        [Benchmark]
        public void CreateWithValueEasing()
        {
            target.FadeIn(1000, new ValueEasingFunction())
                  .Then().FadeOut(500, new ValueEasingFunction())
                  .Then().ScaleTo(new Vector2(2), 500, new ValueEasingFunction());
            target.ClearTransforms();
        }

        [Benchmark]
        public void ApplyWithValueEasing()
        {
            target.FadeIn(1000, new ValueEasingFunction());
            target.ApplyTransformsAt(double.MaxValue);
            target.ClearTransforms();
        }

        [Benchmark]
        public void CreateWithReferenceEasing()
        {
            target.FadeIn(1000, new ReferenceEasingFunction())
                  .Then().FadeOut(500, new ReferenceEasingFunction())
                  .Then().ScaleTo(new Vector2(2), 500, new ReferenceEasingFunction());
            target.ClearTransforms();
        }

        [Benchmark]
        public void ApplyWithReferenceEasing()
        {
            target.FadeIn(1000, new ReferenceEasingFunction());
            target.ApplyTransformsAt(double.MaxValue);
            target.ClearTransforms();
        }

        private readonly struct ValueEasingFunction : IEasingFunction
        {
            public double ApplyEasing(double time) => 0;
        }

        private class ReferenceEasingFunction : IEasingFunction
        {
            public double ApplyEasing(double time) => 0;
        }

        private class TestBox : Box
        {
            public override bool RemoveCompletedTransforms => false;
        }
    }
}
