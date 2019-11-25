// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Threading;
using NUnit.Framework;
using osu.Framework.IO.Stores;
using osu.Framework.Threading;

namespace osu.Framework.Tests.Audio
{
    [TestFixture]
    public class DevicelessAudioTest
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

            // lose all devices
            manager.SimulateDeviceLoss();
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
        public void TestPlayTrackWithoutDevices()
        {
            var track = manager.Tracks.Get("Tracks.sample-track.mp3");

            // start track
            track.Restart();

            Thread.Sleep(100);

            Assert.IsTrue(track.IsRunning);
            Assert.That(track.CurrentTime, Is.GreaterThan(0));

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
