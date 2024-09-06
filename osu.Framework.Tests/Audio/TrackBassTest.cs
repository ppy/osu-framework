// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.Threading;
using NUnit.Framework;
using osu.Framework.Audio;
using osu.Framework.Audio.Track;
using osu.Framework.Bindables;

#pragma warning disable 4014

namespace osu.Framework.Tests.Audio
{
    [TestFixture]
    public class TrackBassTest
    {
        private AudioTestComponents audio;
        private Track track;

        [TearDown]
        public void Teardown()
        {
            audio?.Dispose();
        }

        private void setupBackend(AudioTestComponents.Type id, bool loadTrack = false)
        {
            if (id == AudioTestComponents.Type.BASS)
            {
                audio = new BassTestComponents();
                track = audio.GetTrack();
            }
            else if (id == AudioTestComponents.Type.SDL3)
            {
                audio = new SDL3AudioTestComponents();
                track = audio.GetTrack();

                if (loadTrack)
                    ((SDL3AudioTestComponents)audio).WaitUntilTrackIsLoaded((TrackSDL3)track);
            }
            else
            {
                throw new InvalidOperationException("not a supported id");
            }

            audio.Update();
        }

        [TestCase(AudioTestComponents.Type.BASS)]
        [TestCase(AudioTestComponents.Type.SDL3)]
        public void TestStart(AudioTestComponents.Type id)
        {
            setupBackend(id, true);

            track.StartAsync();
            audio.Update();

            Thread.Sleep(50);

            audio.Update();

            Assert.IsTrue(track.IsRunning);
            Assert.Greater(track.CurrentTime, 0);
        }

        [TestCase(AudioTestComponents.Type.BASS)]
        [TestCase(AudioTestComponents.Type.SDL3)]
        public void TestStop(AudioTestComponents.Type id)
        {
            setupBackend(id);

            track.StartAsync();
            audio.Update();

            track.StopAsync();
            audio.Update();

            Assert.IsFalse(track.IsRunning);

            double expectedTime = track.CurrentTime;
            Thread.Sleep(50);

            Assert.AreEqual(expectedTime, track.CurrentTime);
        }

        [TestCase(AudioTestComponents.Type.BASS)]
        [TestCase(AudioTestComponents.Type.SDL3)]
        public void TestStopWhenDisposed(AudioTestComponents.Type id)
        {
            setupBackend(id);

            track.StartAsync();
            audio.Update();

            Thread.Sleep(50);
            audio.Update();

            Assert.IsTrue(track.IsAlive);
            Assert.IsTrue(track.IsRunning);

            track.Dispose();
            audio.Update();

            Assert.IsFalse(track.IsAlive);
            Assert.IsFalse(track.IsRunning);

            double expectedTime = track.CurrentTime;
            Thread.Sleep(50);

            Assert.AreEqual(expectedTime, track.CurrentTime);
        }

        [TestCase(AudioTestComponents.Type.BASS)]
        [TestCase(AudioTestComponents.Type.SDL3)]
        public void TestStopAtEnd(AudioTestComponents.Type id)
        {
            setupBackend(id, true);

            startPlaybackAt(track.Length - 1);

            Thread.Sleep(50);

            audio.Update();
            track.StopAsync();
            audio.Update();

            Assert.IsFalse(track.IsRunning);
            Assert.AreEqual(track.Length, track.CurrentTime);
        }

        [TestCase(AudioTestComponents.Type.BASS)]
        [TestCase(AudioTestComponents.Type.SDL3)]
        public void TestSeek(AudioTestComponents.Type id)
        {
            setupBackend(id, true);

            track.SeekAsync(1000);
            audio.Update();

            Assert.IsFalse(track.IsRunning);
            Assert.AreEqual(1000, track.CurrentTime);
        }

        [TestCase(AudioTestComponents.Type.BASS)]
        [TestCase(AudioTestComponents.Type.SDL3)]
        public void TestSeekWhileRunning(AudioTestComponents.Type id)
        {
            setupBackend(id);

            track.StartAsync();
            audio.Update();

            track.SeekAsync(1000);
            audio.Update();

            Thread.Sleep(50);
            audio.Update();

            Assert.IsTrue(track.IsRunning);
            Assert.GreaterOrEqual(track.CurrentTime, 1000);
        }

        /// <summary>
        /// Bass does not allow seeking to the end of the track. It should fail and the current time should not change.
        /// </summary>
        [TestCase(AudioTestComponents.Type.BASS)]
        public void TestSeekToEndFails(AudioTestComponents.Type id)
        {
            setupBackend(id);

            bool? success = null;

            audio.RunOnAudioThread(() => { success = track.Seek(track.Length); });
            audio.Update();

            Assert.AreEqual(0, track.CurrentTime);
            Assert.IsFalse(success);
        }

        [TestCase(AudioTestComponents.Type.BASS)]
        [TestCase(AudioTestComponents.Type.SDL3)]
        public void TestSeekBackToSamePosition(AudioTestComponents.Type id)
        {
            setupBackend(id, true);

            track.SeekAsync(1000);
            track.SeekAsync(0);
            audio.Update();

            Thread.Sleep(50);

            audio.Update();

            Assert.GreaterOrEqual(track.CurrentTime, 0);
            Assert.Less(track.CurrentTime, 1000);
        }

        [TestCase(AudioTestComponents.Type.BASS)]
        [TestCase(AudioTestComponents.Type.SDL3)]
        public void TestPlaybackToEnd(AudioTestComponents.Type id)
        {
            setupBackend(id, true);

            startPlaybackAt(track.Length - 1);

            Thread.Sleep(50);

            audio.Update();

            Assert.IsFalse(track.IsRunning);
            Assert.AreEqual(track.Length, track.CurrentTime);
        }

        /// <summary>
        /// Bass restarts the track from the beginning if Start is called when the track has been completed.
        /// This is blocked locally in <see cref="TrackBass"/>, so this test expects the track to not restart.
        /// </summary>
        [TestCase(AudioTestComponents.Type.BASS)]
        [TestCase(AudioTestComponents.Type.SDL3)]
        public void TestStartFromEndDoesNotRestart(AudioTestComponents.Type id)
        {
            setupBackend(id, true);

            startPlaybackAt(track.Length - 1);

            Thread.Sleep(50);

            audio.Update();
            track.StartAsync();
            audio.Update();

            Assert.AreEqual(track.Length, track.CurrentTime);
        }

        [TestCase(AudioTestComponents.Type.BASS)]
        [TestCase(AudioTestComponents.Type.SDL3)]
        public void TestRestart(AudioTestComponents.Type id)
        {
            setupBackend(id);

            startPlaybackAt(1000);

            Thread.Sleep(50);

            audio.Update();
            restartTrack();

            Assert.IsTrue(track.IsRunning);
            Assert.Less(track.CurrentTime, 1000);
        }

        [TestCase(AudioTestComponents.Type.BASS)]
        [TestCase(AudioTestComponents.Type.SDL3)]
        public void TestRestartAtEnd(AudioTestComponents.Type id)
        {
            setupBackend(id);

            startPlaybackAt(track.Length - 1);

            Thread.Sleep(50);

            audio.Update();
            restartTrack();

            Assert.IsTrue(track.IsRunning);
            Assert.LessOrEqual(track.CurrentTime, 1000);
        }

        [TestCase(AudioTestComponents.Type.BASS)]
        [TestCase(AudioTestComponents.Type.SDL3)]
        public void TestRestartFromRestartPoint(AudioTestComponents.Type id)
        {
            setupBackend(id);

            track.RestartPoint = 1000;

            startPlaybackAt(3000);
            restartTrack();

            Assert.IsTrue(track.IsRunning);
            Assert.GreaterOrEqual(track.CurrentTime, 1000);
            Assert.Less(track.CurrentTime, 3000);
        }

        [TestCase(AudioTestComponents.Type.BASS, 0)]
        [TestCase(AudioTestComponents.Type.SDL3, 0)]
        [TestCase(AudioTestComponents.Type.BASS, 1000)]
        [TestCase(AudioTestComponents.Type.SDL3, 1000)]
        public void TestLoopingRestart(AudioTestComponents.Type id, double restartPoint)
        {
            setupBackend(id, true);

            track.Looping = true;
            track.RestartPoint = restartPoint;

            startPlaybackAt(track.Length - 1);

            takeEffectsAndUpdateAfter(50);

            // In a perfect world the track will be running after the update above, but during testing it's possible that the track is in
            // a stalled state due to updates running on Bass' own thread, so we'll loop until the track starts running again
            // Todo: This should be fixed in the future if/when we invoke audio.Update() ourselves
            int loopCount = 0;

            while (++loopCount < 50 && !track.IsRunning)
            {
                audio.Update();
                Thread.Sleep(10);
            }

            if (loopCount == 50)
                throw new TimeoutException("Track failed to start in time.");

            Assert.GreaterOrEqual(track.CurrentTime, restartPoint);
            Assert.LessOrEqual(track.CurrentTime, restartPoint + 1000);
        }

        [TestCase(AudioTestComponents.Type.BASS)]
        [TestCase(AudioTestComponents.Type.SDL3)]
        public void TestSetTempoNegative(AudioTestComponents.Type id)
        {
            setupBackend(id);

            Assert.Throws<ArgumentException>(() => track.Tempo.Value = -1);
            Assert.Throws<ArgumentException>(() => track.Tempo.Value = 0.04f);

            Assert.IsFalse(track.IsReversed);

            track.Tempo.Value = 0.05f;

            Assert.IsFalse(track.IsReversed);
            Assert.AreEqual(0.05f, track.Tempo.Value);
        }

        [TestCase(AudioTestComponents.Type.BASS)]
        [TestCase(AudioTestComponents.Type.SDL3)]
        public void TestRateWithAggregateAdjustments(AudioTestComponents.Type id)
        {
            setupBackend(id);

            track.AddAdjustment(AdjustableProperty.Frequency, new BindableDouble(1.5f));
            Assert.AreEqual(1.5, track.Rate);
        }

        [TestCase(AudioTestComponents.Type.BASS)]
        [TestCase(AudioTestComponents.Type.SDL3)]
        public void TestLoopingTrackDoesntSetCompleted(AudioTestComponents.Type id)
        {
            setupBackend(id);

            bool completedEvent = false;

            track.Completed += () => completedEvent = true;
            track.Looping = true;
            startPlaybackAt(track.Length - 1);
            takeEffectsAndUpdateAfter(50);

            Assert.IsFalse(track.HasCompleted);
            Assert.IsFalse(completedEvent);

            audio.Update();

            Assert.IsTrue(track.IsRunning);
        }

        [TestCase(AudioTestComponents.Type.BASS)]
        [TestCase(AudioTestComponents.Type.SDL3)]
        public void TestHasCompletedResetsOnSeekBack(AudioTestComponents.Type id)
        {
            setupBackend(id, true);

            // start playback and wait for completion.
            startPlaybackAt(track.Length - 1);
            takeEffectsAndUpdateAfter(50);

            Assert.IsTrue(track.HasCompleted);

            // ensure seeking to end doesn't reset completed state.
            track.SeekAsync(track.Length);
            audio.Update();

            Assert.IsTrue(track.HasCompleted);

            // seeking back reset completed state.
            track.SeekAsync(track.Length - 1);
            audio.Update();

            Assert.IsFalse(track.HasCompleted);
        }

        [TestCase(AudioTestComponents.Type.BASS)]
        [TestCase(AudioTestComponents.Type.SDL3)]
        public void TestZeroFrequencyHandling(AudioTestComponents.Type id)
        {
            setupBackend(id, true);

            // start track.
            track.StartAsync();
            takeEffectsAndUpdateAfter(50);

            // ensure running and has progressed.
            Assert.IsTrue(track.IsRunning);
            Assert.Greater(track.CurrentTime, 0);

            // now set to zero frequency and update track to take effects.
            track.Frequency.Value = 0;
            audio.Update();

            double currentTime = track.CurrentTime;

            // assert time is frozen after 50ms sleep and didn't change with full precision, but "IsRunning" is still true.
            Thread.Sleep(50);
            audio.Update();

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
        [TestCase(AudioTestComponents.Type.BASS)]
        [TestCase(AudioTestComponents.Type.SDL3)]
        public void TestZeroFrequencyDoesntResumeTrack(AudioTestComponents.Type id)
        {
            setupBackend(id);

            // start at zero frequency and wait a bit.
            track.Frequency.Value = 0;
            track.StartAsync();
            takeEffectsAndUpdateAfter(50);

            // ensure started but not progressing.
            Assert.IsTrue(track.IsRunning);
            Assert.AreEqual(0, track.CurrentTime);

            // stop track and update.
            track.StopAsync();
            audio.Update();

            Assert.IsFalse(track.IsRunning);

            // set back to 1 frequency.
            track.Frequency.Value = 1;
            takeEffectsAndUpdateAfter(50);

            // assert track channel still paused regardless of frequency because it's stopped via Stop() above.
            Assert.IsFalse(track.IsRunning);
            Assert.AreEqual(0, track.CurrentTime);
        }

        [TestCase(AudioTestComponents.Type.BASS)]
        [TestCase(AudioTestComponents.Type.SDL3)]
        public void TestBitrate(AudioTestComponents.Type id)
        {
            setupBackend(id, true);

            Assert.Greater(track.Bitrate, 0);
        }

        /// <summary>
        /// Tests the case where a start call can be run inline due to already being on the audio thread.
        /// Because it's immediately executed, a `audio.Update()` call is not required before the channel's state is updated.
        /// </summary>
        [TestCase(AudioTestComponents.Type.BASS)]
        [TestCase(AudioTestComponents.Type.SDL3)]
        public void TestIsRunningUpdatedAfterInlineStart(AudioTestComponents.Type id)
        {
            setupBackend(id);

            audio.RunOnAudioThread(() => track.Start());
            Assert.That(track.IsRunning, Is.True);
        }

        /// <summary>
        /// Tests the case where a stop call can be run inline due to already being on the audio thread.
        /// Because it's immediately executed, a `audio.Update()` call is not required before the channel's state is updated.
        /// </summary>
        [TestCase(AudioTestComponents.Type.BASS)]
        [TestCase(AudioTestComponents.Type.SDL3)]
        public void TestIsRunningUpdatedAfterInlineStop(AudioTestComponents.Type id)
        {
            setupBackend(id);

            track.StartAsync();
            audio.Update();

            audio.RunOnAudioThread(() => track.Stop());
            Assert.That(track.IsRunning, Is.False);
        }

        /// <summary>
        /// Tests the case where a seek call can be run inline due to already being on the audio thread.
        /// Because it's immediately executed, a `audio.Update()` call is not required before the channel's state is updated.
        /// </summary>
        [TestCase(AudioTestComponents.Type.BASS)]
        [TestCase(AudioTestComponents.Type.SDL3)]
        public void TestCurrentTimeUpdatedAfterInlineSeek(AudioTestComponents.Type id)
        {
            setupBackend(id);

            track.StartAsync();
            audio.Update();

            audio.RunOnAudioThread(() => track.Seek(20000));
            Assert.That(track.CurrentTime, Is.EqualTo(20000).Within(100));
        }

        private void takeEffectsAndUpdateAfter(int after)
        {
            audio.Update();
            Thread.Sleep(after);
            audio.Update();
        }

        private void startPlaybackAt(double time)
        {
            track.SeekAsync(time);
            track.StartAsync();
            audio.Update();
        }

        private void restartTrack()
        {
            audio.RunOnAudioThread(() =>
            {
                track.Restart();
                audio.Update();
            });
        }
    }
}

#pragma warning restore 4014
