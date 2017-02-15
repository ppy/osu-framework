// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System.Linq;
using osu.Framework.IO.Stores;

namespace osu.Framework.Audio.Track
{
    public class TrackManager : AudioCollectionManager<AudioTrack>
    {
        IResourceStore<byte[]> store;

        AudioTrack exclusiveTrack;

        public TrackManager(IResourceStore<byte[]> store)
        {
            this.store = store;
        }

        public AudioTrack Get(string name)
        {
            AudioTrackBass track = new AudioTrackBass(store.GetStream(name));
            AddItem(track);
            return track;
        }

        /// <summary>
        /// Specify an AudioTrack which should get exclusive playback over everything else.
        /// Will pause all other tracks and throw away any existing exclusive track.
        /// </summary>
        public void SetExclusive(AudioTrack track)
        {
            if (exclusiveTrack == track) return;

            Items.ForEach(i => i.Stop());

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
