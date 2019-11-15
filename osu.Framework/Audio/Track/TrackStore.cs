// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using osu.Framework.Backends.Audio;
using osu.Framework.IO.Stores;

namespace osu.Framework.Audio.Track
{
    internal class TrackStore : AudioCollectionManager<AdjustableAudioComponent>, ITrackStore
    {
        private readonly IResourceStore<byte[]> store;
        private readonly IAudioBackend audioBackend;

        internal TrackStore(IResourceStore<byte[]> store, IAudioBackend audioBackend)
        {
            this.store = store;
            this.audioBackend = audioBackend;

            (store as ResourceStore<byte[]>)?.AddExtension(@"mp3");
        }

        public Track GetVirtual(double length = double.PositiveInfinity)
        {
            if (IsDisposed) throw new ObjectDisposedException($"Cannot retrieve items for an already disposed {nameof(TrackStore)}");

            var track = new TrackVirtual(length);
            AddItem(track);
            return track;
        }

        public Track Get(string name)
        {
            if (IsDisposed) throw new ObjectDisposedException($"Cannot retrieve items for an already disposed {nameof(TrackStore)}");

            if (string.IsNullOrEmpty(name)) return null;

            var dataStream = store.GetStream(name);

            if (dataStream == null)
                return null;

            Track track = audioBackend.CreateTrack(dataStream);
            AddItem(track);
            return track;
        }

        public Task<Track> GetAsync(string name) => Task.Run(() => Get(name));

        public Stream GetStream(string name) => store.GetStream(name);

        public IEnumerable<string> GetAvailableResources() => store.GetAvailableResources();
    }
}
