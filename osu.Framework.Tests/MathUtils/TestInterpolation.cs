// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Transforms;
using osu.Framework.Utils;
using osuTK;

namespace osu.Framework.Tests.MathUtils
{
    [TestFixture]
    public class TestInterpolation
    {
        [TestCaseSource(nameof(getEasings))]
        public void TestEasingStartsAtZero(Easing easing) => Assert.That(Interpolation.ApplyEasing(easing, 0), Is.EqualTo(0).Within(Precision.DOUBLE_EPSILON));

        [TestCaseSource(nameof(getEasings))]
        public void TestEasingEndsAtOne(Easing easing) => Assert.That(Interpolation.ApplyEasing(easing, 1), Is.EqualTo(1).Within(Precision.DOUBLE_EPSILON));

        private static IEnumerable<Easing> getEasings() => Enum.GetValues(typeof(Easing)).OfType<Easing>();

        [Test]
        public void TestLerp()
        {
            Assert.AreEqual(5, Interpolation.Lerp(0, 10, 0.5f));
            Assert.AreEqual(0, Interpolation.Lerp(0, 10, 0));
            Assert.AreEqual(10, Interpolation.Lerp(0, 10, 1));
        }

        [Test]
        public void TestContinuousDamp()
        {
            const double start = 1;
            const double target = 2;
            const double half_time = 1000;

            Assert.AreEqual(start, damp(start, 0));
            Assert.AreEqual(target, damp(start, double.PositiveInfinity));

            double middle = Interpolation.Lerp(start, target, 0.5);
            Assert.AreEqual(middle, damp(start, half_time));

            // The final value shouldn't depend on the frame rate
            Assert.AreEqual(
                damp(start, 300 + 700),
                damp(damp(start, 300), 700));

            // The elapsed time can be negative.
            Assert.AreEqual(
                damp(start, 500 - 200),
                damp(damp(start, 500), -200));

            static double damp(double current, double elapsed) => Interpolation.DampContinuously(current, target, half_time, elapsed);
        }

        [Test]
        public void TestLagrange()
        {
            // lagrange of (0,0) (0.5,0.35) (1,1) is equal to 0.6x*x + 0.4x
            Vector2[] points = { new Vector2(0, 0), new Vector2(0.5f, 0.35f), new Vector2(1, 1) };

            for (double t = -10; t <= 10; t += 0.01)
                Assert.AreEqual(0.6 * t * t + 0.4 * t, Interpolation.Lagrange(points, t), 1e-4);

            Assert.Throws<ArgumentException>(() =>
            {
                Interpolation.Lagrange(null, 4);
            });

            Assert.Throws<ArgumentException>(() =>
            {
                ReadOnlySpan<Vector2> list = Array.Empty<Vector2>();
                Interpolation.Lagrange(list, 4);
            });

            Assert.AreEqual(Interpolation.Lagrange(new[] { new Vector2(3, 4) }, 12), 4);

            double[] weights = Interpolation.BarycentricWeights(points);
            Assert.AreEqual(3, weights.Length);
            Assert.AreEqual(2, weights[0]);
            Assert.AreEqual(-4, weights[1]);
            Assert.AreEqual(2, weights[2]);
        }

        [Test]
        public void TestGenericInterpolation()
        {
            // Implementations from Interpolation
            Assert.AreEqual(10, Interpolation.ValueAt<int>(0.1, 0, 100, 0, 1));
            Assert.IsTrue(Precision.AlmostEquals(0.01, Interpolation.ValueAt<double>(0.1, 0, 0.1, 0, 1)));

            // Implementations inside struct
            Assert.AreEqual(new MarginPadding(10), Interpolation.ValueAt(0.1, new MarginPadding(0), new MarginPadding(100), 0, 1));
            Assert.AreEqual(new TestClassWithValueAt(50), Interpolation.ValueAt(10, new TestClassWithValueAt(0), new TestClassWithValueAt(100), 0, 20));

            // Without implementations
            Assert.Throws<TypeInitializationException>(() => Interpolation.ValueAt(0, new TestClassWithoutValueAt(), new TestClassWithoutValueAt(), 0, 0));
        }

        private struct TestClassWithoutValueAt
        {
        }

        private readonly struct TestClassWithValueAt : IInterpolable<TestClassWithValueAt>
        {
            private readonly int i;

            public TestClassWithValueAt(int i)
            {
                this.i = i;
            }

            public bool Equals(TestClassWithValueAt other) => i == other.i;

            public TestClassWithValueAt ValueAt<TEasing>(double time, TestClassWithValueAt startValue, TestClassWithValueAt endValue, double startTime, double endTime, in TEasing easing)
                where TEasing : IEasingFunction
                => new TestClassWithValueAt(Interpolation.ValueAt(time, startValue.i, endValue.i, startTime, endTime, easing));

            public override string ToString() => $"{nameof(i)}: {i}";
        }
    }
}
