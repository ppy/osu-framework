// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
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
        public Transform CreateSingleBlank() => new TestTransform();

        [Benchmark]
        public void CreateSequenceWithDefaultEasing()
        {
            target.FadeIn(1000, Easing.OutQuint)
                  .Then().FadeOut(500)
                  .Then().ScaleTo(new Vector2(2), 500, Easing.OutQuint);
            target.ClearTransforms();
        }

        [Benchmark]
        public void ApplySequenceWithDefaultEasing()
        {
            target.FadeIn(1000, Easing.OutQuint);
            target.ApplyTransformsAt(double.MaxValue);
            target.ClearTransforms();
        }

        [Benchmark]
        public void CreateSequenceWithValueEasing()
        {
            target.FadeIn(1000, new ValueEasingFunction())
                  .Then().FadeOut(500, new ValueEasingFunction())
                  .Then().ScaleTo(new Vector2(2), 500, new ValueEasingFunction());
            target.ClearTransforms();
        }

        [Benchmark]
        public void ApplySequenceWithValueEasing()
        {
            target.FadeIn(1000, new ValueEasingFunction());
            target.ApplyTransformsAt(double.MaxValue);
            target.ClearTransforms();
        }

        [Benchmark]
        public void CreateSequenceWithReferenceEasing()
        {
            target.FadeIn(1000, new ReferenceEasingFunction())
                  .Then().FadeOut(500, new ReferenceEasingFunction())
                  .Then().ScaleTo(new Vector2(2), 500, new ReferenceEasingFunction());
            target.ClearTransforms();
        }

        [Benchmark]
        public void ApplySequenceWithReferenceEasing()
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

        private class TestTransform : Transform<float, Box>
        {
            public override string TargetMember => throw new NotImplementedException();

            protected override void Apply(Box d, double time)
            {
                throw new NotImplementedException();
            }

            protected override void ReadValues(Box d)
            {
                throw new NotImplementedException();
            }
        }
    }
}
