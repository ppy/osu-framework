// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using BenchmarkDotNet.Attributes;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Transforms;
using osu.Framework.Timing;
using osuTK;

namespace osu.Framework.Benchmarks
{
    public partial class BenchmarkTransform : BenchmarkTest
    {
        private Container target = null!;

        public override void SetUp()
        {
            base.SetUp();

            target = new TestBox { Clock = new FramedClock() };
        }

        [Benchmark]
        public Transform CreateSingleBlank() => new TestTransform();

        [Benchmark]
        public void CreateSequenceThenRewind()
        {
            target.FadeIn(1000, Easing.OutQuint)
                  .Then().FadeOut(1000)
                  .Then().FadeOut(1000)
                  .Then().FadeOut(1000)
                  .Then().FadeOut(1000)
                  .Then().FadeOut(1000)
                  .Then().FadeOut(1000)
                  .Then().FadeOut(1000)
                  .Then().FadeOut(1000)
                  .Then().FadeOut(1000)
                  .Then().FadeOut(1000);

            for (int i = 0; i < 1000; i++)
            {
                target.ApplyTransformsAt(50000);
                target.ApplyTransformsAt(0);
            }

            target.ClearTransforms(true);
        }

        [Benchmark]
        public void CreateSequenceThenRewindManyChildren()
        {
            var nested = target;

            for (int i = 0; i < 5; i++)
            {
                nested.Add(new TestBox());
                nested.FadeOutFromOne(1000)
                      .Then().FadeOutFromOne(1000)
                      .Then().FadeOutFromOne(1000)
                      .Then().FadeOutFromOne(1000);

                nested.Add(nested = new TestBox());
                nested.FadeOutFromOne(1000)
                      .Then().FadeOutFromOne(1000)
                      .Then().FadeOutFromOne(1000)
                      .Then().FadeOutFromOne(1000);
            }

            for (int i = 0; i < 1000; i++)
            {
                target.ApplyTransformsAt(50000, true);
                target.ApplyTransformsAt(0, true);
            }

            target.ClearTransforms(true);
            target.Clear();
        }

        [Benchmark]
        public void CreateSequenceThenClearAfter()
        {
            target.FadeIn(1000, Easing.OutQuint)
                  .Then().FadeOut(1000)
                  .Then().FadeOut(1000)
                  .Then().FadeOut(1000)
                  .Then().FadeOut(1000)
                  .Then().FadeOut(1000)
                  .Then().FadeOut(1000)
                  .Then().FadeOut(1000)
                  .Then().FadeOut(1000)
                  .Then().FadeOut(1000)
                  .Then().FadeOut(1000);

            target.ClearTransformsAfter(5000);
        }

        [Benchmark]
        public void Expiry()
        {
            target.FadeIn(1000, Easing.OutQuint)
                  .ScaleTo(2, 1000, Easing.OutQuint)
                  .RotateTo(2, 1000, Easing.OutQuint);

            for (int i = 0; i < 1000; i++)
                target.Expire();

            target.ClearTransforms();
        }

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

        private partial class TestBox : Container
        {
            public override bool RemoveCompletedTransforms => false;
        }

        private class TestTransform : Transform<float, TestBox>
        {
            public override string TargetMember => throw new NotImplementedException();

            protected override void Apply(TestBox d, double time)
            {
                throw new NotImplementedException();
            }

            protected override void ReadIntoStartValue(TestBox d)
            {
                throw new NotImplementedException();
            }
        }
    }
}
