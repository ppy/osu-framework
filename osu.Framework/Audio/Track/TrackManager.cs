// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System.IO;
using osu.Framework.IO.Stores;

namespace osu.Framework.Audio.Track
{
    public class TrackManager : AudioCollectionManager<Track>
    {
        private readonly IResourceStore<byte[]> store;

        protected virtual TrackBass CreateTrackBass(Stream data, bool quick) => new TrackBass(data, quick);

        public TrackManager(IResourceStore<byte[]> store)
        {
            this.store = store;
        }

        public Track Get(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;

            TrackBass track = CreateTrackBass(store.GetStream(name), false);
            AddItem(track);
            return track;
        }
    }
}
