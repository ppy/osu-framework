// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using NUnit.Framework;
using osu.Framework.Audio.Sample;
using osu.Framework.Audio.Track;
using osu.Framework.IO.Stores;

namespace osu.Framework.Tests.Audio
{
    [TestFixture]
    public class AudioComponentTest : AudioThreadTest
    {
        [Test]
        public void TestNestedStoreAdjustments()
        {
            var customStore = Manager.GetSampleStore(new ResourceStore<byte[]>());

            checkAggregateVolume(Manager.Samples, 1);
            checkAggregateVolume(customStore, 1);

            Manager.Samples.Volume.Value = 0.5;

            WaitAudioFrame();

            checkAggregateVolume(Manager.Samples, 0.5);
            checkAggregateVolume(customStore, 0.5);

            customStore.Volume.Value = 0.5;

            WaitAudioFrame();

            checkAggregateVolume(Manager.Samples, 0.5);
            checkAggregateVolume(customStore, 0.25);
        }

        private void checkAggregateVolume(ISampleStore store, double expected)
        {
            Assert.AreEqual(expected, store.AggregateVolume.Value);
        }

        [Test]
        public void TestVirtualTrack()
        {
            var track = Manager.Tracks.GetVirtual();

            WaitAudioFrame();

            checkTrackCount(1);

            Assert.IsFalse(track.IsRunning);
            Assert.AreEqual(0, track.CurrentTime);

            track.Start();
            WaitAudioFrame();

            Assert.Greater(track.CurrentTime, 0);

            track.Stop();
            Assert.IsFalse(track.IsRunning);

            track.Dispose();
            WaitAudioFrame();

            checkTrackCount(0);
        }

        [Test]
        public void TestTrackVirtualSeekCurrent()
        {
            var trackVirtual = Manager.Tracks.GetVirtual();
            trackVirtual.Start();

            WaitAudioFrame();

            Assert.Greater(trackVirtual.CurrentTime, 0);

            trackVirtual.Tempo.Value = 2.0f;
            trackVirtual.Frequency.Value = 2.0f;

            WaitAudioFrame();

            Assert.AreEqual(4.0f, trackVirtual.Rate);

            trackVirtual.Stop();
            double stoppedTime = trackVirtual.CurrentTime;
            Assert.Greater(stoppedTime, 0);

            trackVirtual.Seek(stoppedTime);

            Assert.AreEqual(stoppedTime, trackVirtual.CurrentTime);
        }

        private void checkTrackCount(int expected)
            => Assert.AreEqual(expected, ((TrackStore)Manager.Tracks).Items.Count);
    }
}
