// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.IO.Stores;

namespace osu.Framework.Audio.Track
{
    public class TrackManager : AudioCollectionManager<Track>
    {
        private readonly IResourceStore<byte[]> store;

        public TrackManager(IResourceStore<byte[]> store)
        {
            this.store = store;
        }

        public Track Get(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;

            Track track = new TrackBass(store.GetStream(name));
            AddItem(track);
            return track;
        }
    }
}
