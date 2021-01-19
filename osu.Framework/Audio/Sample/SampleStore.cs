// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using osu.Framework.IO.Stores;
using osu.Framework.Statistics;

namespace osu.Framework.Audio.Sample
{
    internal class SampleStore : AudioCollectionManager<AudioComponent>, ISampleStore
    {
        private readonly IResourceStore<byte[]> store;

        private readonly ConcurrentDictionary<string, Sample> sampleCache = new ConcurrentDictionary<string, Sample>();

        public int PlaybackConcurrency { get; set; } = Sample.DEFAULT_CONCURRENCY;

        internal SampleStore(IResourceStore<byte[]> store)
        {
            this.store = store;

            (store as ResourceStore<byte[]>)?.AddExtension(@"wav");
            (store as ResourceStore<byte[]>)?.AddExtension(@"mp3");
        }

        public Sample Get(string name)
        {
            if (IsDisposed) throw new ObjectDisposedException($"Cannot retrieve items for an already disposed {nameof(SampleStore)}");

            if (string.IsNullOrEmpty(name)) return null;

            lock (sampleCache)
            {
                if (sampleCache.TryGetValue(name, out Sample sample))
                    return sample;

                this.LogIfNonBackgroundThread(name);

                byte[] data = store.Get(name);

                sample = sampleCache[name] = data == null
                    ? null
                    : new SampleBass(data, PlaybackConcurrency) { AddChannel = AddItem };

                if (sample != null)
                    AddItem(sample);

                return sample;
            }
        }

        public Task<Sample> GetAsync(string name) => Task.Run(() => Get(name));

        internal override void UpdateDevice(int deviceIndex)
        {
            foreach (var sample in sampleCache.Values.OfType<IBassAudio>())
                sample.UpdateDevice(deviceIndex);

            base.UpdateDevice(deviceIndex);
        }

        protected override void UpdateState()
        {
            FrameStatistics.Add(StatisticsCounterType.Samples, sampleCache.Count);
            base.UpdateState();
        }

        public Stream GetStream(string name) => store.GetStream(name);

        public IEnumerable<string> GetAvailableResources() => store.GetAvailableResources();
    }
}
