// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using osu.Framework.IO.Stores;
using osu.Framework.Statistics;
using System.Linq;
using System.Threading.Tasks;
using osu.Framework.Audio.Track;

namespace osu.Framework.Audio.Sample
{
    internal class SampleStore : AudioCollectionManager<AdjustableAudioComponent>, ISampleStore
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

        public SampleChannel Get(string name)
        {
            if (IsDisposed) throw new ObjectDisposedException($"Cannot retrieve items for an already disposed {nameof(SampleStore)}");

            if (string.IsNullOrEmpty(name)) return null;

            this.LogIfNonBackgroundThread(name);

            lock (sampleCache)
            {
                SampleChannel channel = null;

                if (!sampleCache.TryGetValue(name, out Sample sample))
                {
                    byte[] data = store.Get(name);
                    sample = sampleCache[name] = data == null ? null : new SampleBass(data, PendingActions, PlaybackConcurrency);
                }

                if (sample != null)
                {
                    channel = new SampleChannelBass(sample, AddItem);
                }

                return channel;
            }
        }

        public Task<SampleChannel> GetAsync(string name) => Task.Run(() => Get(name));

        public override void UpdateDevice(int deviceIndex)
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
