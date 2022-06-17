// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.Diagnostics;
using System.Threading;
using NUnit.Framework;
using osu.Framework.Audio;
using osu.Framework.Audio.Track;
using osu.Framework.Bindables;
using osu.Framework.Development;
using osu.Framework.Threading;

namespace osu.Framework.Tests.Audio
{
    [TestFixture]
    public class TrackVirtualTest
    {
        private TrackVirtual track;

        [SetUp]
        public void Setup()
        {
            track = new TrackVirtual(60000);
            updateTrack();
        }

        [Test]
        public void TestStart()
        {
            track.Start();
            updateTrack();

            Thread.Sleep(50);

            updateTrack();

            Assert.IsTrue(track.IsRunning);
            Assert.Greater(track.CurrentTime, 0);
        }

        [Test]
        public void TestStartZeroLength()
        {
            // override default with custom length
            track = new TrackVirtual(0);

            track.Start();
            updateTrack();

            Thread.Sleep(50);

            Assert.IsTrue(!track.IsRunning);
            Assert.AreEqual(0, track.CurrentTime);
        }

        [Test]
        public void TestStop()
        {
            track.Start();
            track.Stop();
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
            track.Stop();
            updateTrack();

            Assert.IsFalse(track.IsRunning);
            Assert.AreEqual(track.Length, track.CurrentTime);
        }

        [Test]
        public void TestStopWhenDisposed()
        {
            startPlaybackAt(0);

            Thread.Sleep(50);
            updateTrack();

            Assert.IsTrue(track.IsAlive);
            Assert.IsTrue(track.IsRunning);

            track.Dispose();
            updateTrack();

            Assert.IsFalse(track.IsAlive);
            Assert.IsFalse(track.IsRunning);

            double expectedTime = track.CurrentTime;
            Thread.Sleep(50);

            Assert.AreEqual(expectedTime, track.CurrentTime);
        }

        [Test]
        public void TestSeek()
        {
            track.Seek(1000);
            updateTrack();

            Assert.IsFalse(track.IsRunning);
            Assert.AreEqual(1000, track.CurrentTime);
        }

        [Test]
        public void TestSeekWhileRunning()
        {
            track.Start();
            track.Seek(1000);
            updateTrack();

            Assert.IsTrue(track.IsRunning);
            Assert.GreaterOrEqual(track.CurrentTime, 1000);
        }

        [Test]
        public void TestSeekBackToSamePosition()
        {
            track.Seek(1000);
            track.Seek(0);
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
        /// This is blocked locally in <see cref="TrackVirtual"/>, so this test expects the track to not restart.
        /// </summary>
        [Test]
        public void TestStartFromEndDoesNotRestart()
        {
            startPlaybackAt(track.Length - 1);

            Thread.Sleep(50);

            updateTrack();
            track.Start();
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
            Assert.IsFalse(track.IsReversed);

            track.Tempo.Value = 0.05f;

            Assert.IsFalse(track.IsReversed);
            Assert.AreEqual(0.05f, track.Tempo.Value);
        }

        [Test]
        public void TestRateWithAggregateTempoAdjustments()
        {
            track.AddAdjustment(AdjustableProperty.Tempo, new BindableDouble(1.5f));
            Assert.AreEqual(1.5, track.Rate);

            testPlaybackRate(1.5);
        }

        [Test]
        public void TestRateWithAggregateFrequencyAdjustments()
        {
            track.AddAdjustment(AdjustableProperty.Frequency, new BindableDouble(1.5f));
            Assert.AreEqual(1.5, track.Rate);

            testPlaybackRate(1.5);
        }

        [Test]
        public void TestCurrentTimeUpdatedAfterInlineSeek()
        {
            track.Start();
            updateTrack();

            RunOnAudioThread(() => track.Seek(20000));
            Assert.That(track.CurrentTime, Is.EqualTo(20000).Within(100));
        }

        [Test]
        public void TestSeekToCurrentTime()
        {
            track.Seek(5000);

            bool seekSucceeded = false;
            RunOnAudioThread(() => seekSucceeded = track.Seek(track.CurrentTime));

            Assert.That(seekSucceeded, Is.True);
            Assert.That(track.CurrentTime, Is.EqualTo(5000));
        }

        [Test]
        public void TestSeekBeyondStartTime()
        {
            bool seekSucceeded = false;
            RunOnAudioThread(() => seekSucceeded = track.Seek(-1000));

            Assert.That(seekSucceeded, Is.False);
            Assert.That(track.CurrentTime, Is.EqualTo(0));
        }

        [Test]
        public void TestSeekBeyondEndTime()
        {
            bool seekSucceeded = false;
            RunOnAudioThread(() => seekSucceeded = track.Seek(track.Length + 1000));

            Assert.That(seekSucceeded, Is.False);
            Assert.That(track.CurrentTime, Is.EqualTo(track.Length));
        }

        private void testPlaybackRate(double expectedRate)
        {
            const double play_time = 1000;
            const double fudge = play_time * 0.1;

            track.Start();

            var sw = new Stopwatch();
            sw.Start();

            while (sw.ElapsedMilliseconds < play_time)
            {
                Thread.Sleep(50);
                track.Update();
            }

            sw.Stop();

            Assert.GreaterOrEqual(track.CurrentTime, sw.ElapsedMilliseconds * expectedRate - fudge);
            Assert.LessOrEqual(track.CurrentTime, sw.ElapsedMilliseconds * expectedRate + fudge);
        }

        private void startPlaybackAt(double time)
        {
            track.Seek(time);
            track.Start();
            updateTrack();
        }

        private void updateTrack() => RunOnAudioThread(() => track.Update());

        private void restartTrack()
        {
            RunOnAudioThread(() =>
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
        public static void RunOnAudioThread(Action action)
        {
            var resetEvent = new ManualResetEvent(false);

            new Thread(() =>
            {
                ThreadSafety.IsAudioThread = true;

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
