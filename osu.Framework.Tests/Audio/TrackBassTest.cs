// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Threading;
using NUnit.Framework;
using osu.Framework.Audio.Track;

#pragma warning disable 4014

namespace osu.Framework.Tests.Audio
{
    public class TrackBassTest : BassTest
    {
        private TrackBass track;

        [SetUp]
        public override void Setup()
        {
            base.Setup();

            AudioComponent = track = Manager.GetTrackStore(Store).Get("Tracks.sample-track.mp3") as TrackBass;
            UpdateComponent();
        }

        [Test]
        public void TestStart()
        {
            track.StartAsync();
            UpdateComponent();

            Thread.Sleep(50);

            UpdateComponent();

            Assert.IsTrue(track.IsRunning);
            Assert.Greater(track.CurrentTime, 0);
        }

        [Test]
        public void TestStop()
        {
            track.StartAsync();
            track.StopAsync();
            UpdateComponent();

            Assert.IsFalse(track.IsRunning);

            double expectedTime = track.CurrentTime;
            Thread.Sleep(50);

            Assert.AreEqual(expectedTime, track.CurrentTime);
        }

        [Test]
        public void TestStopAtEnd()
        {
            startPlaybackAt(track.Length - 1);

            Thread.Sleep(50);

            UpdateComponent();
            track.StopAsync();
            UpdateComponent();

            Assert.IsFalse(track.IsRunning);
            Assert.AreEqual(track.Length, track.CurrentTime);
        }

        [Test]
        public void TestSeek()
        {
            track.SeekAsync(1000);
            UpdateComponent();

            Assert.IsFalse(track.IsRunning);
            Assert.AreEqual(1000, track.CurrentTime);
        }

        [Test]
        public void TestSeekWhileRunning()
        {
            track.StartAsync();
            track.SeekAsync(1000);
            UpdateComponent();

            Assert.IsTrue(track.IsRunning);
            Assert.GreaterOrEqual(track.CurrentTime, 1000);
        }

        /// <summary>
        /// Bass does not allow seeking to the end of the track. It should fail and the current time should not change.
        /// </summary>
        [Test]
        public void TestSeekToEndFails()
        {
            bool? success = null;

            RunOnAudioThread(() => { success = track.Seek(track.Length); });
            UpdateComponent();

            Assert.AreEqual(0, track.CurrentTime);
            Assert.IsFalse(success);
        }

        [Test]
        public void TestSeekBackToSamePosition()
        {
            track.SeekAsync(1000);
            track.SeekAsync(0);
            UpdateComponent();

            Thread.Sleep(50);

            UpdateComponent();

            Assert.GreaterOrEqual(track.CurrentTime, 0);
            Assert.Less(track.CurrentTime, 1000);
        }

        [Test]
        public void TestPlaybackToEnd()
        {
            startPlaybackAt(track.Length - 1);

            Thread.Sleep(50);

            UpdateComponent();

            Assert.IsFalse(track.IsRunning);
            Assert.AreEqual(track.Length, track.CurrentTime);
        }

        /// <summary>
        /// Bass restarts the track from the beginning if Start is called when the track has been completed.
        /// This is blocked locally in <see cref="TrackBass"/>, so this test expects the track to not restart.
        /// </summary>
        [Test]
        public void TestStartFromEndDoesNotRestart()
        {
            startPlaybackAt(track.Length - 1);

            Thread.Sleep(50);

            UpdateComponent();
            track.StartAsync();
            UpdateComponent();

            Assert.AreEqual(track.Length, track.CurrentTime);
        }

        [Test]
        public void TestRestart()
        {
            startPlaybackAt(1000);

            Thread.Sleep(50);

            UpdateComponent();
            restartTrack();

            Assert.IsTrue(track.IsRunning);
            Assert.Less(track.CurrentTime, 1000);
        }

        [Test]
        public void TestRestartAtEnd()
        {
            startPlaybackAt(track.Length - 1);

            Thread.Sleep(50);

            UpdateComponent();
            restartTrack();

            Assert.IsTrue(track.IsRunning);
            Assert.LessOrEqual(track.CurrentTime, 1000);
        }

        [Test]
        public void TestRestartFromRestartPoint()
        {
            track.RestartPoint = 1000;

            startPlaybackAt(3000);
            restartTrack();

            Assert.IsTrue(track.IsRunning);
            Assert.GreaterOrEqual(track.CurrentTime, 1000);
            Assert.Less(track.CurrentTime, 3000);
        }

        [Test]
        public void TestLoopingRestart()
        {
            track.Looping = true;

            startPlaybackAt(track.Length - 1);

            Thread.Sleep(50);

            // The first update brings the track to its end time and restarts it
            UpdateComponent();

            // The second update updates the IsRunning state
            UpdateComponent();

            // In a perfect world the track will be running after the update above, but during testing it's possible that the track is in
            // a stalled state due to updates running on Bass' own thread, so we'll loop until the track starts running again
            // Todo: This should be fixed in the future if/when we invoke Bass.Update() ourselves
            int loopCount = 0;

            while (++loopCount < 50 && !track.IsRunning)
            {
                UpdateComponent();
                Thread.Sleep(10);
            }

            if (loopCount == 50)
                throw new TimeoutException("Track failed to start in time.");

            Assert.LessOrEqual(track.CurrentTime, 1000);
        }

        [Test]
        public void TestSetTempoNegative()
        {
            Assert.Throws<ArgumentException>(() => track.TempoAdjust = -1);
            Assert.Throws<ArgumentException>(() => track.TempoAdjust = 0.04f);

            Assert.IsFalse(track.IsReversed);

            track.TempoAdjust = 0.05f;

            Assert.IsFalse(track.IsReversed);
            Assert.AreEqual(0.05f, track.Tempo.Value);
        }

        private void startPlaybackAt(double time)
        {
            track.SeekAsync(time);
            track.StartAsync();
            UpdateComponent();
        }

        private void restartTrack() => RunOnAudioThread(() =>
        {
            track.Restart();
            track.Update();
        });
    }
}

#pragma warning restore 4014
