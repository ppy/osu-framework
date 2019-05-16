// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Threading;
using ManagedBass;
using NUnit.Framework;
using osu.Framework.Audio.Track;
using osu.Framework.IO.Stores;
using osu.Framework.Platform;

namespace osu.Framework.Tests.Platform
{
    [TestFixture]
    public class TrackBassTest
    {
        private readonly TrackBass track;

        public TrackBassTest()
        {
            Architecture.SetIncludePath();

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
#pragma warning restore 4014
            track.Update();
            Assert.AreEqual(1000, track.CurrentTime);
        }

        /// <summary>
        /// Bass does not allow seeking to the end of the track. It should fail and the current time should not change.
        /// </summary>
        [Test]
        public void TestTrackSeekingToEndFails()
        {
#pragma warning disable 4014
            track.SeekAsync(track.Length);
#pragma warning restore 4014
            track.Update();
            Assert.AreEqual(1000, track.CurrentTime);
        }

        /// <summary>
        /// Bass restarts the track from the beginning if Start is called when the track has been completed.
        /// This is blocked locally in <see cref="TrackBass"/>, so this test expects the track to not restart.
        /// </summary>
        [Test]
        public void TestTrackPlaybackBlocksAtTrackEnd()
        {
#pragma warning disable 4014
            track.SeekAsync(track.Length - 1);
#pragma warning restore 4014
            track.StartAsync();
            track.Update();
            Thread.Sleep(50);
            track.Update();
            Assert.IsFalse(track.IsRunning);
            Assert.AreEqual(track.Length, track.CurrentTime);
            track.StartAsync();
            track.Update();
            Assert.AreEqual(track.Length, track.CurrentTime);
        }
    }
}
