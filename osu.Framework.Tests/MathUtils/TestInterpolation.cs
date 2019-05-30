﻿// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Numerics;
using NUnit.Framework;
using osu.Framework.Graphics;
using osu.Framework.MathUtils;

namespace osu.Framework.Tests.MathUtils
{
    [TestFixture]
    public class TestInterpolation
    {
        [Test]
        public void TestLerp()
        {
            Assert.AreEqual(5, Interpolation.Lerp(0, 10, 0.5f));
            Assert.AreEqual(0, Interpolation.Lerp(0, 10, 0));
            Assert.AreEqual(10, Interpolation.Lerp(0, 10, 1));
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
                ReadOnlySpan<Vector2> list = new Vector2[0];
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
            Assert.AreEqual(10, Interpolation<int>.ValueAt(0.1, 0, 100, 0, 1));
            Assert.IsTrue(Precision.AlmostEquals(0.01, Interpolation<double>.ValueAt(0.1, 0, 0.1, 0, 1)));

            // Implementations inside struct
            Assert.AreEqual(new MarginPadding(10), Interpolation<MarginPadding>.ValueAt(0.1, new MarginPadding(0), new MarginPadding(100), 0, 1));
            Assert.AreEqual(new TestClassWithValueAt(50), Interpolation<TestClassWithValueAt>.ValueAt(10, new TestClassWithValueAt(0), new TestClassWithValueAt(100), 0, 20));

            // Without implementations
            Assert.Throws<TypeInitializationException>(() => Interpolation<TestClassWithoutValueAt>.ValueAt(0, new TestClassWithoutValueAt(), new TestClassWithoutValueAt(), 0, 0));
        }

        private struct TestClassWithoutValueAt
        {
        }

        private struct TestClassWithValueAt
        {
            private readonly int i;

            public TestClassWithValueAt(int i)
            {
                this.i = i;
            }

            public bool Equals(TestClassWithValueAt other) => i == other.i;

            public static TestClassWithValueAt ValueAt(double time, TestClassWithValueAt startValue, TestClassWithValueAt endValue, double startTime, double endTime, Easing easingType) => new TestClassWithValueAt(Interpolation.ValueAt(time, startValue.i, endValue.i, startTime, endTime, easingType));

            public override string ToString() => $"{nameof(i)}: {i}";
        }
    }
}
