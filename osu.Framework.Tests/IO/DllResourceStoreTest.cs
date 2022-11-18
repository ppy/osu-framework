// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Threading.Tasks;
using NUnit.Framework;
using osu.Framework.IO.Stores;
using osu.Framework.Tests.Visual;

namespace osu.Framework.Tests.IO
{
    public class DllResourceStoreTest
    {
        [Test]
        public async Task TestSuccessfulAsyncLookup()
        {
            var resourceStore = new DllResourceStore(typeof(FrameworkTestScene).Assembly);

            byte[]? stream = await resourceStore.GetAsync("Resources.Tracks.sample-track.mp3").ConfigureAwait(false);
            Assert.That(stream, Is.Not.Null);
        }

        [Test]
        public async Task TestFailedAsyncLookup()
        {
            var resourceStore = new DllResourceStore(typeof(FrameworkTestScene).Assembly);

            byte[]? stream = await resourceStore.GetAsync("Resources.Tracks.sample-track.mp5").ConfigureAwait(false);
            Assert.That(stream, Is.Null);
        }
    }
}
