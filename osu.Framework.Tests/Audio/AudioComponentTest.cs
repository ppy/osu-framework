// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Threading.Tasks;
using NUnit.Framework;
using osu.Framework.Audio;
using osu.Framework.IO.Stores;
using osu.Framework.Platform;
using osu.Framework.Threading;

namespace osu.Framework.Tests.Audio
{
    [TestFixture]
    public class AudioComponentTest
    {
        [Test]
        public void TestVirtualTrack()
        {
            Architecture.SetIncludePath();

            var thread = new AudioThread();
            var store = new NamespacedResourceStore<byte[]>(new DllResourceStore(@"osu.Framework.dll"), @"Resources");

            var manager = new AudioManager(thread, store, store);

            thread.Start();

            var track = manager.Tracks.GetVirtual();

            Assert.IsFalse(track.IsRunning);
            Assert.AreEqual(0, track.CurrentTime);

            track.Start();

            Task.Delay(50);

            Assert.Greater(track.CurrentTime, 0);

            track.Stop();
            Assert.IsFalse(track.IsRunning);

            thread.Exit();

            Task.Delay(500);

            Assert.IsFalse(thread.Exited);
        }
    }
}
