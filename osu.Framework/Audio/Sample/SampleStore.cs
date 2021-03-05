// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using osu.Framework.IO.Stores;
using osu.Framework.Statistics;

namespace osu.Framework.Audio.Sample
{
    internal class SampleStore : AudioCollectionManager<AdjustableAudioComponent>, ISampleStore
    {
        private readonly IResourceStore<byte[]> store;

        private readonly ConcurrentDictionary<string, SampleBassFactory> factories = new ConcurrentDictionary<string, SampleBassFactory>();

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

            lock (factories)
            {
                if (!factories.TryGetValue(name, out SampleBassFactory factory))
                {
                    this.LogIfNonBackgroundThread(name);

                    byte[] data = store.Get(name);
                    factory = factories[name] = data == null ? null : new SampleBassFactory(data) { PlaybackConcurrency = { Value = PlaybackConcurrency } };

                    if (factory != null)
                        AddItem(factory);
                }

                return factory?.CreateSample();
            }
        }

        public Task<Sample> GetAsync(string name) => Task.Run(() => Get(name));

        protected override void UpdateState()
        {
            FrameStatistics.Add(StatisticsCounterType.Samples, factories.Count);
            base.UpdateState();
        }

        public Stream GetStream(string name) => store.GetStream(name);

        public IEnumerable<string> GetAvailableResources() => store.GetAvailableResources();
    }
}
