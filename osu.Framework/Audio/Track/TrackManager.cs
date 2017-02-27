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

        private Track exclusiveTrack;
        private object exclusiveMutex = new object();

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

            Track last;
            lock (exclusiveMutex)
            {
                if (exclusiveTrack == track) return;
                last = exclusiveTrack;
                exclusiveTrack = track;
            }

            PendingActions.Enqueue(() =>
            {
                foreach (var item in Items)
                    if (!item.HasCompleted)
                        item.Stop();

                last?.Dispose();
                AddItem(track);
            });
        }

        public override void Update()
        {
            if (exclusiveTrack?.HasCompleted != false)
                lock (exclusiveMutex)
                    // We repeat the if-check inside the lock to make sure exclusiveTrack
                    // has not been overwritten prior to us taking the lock.
                    if (exclusiveTrack?.HasCompleted != false)
                        exclusiveTrack = Items.FirstOrDefault();

            base.Update();
        }
    }
}
