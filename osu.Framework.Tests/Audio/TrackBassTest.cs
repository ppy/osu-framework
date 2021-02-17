// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Threading;
using ManagedBass;
using NUnit.Framework;
using osu.Framework.Audio;
using osu.Framework.Audio.Track;
using osu.Framework.Bindables;
using osu.Framework.Development;
using osu.Framework.IO.Stores;
using osu.Framework.Platform.Linux.Native;
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
            if (RuntimeInfo.OS == RuntimeInfo.Platform.Linux)
            {
                // required for the time being to address libbass_fx.so load failures (see https://github.com/ppy/osu/issues/2852)
                Library.Load("libbass.so", Library.LoadFlags.RTLD_LAZY | Library.LoadFlags.RTLD_GLOBAL);
            }

            // Initialize bass with no audio to make sure the test remains consistent even if there is no audio device.
            Bass.Init(0);

            resources = new DllResourceStore(typeof(TrackBassTest).Assembly);

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
            updateTrack();

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
            updateTrack();

            track.SeekAsync(1000);
            updateTrack();

            Thread.Sleep(50);
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

        [TestCase(0)]
        [TestCase(1000)]
        public void TestLoopingRestart(double restartPoint)
        {
            track.Looping = true;
            track.RestartPoint = restartPoint;

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

            Assert.GreaterOrEqual(track.CurrentTime, restartPoint);
            Assert.LessOrEqual(track.CurrentTime, restartPoint + 1000);
        }

        [Test]
        public void TestSetTempoNegative()
        {
            Assert.Throws<ArgumentException>(() => track.Tempo.Value = -1);
            Assert.Throws<ArgumentException>(() => track.Tempo.Value = 0.04f);

            Assert.IsFalse(track.IsReversed);

            track.Tempo.Value = 0.05f;

            Assert.IsFalse(track.IsReversed);
            Assert.AreEqual(0.05f, track.Tempo.Value);
        }

        [Test]
        public void TestRateWithAggregateAdjustments()
        {
            track.AddAdjustment(AdjustableProperty.Frequency, new BindableDouble(1.5f));
            Assert.AreEqual(1.5, track.Rate);
        }

        [Test]
        public void TestLoopingTrackDoesntSetCompleted()
        {
            bool completedEvent = false;

            track.Completed += () => completedEvent = true;
            track.Looping = true;
            startPlaybackAt(track.Length - 1);
            takeEffectsAndUpdateAfter(50);

            Assert.IsFalse(track.HasCompleted);
            Assert.IsFalse(completedEvent);

            updateTrack();

            Assert.IsTrue(track.IsRunning);
        }

        [Test]
        public void TestHasCompletedResetsOnSeekBack()
        {
            // start playback and wait for completion.
            startPlaybackAt(track.Length - 1);
            takeEffectsAndUpdateAfter(50);

            Assert.IsTrue(track.HasCompleted);

            // ensure seeking to end doesn't reset completed state.
            track.SeekAsync(track.Length);
            updateTrack();

            Assert.IsTrue(track.HasCompleted);

            // seeking back reset completed state.
            track.SeekAsync(track.Length - 1);
            updateTrack();

            Assert.IsFalse(track.HasCompleted);
        }

        [Test]
        public void TestZeroFrequencyHandling()
        {
            // start track.
            track.StartAsync();
            takeEffectsAndUpdateAfter(50);

            // ensure running and has progressed.
            Assert.IsTrue(track.IsRunning);
            Assert.Greater(track.CurrentTime, 0);

            // now set to zero frequency and update track to take effects.
            track.Frequency.Value = 0;
            updateTrack();

            var currentTime = track.CurrentTime;

            // assert time is frozen after 50ms sleep and didn't change with full precision, but "IsRunning" is still true.
            Thread.Sleep(50);
            updateTrack();

            Assert.IsTrue(track.IsRunning);
            Assert.AreEqual(currentTime, track.CurrentTime);

            // set back to one and update track.
            track.Frequency.Value = 1;
            takeEffectsAndUpdateAfter(50);

            // ensure time didn't jump away, and is progressing normally.
            Assert.IsTrue(track.IsRunning);
            Assert.Greater(track.CurrentTime, currentTime);
            Assert.Less(track.CurrentTime, currentTime + 1000.0);
        }

        /// <summary>
        /// Ensure setting a paused (or not yet played) track's frequency from zero to one doesn't resume / play it.
        /// </summary>
        [Test]
        public void TestZeroFrequencyDoesntResumeTrack()
        {
            // start at zero frequency and wait a bit.
            track.Frequency.Value = 0;
            track.StartAsync();
            takeEffectsAndUpdateAfter(50);

            // ensure started but not progressing.
            Assert.IsTrue(track.IsRunning);
            Assert.AreEqual(0, track.CurrentTime);

            // stop track and update.
            track.StopAsync();
            updateTrack();

            Assert.IsFalse(track.IsRunning);

            // set back to 1 frequency.
            track.Frequency.Value = 1;
            takeEffectsAndUpdateAfter(50);

            // assert track channel still paused regardless of frequency because it's stopped via Stop() above.
            Assert.IsFalse(track.IsRunning);
            Assert.AreEqual(0, track.CurrentTime);
        }

        private void takeEffectsAndUpdateAfter(int after)
        {
            updateTrack();
            Thread.Sleep(after);
            updateTrack();
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

#pragma warning restore 4014
