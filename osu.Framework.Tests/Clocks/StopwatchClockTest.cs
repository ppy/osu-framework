// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Threading;
using NUnit.Framework;
using osu.Framework.Audio.Track;
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

            stopwatchClock.Rate = 2.0f;
            stopwatchClock.Stop();
            stopwatchClock.Reset();

            Assert.AreEqual(0, stopwatchClock.CurrentTime);
        }

        [Test]
        public void TestSeekStopwatch()
        {
            var stopwatchClock = new StopwatchClock();
            stopwatchClock.Seek(5000);
            Assert.AreEqual(5000, stopwatchClock.CurrentTime);
        }

        [Test]
        public void TestSeekCurrent()
        {
            var stopwatchClock = new StopwatchClock();
            stopwatchClock.Start();

            Thread.Sleep(1000);

            stopwatchClock.Stop();
            var stoppedTime = stopwatchClock.CurrentTime;
            stopwatchClock.Seek(stopwatchClock.CurrentTime);

            Assert.AreEqual(stoppedTime, stopwatchClock.CurrentTime);
        }

        [Test]
        public void TestSeekNegativeAdjustRate()
        {
            var stopwatchClock = new StopwatchClock();
            stopwatchClock.Seek(-5000);
            stopwatchClock.Rate = 2.0f;
            stopwatchClock.Start();

            Thread.Sleep(1000);

            stopwatchClock.Stop();
            var stoppedTime = stopwatchClock.CurrentTime;
            stopwatchClock.Seek(stopwatchClock.CurrentTime);

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

        [Test]
        public void TestTrackVirtualSeekCurrent()
        {
            var trackVirtual = new TrackVirtual(60000);
            trackVirtual.Start();

            Thread.Sleep(1000);

            trackVirtual.Tempo.Value = 2.0f;
            trackVirtual.Frequency.Value = 2.0f;
            trackVirtual.OnStateChanged();

            trackVirtual.Stop();
            var stoppedTime = trackVirtual.CurrentTime;
            trackVirtual.Seek(trackVirtual.CurrentTime);

            Assert.AreEqual(stoppedTime, trackVirtual.CurrentTime);
        }
    }
}
