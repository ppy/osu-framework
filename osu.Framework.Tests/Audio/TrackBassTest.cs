// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Threading;
using ManagedBass;
using NUnit.Framework;
using osu.Framework.Audio.Track;
using osu.Framework.IO.Stores;
using osu.Framework.Platform;
using osu.Framework.Threading;

#pragma warning disable 4014

namespace osu.Framework.Tests.Audio
{
    [TestFixture]
    public class TrackBassTest
    {
        private DllResourceStore resources;

        private TrackBass track;

        [SetUp]
        public void Setup()
        {
            Architecture.SetIncludePath();

            // Initialize bass with no audio to make sure the test remains consistent even if there is no audio device.
            Bass.Init(0);

            resources = new DllResourceStore("osu.Framework.Tests.dll");

            track = new TrackBass(resources.GetStream("Resources.Tracks.sample-track.mp3"));
            updateTrack();
        }

        [TearDown]
        public void Teardown()
        {
            Bass.Free();
        }

        [Test]
        public void TestStart()
        {
            track.StartAsync();
            updateTrack();

            Thread.Sleep(50);

            updateTrack();

            Assert.IsTrue(track.IsRunning);
            Assert.Greater(track.CurrentTime, 0);
        }

        [Test]
        public void TestStop()
        {
            track.StartAsync();
            track.StopAsync();
            updateTrack();

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

            updateTrack();
            track.StopAsync();
            updateTrack();

            Assert.IsFalse(track.IsRunning);
            Assert.AreEqual(track.Length, track.CurrentTime);
        }

        [Test]
        public void TestSeek()
        {
            track.SeekAsync(1000);
            updateTrack();

            Assert.IsFalse(track.IsRunning);
            Assert.AreEqual(1000, track.CurrentTime);
        }

        [Test]
        public void TestSeekWhileRunning()
        {
            track.StartAsync();
            track.SeekAsync(1000);
            updateTrack();

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

            runOnAudioThread(() => { success = track.Seek(track.Length); });
            updateTrack();

            Assert.AreEqual(0, track.CurrentTime);
            Assert.IsFalse(success);
        }

        [Test]
        public void TestSeekBackToSamePosition()
        {
            track.SeekAsync(1000);
            track.SeekAsync(0);
            updateTrack();

            Thread.Sleep(50);

            updateTrack();

            Assert.GreaterOrEqual(track.CurrentTime, 0);
            Assert.Less(track.CurrentTime, 1000);
        }

        [Test]
        public void TestPlaybackToEnd()
        {
            startPlaybackAt(track.Length - 1);

            Thread.Sleep(50);

            updateTrack();

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

            updateTrack();
            track.StartAsync();
            updateTrack();

            Assert.AreEqual(track.Length, track.CurrentTime);
        }

        [Test]
        public void TestRestart()
        {
            startPlaybackAt(1000);

            Thread.Sleep(50);

            updateTrack();
            restartTrack();

            Assert.IsTrue(track.IsRunning);
            Assert.Less(track.CurrentTime, 1000);
        }

        [Test]
        public void TestRestartAtEnd()
        {
            startPlaybackAt(track.Length - 1);

            Thread.Sleep(50);

            updateTrack();
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
            updateTrack();

            // The second update updates the IsRunning state
            updateTrack();

            // In a perfect world the track will be running after the update above, but during testing it's possible that the track is in
            // a stalled state due to updates running on Bass' own thread, so we'll loop until the track starts running again
            // Todo: This should be fixed in the future if/when we invoke Bass.Update() ourselves
            int loopCount = 0;

            while (++loopCount < 50 && !track.IsRunning)
            {
                updateTrack();
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
            updateTrack();
        }

        private void updateTrack() => runOnAudioThread(() => track.Update());

        private void restartTrack()
        {
            runOnAudioThread(() =>
            {
                track.Restart();
                track.Update();
            });
        }

        /// <summary>
        /// Certain actions are invoked on the audio thread.
        /// Here we simulate this process on a correctly named thread to avoid endless blocking.
        /// </summary>
        /// <param name="action">The action to perform.</param>
        private void runOnAudioThread(Action action)
        {
            var resetEvent = new ManualResetEvent(false);

            new Thread(() =>
            {
                action();

                resetEvent.Set();
            })
            {
                Name = GameThread.PrefixedThreadNameFor("Audio")
            }.Start();

            if (!resetEvent.WaitOne(TimeSpan.FromSeconds(10)))
                throw new TimeoutException();
        }
    }
}

#pragma warning restore 4014
