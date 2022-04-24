// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Threading;
using NUnit.Framework;
using osu.Framework.Timing;

namespace osu.Framework.Tests.Clocks
{
    [TestFixture]
    public class StopwatchClockTest
    {
        [Test]
        public void TestResetTime()
        {
            var stopwatchClock = new StopwatchClock();
            stopwatchClock.Start();

            Thread.Sleep(1000);

            Assert.Greater(stopwatchClock.CurrentTime, 0);

            stopwatchClock.Stop();
            stopwatchClock.Reset();

            Assert.AreEqual(0, stopwatchClock.CurrentTime);
        }

        [Test]
        public void TestRateUpResetTime()
        {
            var stopwatchClock = new StopwatchClock();
            stopwatchClock.Start();

            Thread.Sleep(1000);

            stopwatchClock.Stop();
            double stoppedTime = stopwatchClock.CurrentTime;
            Assert.Greater(stoppedTime, 0);

            stopwatchClock.Rate = 2.0f;
            Assert.AreEqual(stoppedTime, stopwatchClock.CurrentTime);

            stopwatchClock.Reset();

            Assert.AreEqual(0, stopwatchClock.CurrentTime);
        }

        [Test]
        public void TestSeekWhileStopped()
        {
            var stopwatchClock = new StopwatchClock();
            stopwatchClock.Seek(5000);
            Assert.AreEqual(5000, stopwatchClock.CurrentTime);
        }

        [Test]
        public void TestSeekWhenNonZero()
        {
            var stopwatchClock = new StopwatchClock();
            stopwatchClock.Start();

            Thread.Sleep(1000);

            stopwatchClock.Stop();
            double stoppedTime = stopwatchClock.CurrentTime;
            Assert.Greater(stoppedTime, 0);

            stopwatchClock.Seek(stoppedTime);

            Assert.AreEqual(stoppedTime, stopwatchClock.CurrentTime);
        }

        [Test]
        public void TestSeekNegativeAdjustRate()
        {
            var stopwatchClock = new StopwatchClock();

            stopwatchClock.Seek(-5000);
            Assert.AreEqual(-5000, stopwatchClock.CurrentTime);

            stopwatchClock.Rate = 2.0f;
            stopwatchClock.Start();

            Thread.Sleep(1000);

            stopwatchClock.Stop();
            double stoppedTime = stopwatchClock.CurrentTime;
            Assert.Less(stoppedTime, 0);

            stopwatchClock.Seek(stoppedTime);

            Assert.AreEqual(stoppedTime, stopwatchClock.CurrentTime);
        }

        [Test]
        public void TestNegativeRate()
        {
            var stopwatchClock = new StopwatchClock { Rate = -2.0f };
            stopwatchClock.Start();

            Thread.Sleep(1000);

            stopwatchClock.Stop();

            Assert.Less(stopwatchClock.CurrentTime, 0);
        }
    }
}
