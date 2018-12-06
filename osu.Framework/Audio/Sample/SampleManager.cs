// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System.Collections.Concurrent;
using System.IO;
using osu.Framework.IO.Stores;
using osu.Framework.Statistics;
using System.Linq;
using System.Threading.Tasks;
using System;

namespace osu.Framework.Audio.Sample
{
    public class SampleManager : AudioCollectionManager<SampleChannel>, IResourceStore<SampleChannel>
    {
        private readonly IResourceStore<byte[]> store;

        private readonly ConcurrentDictionary<string, Sample> sampleCache = new ConcurrentDictionary<string, Sample>();

        /// <summary>
        /// How many instances of a single sample should be allowed to playback concurrently before stopping the longest playing.
        /// </summary>
        public int PlaybackConcurrency { get; set; } = Sample.DEFAULT_CONCURRENCY;

        public SampleManager(IResourceStore<byte[]> store)
        {
            this.store = store;
        }

        public SampleChannel Get(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;

            lock (sampleCache)
            {
                SampleChannel channel = null;
                if (!sampleCache.TryGetValue(name, out Sample sample))
                {
                    byte[] data = store.Get(name);
                    sample = sampleCache[name] = data == null ? null : CreateSample(data, PendingActions, PlaybackConcurrency);
                }

                if (sample != null)
                {
                    channel = CreateSampleChannel(sample, AddItemToList);
                    RegisterItem(channel);
                }

                return channel;
            }
        }

        public virtual Sample CreateSample(byte[] data, ConcurrentQueue<Task> customPendingActions, int concurrency) => new SampleBass(data, customPendingActions, concurrency);

        public virtual SampleChannel CreateSampleChannel(Sample sample, Action<SampleChannel> onPlay) => new SampleChannelBass(sample, onPlay);

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

        public Stream GetStream(string name)
        {
            return store.GetStream(name);
        }
    }
}
