// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System.Linq;
using osu.Framework.IO.Stores;
using System;

namespace osu.Framework.Audio.Track
{
    public class TrackManager : AudioCollectionManager<Track>
    {
        IResourceStore<byte[]> store;

        Track exclusiveTrack;

        public TrackManager(IResourceStore<byte[]> store)
        {
            this.store = store;
        }

        public Track Get(string name)
        {
            TrackBass track = new TrackBass(store.GetStream(name));
            AddItem(track);
            return track;
        }

        /// <summary>
        /// Specify an AudioTrack which should get exclusive playback over everything else.
        /// Will pause all other tracks and throw away any existing exclusive track.
        /// </summary>
        public void SetExclusive(Track track)
        {
            if (track == null)
                throw new ArgumentNullException(nameof(track));

            if (exclusiveTrack == track) return;

            foreach (var item in Items)
                item.Stop();

            exclusiveTrack?.Dispose();
            exclusiveTrack = track;

            AddItem(track);
        }

        public override void Update()
        {
            base.Update();

            if (exclusiveTrack?.HasCompleted != false)
                findExclusiveTrack();
        }

        private void findExclusiveTrack()
        {
            exclusiveTrack = Items.FirstOrDefault();
        }
    }
}
