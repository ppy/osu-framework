// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Threading;
using ManagedBass;
using NUnit.Framework;
using osu.Framework.Audio.Track;
using osu.Framework.IO.Stores;

namespace osu.Framework.Tests.Platform
{
    [TestFixture]
    public class TrackBassTest
    {
        private readonly TrackBass track;

        public TrackBassTest()
        {
            // Initialize bass with no audio to make sure the test remains consistent even if there is no audio device.
            Bass.Init(0);

            var resources = new DllResourceStore("osu.Framework.Tests.dll");
            var fileStream = resources.GetStream("Resources.Tracks.sample-track.mp3");
            track = new TrackBass(fileStream);
        }

        [SetUp]
        public void Setup()
        {
#pragma warning disable 4014
            track.SeekAsync(1000);
            track.Update();
            Assert.That(track.CurrentTime == 1000);
        }

        /// <summary>
        /// Bass does not allow seeking to the end of the track. It should fail and the current time should not change.
        /// </summary>
        [Test]
        public void TestTrackSeekingToEndFails()
        {
            track.SeekAsync(track.Length);
            track.Update();
            Assert.That(track.CurrentTime == 1000);
        }

        /// <summary>
        /// Bass restarts the track from the beginning if Start is called when the track has been completed.
        /// This is blocked locally in <see cref="TrackBass"/>, so this test expects the track to not restart.
        /// </summary>
        [Test]
        public void TestTrackPlaybackBlocksAtTrackEnd()
        {
            track.SeekAsync(track.Length - 1);
#pragma warning restore 4014
            track.StartAsync();
            track.Update();
            Thread.Sleep(50);
            track.Update();
            Assert.That(!track.IsRunning && track.CurrentTime == track.Length);
            track.StartAsync();
            track.Update();
            Assert.That(track.CurrentTime == track.Length);
        }
    }
}
