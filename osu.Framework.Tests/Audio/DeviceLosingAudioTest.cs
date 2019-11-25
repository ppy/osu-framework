// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Threading;
using NUnit.Framework;
using osu.Framework.IO.Stores;
using osu.Framework.Threading;

namespace osu.Framework.Tests.Audio
{
    /// <remarks>
    /// This unit will ALWAYS SKIP if the system does not have a physical audio device!!!
    /// A physical audio device is required to simulate the "loss" of it during playback.
    /// </remarks>
    [TestFixture]
    public class DeviceLosingAudioTest
    {
        private AudioThread thread;
        private NamespacedResourceStore<byte[]> store;
        private AudioManagerWithDeviceLoss manager;

        [SetUp]
        public void SetUp()
        {
            thread = new AudioThread();
            store = new NamespacedResourceStore<byte[]>(new DllResourceStore(@"osu.Framework.Tests.dll"), @"Resources");

            manager = new AudioManagerWithDeviceLoss(thread, store, store);

            thread.Start();

            // wait for any device to be initialized
            manager.WaitForDeviceChange(-1);

            // if the initialized device is "No sound", it indicates that no other physical devices are available, so this unit should be ignored
            if (manager.CurrentDevice == 0)
                Assert.Ignore("Physical audio devices are required for this unit.");

            // we don't want music playing in unit tests :)
            manager.Volume.Value = 0;
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
        public void TestPlaybackWithDeviceLoss() => testPlayback(manager.SimulateDeviceRestore, manager.SimulateDeviceLoss);

        [Test]
        public void TestPlaybackWithDeviceRestore() => testPlayback(manager.SimulateDeviceLoss, manager.SimulateDeviceRestore);

        private void testPlayback(Action preparation, Action simulate)
        {
            preparation();

            var track = manager.Tracks.Get("Tracks.sample-track.mp3");

            // start track
            track.Restart();

            Thread.Sleep(100);

            Assert.IsTrue(track.IsRunning);
            Assert.That(track.CurrentTime, Is.GreaterThan(0));

            var timeBeforeLosing = track.CurrentTime;

            // simulate change (loss/restore)
            simulate();

            Assert.IsTrue(track.IsRunning);

            Thread.Sleep(100);

            // playback should be continuing after device change
            Assert.IsTrue(track.IsRunning);
            Assert.That(track.CurrentTime, Is.GreaterThan(timeBeforeLosing));

            // stop track
            track.Stop();

            Thread.Sleep(100);

            Assert.IsFalse(track.IsRunning);

            // seek track
            track.Seek(0);

            Thread.Sleep(100);

            Assert.IsFalse(track.IsRunning);
            Assert.AreEqual(track.CurrentTime, 0);
        }
    }
}
