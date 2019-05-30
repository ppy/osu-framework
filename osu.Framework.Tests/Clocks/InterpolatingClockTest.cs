// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Threading;
using NUnit.Framework;
using osu.Framework.MathUtils;
using osu.Framework.Timing;

namespace osu.Framework.Tests.Clocks
{
    [TestFixture]
    public class InterpolatingClockTest
    {
        private TestClock source;
        private InterpolatingFramedClock interpolating;

        [SetUp]
        public void SetUp()
        {
            source = new TestClock();

            interpolating = new InterpolatingFramedClock();
            interpolating.ChangeSource(source);
        }

        [Test]
        public void NeverInterpolatesBackwards()
        {
            Assert.AreEqual(source.CurrentTime, interpolating.CurrentTime, "Interpolating should match source time.");
            source.Start();
            Assert.AreEqual(source.CurrentTime, interpolating.CurrentTime, "Interpolating should match source time.");
            interpolating.ProcessFrame();

            // test with test clock not elapsing
            double lastValue = interpolating.CurrentTime;

            for (int i = 0; i < 100; i++)
            {
                interpolating.ProcessFrame();
                Assert.GreaterOrEqual(interpolating.CurrentTime, lastValue, "Interpolating should not jump against rate.");
                Assert.GreaterOrEqual(interpolating.CurrentTime, source.CurrentTime, "Interpolating should not jump before source time.");

                Thread.Sleep((int)(interpolating.AllowableErrorMilliseconds / 2));
                lastValue = interpolating.CurrentTime;
            }

            int interpolatingCount = 0;

            // test with test clock elapsing
            lastValue = interpolating.CurrentTime;

            for (int i = 0; i < 100; i++)
            {
                source.CurrentTime += 50;
                interpolating.ProcessFrame();

                Assert.GreaterOrEqual(interpolating.CurrentTime, lastValue, "Interpolating should not jump against rate.");
                Assert.LessOrEqual(Math.Abs(interpolating.CurrentTime - source.CurrentTime), interpolating.AllowableErrorMilliseconds, "Interpolating should be within allowance.");

                if (interpolating.CurrentTime != source.CurrentTime)
                    interpolatingCount++;

                Thread.Sleep((int)(interpolating.AllowableErrorMilliseconds / 2));
                lastValue = interpolating.CurrentTime;
            }

            Assert.Greater(interpolatingCount, 10);
        }

        [Test]
        public void NeverInterpolatesBackwardsOnInterpolationFail()
        {
            double lastValue = interpolating.CurrentTime;

            source.Start();
            source.Rate = 10; // use a higher rate to ensure we may seek backwards.

            int sleepTime = (int)(interpolating.AllowableErrorMilliseconds / 2);

            int interpolatingCount = 0;

            for (int i = 0; i < 200; i++)
            {
                if (i < 100) // stop the elapsing at some point in time. should still work as source's ElapsedTime is zero.
                    source.CurrentTime += sleepTime * source.Rate;

                interpolating.ProcessFrame();

                if (interpolating.CurrentTime != source.CurrentTime)
                    interpolatingCount++;

                Assert.GreaterOrEqual(interpolating.CurrentTime, lastValue, "Interpolating should not jump against rate.");
                Assert.LessOrEqual(Math.Abs(interpolating.CurrentTime - source.CurrentTime), interpolating.AllowableErrorMilliseconds, "Interpolating should be within allowance.");

                Thread.Sleep(sleepTime);
                lastValue = interpolating.CurrentTime;
            }

            Assert.Greater(interpolatingCount, 10);
        }

        [Test]
        public void CanSeekBackwards()
        {
            Assert.AreEqual(source.CurrentTime, interpolating.CurrentTime, "Interpolating should match source time.");
            source.Start();

            Assert.AreEqual(source.CurrentTime, interpolating.CurrentTime, "Interpolating should match source time.");
            interpolating.ProcessFrame();

            source.Seek(10000);
            interpolating.ProcessFrame();
            Assert.AreEqual(source.CurrentTime, interpolating.CurrentTime, "Interpolating should match source time.");

            source.Seek(0);
            interpolating.ProcessFrame();
            Assert.AreEqual(source.CurrentTime, interpolating.CurrentTime, "Interpolating should match source time.");
        }

        [Test]
        public void InterpolationStaysWithinBounds()
        {
            source.Start();

            const int sleep_time = 20;

            for (int i = 0; i < 100; i++)
            {
                source.CurrentTime += sleep_time;
                interpolating.ProcessFrame();

                Assert.IsTrue(Precision.AlmostEquals(interpolating.CurrentTime, source.CurrentTime, interpolating.AllowableErrorMilliseconds),
                    "Interpolating should be within allowable error bounds.");

                Thread.Sleep(sleep_time);
            }

            source.Stop();
            interpolating.ProcessFrame();

            Assert.IsFalse(interpolating.IsRunning);
            Assert.AreEqual(source.CurrentTime, interpolating.CurrentTime, "Interpolating should match source time.");
        }
    }
}
