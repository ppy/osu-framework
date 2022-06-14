// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using NUnit.Framework;

namespace osu.Framework.Tests.Audio
{
    [TestFixture]
    public class DevicelessAudioTest : AudioThreadTest
    {
        public override void SetUp()
        {
            base.SetUp();

            // lose all devices
            Manager.SimulateDeviceLoss();
        }

        [Test]
        public void TestPlayTrackWithoutDevices()
        {
            var track = Manager.Tracks.Get("Tracks.sample-track.mp3");

            // start track
            track.Restart();

            WaitForOrAssert(() => track.IsRunning, "Track started", 1000);

            CheckTrackIsProgressing(track);

            // stop track
            track.Stop();

            WaitForOrAssert(() => !track.IsRunning, "Track did not stop", 1000);

            Assert.IsFalse(track.IsRunning);

            // seek track
            track.Seek(0);

            Assert.IsFalse(track.IsRunning);
            WaitForOrAssert(() => track.CurrentTime == 0, "Track did not seek correctly", 1000);
        }
    }
}
