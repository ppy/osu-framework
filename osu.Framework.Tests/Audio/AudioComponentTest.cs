// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using osu.Framework.Audio;
using osu.Framework.Audio.Track;
using osu.Framework.IO.Stores;
using osu.Framework.Platform;
using osu.Framework.Threading;

namespace osu.Framework.Tests.Audio
{
    [TestFixture]
    public class AudioComponentTest
    {
        private AudioThread thread;
        private NamespacedResourceStore<byte[]> store;
        private AudioManager manager;

        [SetUp]
        public void SetUp()
        {
            Architecture.SetIncludePath();

            thread = new AudioThread();
            store = new NamespacedResourceStore<byte[]>(new DllResourceStore(@"osu.Framework.dll"), @"Resources");

            manager = new AudioManager(thread, store, store);

            thread.Start();
        }

        [TearDown]
        public void TearDown()
        {
            Assert.IsFalse(thread.Exited);

            thread.Exit();

            Thread.Sleep(500);

            Assert.IsTrue(thread.Exited);
        }

        [Test]
        public void TestNestedStoreAdjustments()
        {
            var customStore = manager.GetSampleStore(new ResourceStore<byte[]>());

            checkAggregateVolume(manager.Samples, 1);
            checkAggregateVolume(customStore, 1);

            manager.Samples.Volume.Value = 0.5;

            waitAudioFrame();

            checkAggregateVolume(manager.Samples, 0.5);
            checkAggregateVolume(customStore, 0.5);

            customStore.Volume.Value = 0.5;

            waitAudioFrame();

            checkAggregateVolume(manager.Samples, 0.5);
            checkAggregateVolume(customStore, 0.25);
        }

        private void checkAggregateVolume(ISampleStore store, double expected)
        {
            Assert.AreEqual(expected, ((IAggregateAudioAdjustment)store).AggregateVolume.Value);
        }

        [Test]
        public void TestVirtualTrack()
        {
            var track = manager.Tracks.GetVirtual();

            waitAudioFrame();

            checkTrackCount(1);

            Assert.IsFalse(track.IsRunning);
            Assert.AreEqual(0, track.CurrentTime);

            track.Start();
            waitAudioFrame();

            Assert.Greater(track.CurrentTime, 0);

            track.Stop();
            Assert.IsFalse(track.IsRunning);

            track.Dispose();
            waitAudioFrame();

            checkTrackCount(0);
        }

        [Test]
        public void TestTrackVirtualSeekCurrent()
        {
            var trackVirtual = manager.Tracks.GetVirtual();
            trackVirtual.Start();

            waitAudioFrame();

            Assert.Greater(trackVirtual.CurrentTime, 0);

            trackVirtual.Tempo.Value = 2.0f;
            trackVirtual.Frequency.Value = 2.0f;

            waitAudioFrame();

            Assert.AreEqual(4.0f, trackVirtual.Rate);

            trackVirtual.Stop();
            var stoppedTime = trackVirtual.CurrentTime;
            Assert.Greater(stoppedTime, 0);

            trackVirtual.Seek(stoppedTime);

            Assert.AreEqual(stoppedTime, trackVirtual.CurrentTime);
        }

        private void checkTrackCount(int expected)
            => Assert.AreEqual(expected, ((TrackStore)manager.Tracks).Items.Count);

        /// <summary>
        /// Block for a specified number of audio thread frames.
        /// </summary>
        /// <param name="count">The number of frames to wait for. Two frames is generally considered safest.</param>
        private void waitAudioFrame(int count = 2)
        {
            var cts = new TaskCompletionSource<bool>();

            void runScheduled()
            {
                thread.Scheduler.Add(() =>
                {
                    if (count-- > 0)
                        runScheduled();
                    else
                    {
                        cts.SetResult(true);
                    }
                });
            }

            runScheduled();

            Task.WaitAll(cts.Task);
        }
    }
}
