// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System.IO;
using osu.Framework.IO.Stores;

namespace osu.Framework.Audio.Track
{
    public class TrackManager : AudioCollectionManager<Track>
    {
        private readonly IResourceStore<byte[]> store;

        /// <summary>
        /// Constructs a new <see cref="Track"/> from provided audio data.
        /// </summary>
        /// <param name="data">The sample data stream.</param>
        /// <param name="quick">If true, the <see cref="Track"/> will not be fully loaded, and should only be used for preview purposes.  Defaults to false.</param>
        public virtual Track CreateTrack(Stream data, bool quick = false) => new TrackBass(data, quick);

        /// <summary>
        /// Constructs a new <see cref="Waveform"/> from provided audio data.
        /// </summary>
        /// <param name="data">The sample data stream. If null, an empty waveform is constructed.</param>
        public virtual Waveform CreateWaveform(Stream data) => new Waveform(data);

        public TrackManager(IResourceStore<byte[]> store)
        {
            this.store = store;
        }

        public Track Get(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;

            Track track = CreateTrack(store.GetStream(name));
            AddItem(track);
            return track;
        }
    }
}
