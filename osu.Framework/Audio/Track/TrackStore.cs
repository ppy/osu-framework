// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.IO;
using System.Threading.Tasks;
using osu.Framework.IO.Stores;

namespace osu.Framework.Audio.Track
{
    public class TrackStore : AudioCollectionManager<Track>, IResourceStore<Track>
    {
        private readonly IResourceStore<byte[]> store;

        public TrackStore(IResourceStore<byte[]> store)
        {
            this.store = store;
        }

        public Track Get(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;

            Track track = new TrackBass(store.GetStream(name));
            return track;
        }

        public Task<Track> GetAsync(string name) => Task.Run(() => Get(name));

        public Stream GetStream(string name) => store.GetStream(name);
    }
}
