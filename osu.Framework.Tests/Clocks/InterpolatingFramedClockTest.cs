// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Threading;
using NUnit.Framework;
using osu.Framework.Utils;
using osu.Framework.Timing;

namespace osu.Framework.Tests.Clocks
{
    [TestFixture]
    public class InterpolatingFramedClockTest
    {
        private TestClock source = null!;
        private InterpolatingFramedClock interpolating = null!;

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

            int interpolatedCount = 0;

            // test with test clock elapsing
            lastValue = interpolating.CurrentTime;

            for (int i = 0; i < 100; i++)
            {
                // we want to interpolate but not fall behind and fail interpolation too much
                source.CurrentTime += interpolating.AllowableErrorMilliseconds / 2 + 5;
                interpolating.ProcessFrame();

                Assert.GreaterOrEqual(interpolating.CurrentTime, lastValue, "Interpolating should not jump against rate.");
                Assert.LessOrEqual(Math.Abs(interpolating.CurrentTime - source.CurrentTime), interpolating.AllowableErrorMilliseconds, "Interpolating should be within allowance.");

                if (interpolating.IsInterpolating)
                    interpolatedCount++;

                Thread.Sleep((int)(interpolating.AllowableErrorMilliseconds / 2));
                lastValue = interpolating.CurrentTime;
            }

            Assert.Greater(interpolatedCount, 10);
        }

        [Test]
        public void SourceChangeTransfersValueAdjustable()
        {
            // For interpolating clocks, value transfer is always in the direction of the interpolating clock.

            const double first_source_time = 256000;
            const double second_source_time = 128000;

            source.Seek(first_source_time);

            var secondSource = new TestClock
            {
                // importantly, test a value lower than the original source.
                // this is to both test value transfer *and* the case where time is going backwards, as
                // some clocks have special provisions for this.
                CurrentTime = second_source_time
            };

            interpolating.ProcessFrame();
            Assert.That(interpolating.CurrentTime, Is.EqualTo(first_source_time));

            interpolating.ChangeSource(secondSource);
            interpolating.ProcessFrame();

            Assert.That(secondSource.CurrentTime, Is.EqualTo(second_source_time));
            Assert.That(interpolating.CurrentTime, Is.EqualTo(second_source_time));
        }

        [Test]
        public void SourceChangeTransfersValueNonAdjustable()
        {
            // For interpolating clocks, value transfer is always in the direction of the interpolating clock.

            const double first_source_time = 256000;
            const double second_source_time = 128000;

            source.Seek(first_source_time);

            var secondSource = new TestNonAdjustableClock
            {
                // importantly, test a value lower than the original source.
                // this is to both test value transfer *and* the case where time is going backwards, as
                // some clocks have special provisions for this.
                CurrentTime = second_source_time
            };

            interpolating.ProcessFrame();
            Assert.That(interpolating.CurrentTime, Is.EqualTo(first_source_time));

            interpolating.ChangeSource(secondSource);
            interpolating.ProcessFrame();

            Assert.That(secondSource.CurrentTime, Is.EqualTo(second_source_time));
            Assert.That(interpolating.CurrentTime, Is.EqualTo(second_source_time));
        }

        [Test]
        public void NeverInterpolatesBackwardsOnInterpolationFail()
        {
            const int sleep_time = 20;

            double lastValue = interpolating.CurrentTime;
            source.Start();
            int interpolatedCount = 0;

            for (int i = 0; i < 200; i++)
            {
                source.Rate += i * 10;

                if (i < 100) // stop the elapsing at some point in time. should still work as source's ElapsedTime is zero.
                    source.CurrentTime += sleep_time * source.Rate;

                interpolating.ProcessFrame();

                if (interpolating.IsInterpolating)
                    interpolatedCount++;

                Assert.GreaterOrEqual(interpolating.CurrentTime, lastValue, "Interpolating should not jump against rate.");
                Assert.LessOrEqual(Math.Abs(interpolating.CurrentTime - source.CurrentTime), interpolating.AllowableErrorMilliseconds * source.Rate, "Interpolating should be within allowance.");

                Thread.Sleep(sleep_time);
                lastValue = interpolating.CurrentTime;
            }

            Assert.Greater(interpolatedCount, 10);
        }

        // Regularly failing on single-thread macOS CI, failing when test asserts interpolating.IsInterpolating = false
        //
        // CanSeekForwardsOnInterpolationFail
        //     Expected: False
        //     But was:  True
        [Test]
        [FlakyTest]
        public void CanSeekForwardsOnInterpolationFail()
        {
            const int sleep_time = 20;

            double lastValue = interpolating.CurrentTime;
            source.Start();
            int interpolatedCount = 0;

            for (int i = 0; i < 200; i++)
            {
                source.Rate += i * 10;

                bool skipSourceForwards = i == 100;

                if (skipSourceForwards) // seek forward once at a random point.
                {
                    source.CurrentTime += interpolating.AllowableErrorMilliseconds * 10 * source.Rate;
                    interpolating.ProcessFrame();
                    Assert.That(interpolating.IsInterpolating, Is.False);
                    Assert.That(interpolating.CurrentTime, Is.EqualTo(source.CurrentTime));
                }
                else
                {
                    source.CurrentTime += sleep_time * source.Rate;
                    interpolating.ProcessFrame();
                }

                if (interpolating.IsInterpolating)
                    interpolatedCount++;

                Assert.GreaterOrEqual(interpolating.CurrentTime, lastValue, "Interpolating should not jump against rate.");
                Assert.LessOrEqual(Math.Abs(interpolating.CurrentTime - source.CurrentTime), interpolating.AllowableErrorMilliseconds * source.Rate, "Interpolating should be within allowance.");

                Thread.Sleep(sleep_time);
                lastValue = interpolating.CurrentTime;
            }

            Assert.Greater(interpolatedCount, 10);
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
        public void TestInterpolationAfterSourceStoppedThenSeeked()
        {
            // Just to make sure this works even when still in interpolation allowance.
            interpolating.AllowableErrorMilliseconds = 100000;

            source.Start();

            while (!interpolating.IsInterpolating)
            {
                source.CurrentTime += 10;
                Thread.Sleep(10);
                interpolating.ProcessFrame();
            }

            source.Stop();
            source.Seek(-10000);

            interpolating.ProcessFrame();
            Assert.That(interpolating.IsInterpolating, Is.False);
            Assert.That(interpolating.CurrentTime, Is.EqualTo(-10000).Within(100));
            Assert.That(interpolating.ElapsedFrameTime, Is.EqualTo(-10000).Within(100));

            source.Start();
            interpolating.ProcessFrame();
            Assert.That(interpolating.CurrentTime, Is.EqualTo(-10000).Within(100));
            Assert.That(interpolating.ElapsedFrameTime, Is.EqualTo(0).Within(100));
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

                // should be a nooop
                interpolating.ChangeSource(source);

                Assert.IsTrue(Precision.AlmostEquals(interpolating.CurrentTime, source.CurrentTime, interpolating.AllowableErrorMilliseconds),
                    "Interpolating should be within allowable error bounds.");

                Thread.Sleep(sleep_time);
            }

            source.Stop();
            interpolating.ProcessFrame();

            Assert.IsFalse(interpolating.IsRunning);
            Assert.That(source.CurrentTime, Is.EqualTo(interpolating.CurrentTime).Within(interpolating.AllowableErrorMilliseconds));
        }
    }
}
