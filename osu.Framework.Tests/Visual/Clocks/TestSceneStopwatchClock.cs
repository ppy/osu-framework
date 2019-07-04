// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using NUnit.Framework;
using osu.Framework.Audio.Track;
using osu.Framework.Timing;

namespace osu.Framework.Tests.Visual.Clocks
{
    public class TestSceneStopwatchClock : TestSceneClock
    {
        private TrackVirtual trackVirtual;
        private StopwatchClock stopwatchClock;

        [Test]
        public void TestStopwatchClock()
        {
            AddStep("Create Stopwatch Clock", () =>
            {
                AddClock(stopwatchClock = new StopwatchClock());
                stopwatchClock.Start();
            });
            AddWaitStep("Wait for time to pass", 5);
            AddStep("Adjust rate", () => stopwatchClock.Rate = 2.0f);
            AddStep("Stop and reset", () =>
            {
                stopwatchClock.Stop();
                stopwatchClock.Reset();
            });
            AddAssert("Current time is 0", () => stopwatchClock.CurrentTime == 0);
        }

        [Test]
        public void TestTrackVirtual()
        {
            double? stoppedTime = null;
            AddStep("Create TrackVirtual", () =>
            {
                AddClock(trackVirtual = new TrackVirtual(60000));
                trackVirtual.Start();
            });
            AddWaitStep("Wait for time to pass", 5);
            AddStep("Adjust rate", () =>
            {
                trackVirtual.Tempo.Value = 2.0f;
                trackVirtual.Frequency.Value = 2.0f;
                trackVirtual.OnStateChanged();
            });
            AddStep("Pause", () =>
            {
                trackVirtual.Stop();
                stoppedTime = trackVirtual.CurrentTime;
            });
            AddStep("Seek current time", () => trackVirtual.Seek(trackVirtual.CurrentTime));
            AddAssert("Current time has not changed", () => stoppedTime == trackVirtual.CurrentTime);
        }
    }
}
