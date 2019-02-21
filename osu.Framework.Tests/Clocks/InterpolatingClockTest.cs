// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

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

            double lastValue = interpolating.CurrentTime;
            for (int i = 0; i < 100; i++)
            {
                interpolating.ProcessFrame();
                Assert.GreaterOrEqual(interpolating.CurrentTime, lastValue, "Interpolating should not jump against rate.");
                Assert.GreaterOrEqual(interpolating.CurrentTime, source.CurrentTime, "Interpolating should not jump before source time.");

                Thread.Sleep((int)(interpolating.AllowableErrorMilliseconds / 2));
            }
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

                Assert.IsTrue(Precision.AlmostEquals(interpolating.CurrentTime, source.CurrentTime, interpolating.AllowableErrorMilliseconds), "Interpolating should be within allowable error bounds.");

                Thread.Sleep(sleep_time);
            }

            source.Stop();
            interpolating.ProcessFrame();

            Assert.IsFalse(interpolating.IsRunning);
            Assert.AreEqual(source.CurrentTime, interpolating.CurrentTime, "Interpolating should match source time.");
        }
    }
}
