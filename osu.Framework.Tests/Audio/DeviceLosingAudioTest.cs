// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using NUnit.Framework;

namespace osu.Framework.Tests.Audio
{
    /// <remarks>
    /// This unit will ALWAYS SKIP if the system does not have a physical audio device!!!
    /// A physical audio device is required to simulate the "loss" of it during playback.
    /// </remarks>
    [TestFixture]
    public class DeviceLosingAudioTest : AudioThreadTest
    {
        public override void SetUp()
        {
            base.SetUp();

            // wait for any device to be initialized
            Manager.WaitForDeviceChange(-1);

            // if the initialized device is "No sound", it indicates that no other physical devices are available, so this unit should be ignored
            if (Manager.CurrentDevice == 0)
                Assert.Ignore("Physical audio devices are required for this unit.");

            // we don't want music playing in unit tests :)
            Manager.Volume.Value = 0;
        }

        [Test]
        public void TestPlaybackWithDeviceLoss() => testPlayback(Manager.SimulateDeviceRestore, Manager.SimulateDeviceLoss);

        [Test]
        public void TestPlaybackWithDeviceRestore() => testPlayback(Manager.SimulateDeviceLoss, Manager.SimulateDeviceRestore);

        private void testPlayback(Action preparation, Action simulate)
        {
            preparation();

            var track = Manager.Tracks.Get("Tracks.sample-track.mp3");

            // start track
            track.Restart();

            WaitForOrAssert(() => track.IsRunning, "Track did not start running");

            WaitForOrAssert(() => track.CurrentTime > 0, "Track did not start running");

            // simulate change (loss/restore)
            simulate();

            CheckTrackIsProgressing(track);

            // stop track
            track.Stop();

            WaitForOrAssert(() => !track.IsRunning, "Track did not stop", 1000);

            // seek track
            track.Seek(0);

            Assert.IsFalse(track.IsRunning);
            WaitForOrAssert(() => track.CurrentTime == 0, "Track did not seek correctly", 1000);
        }
    }
}
