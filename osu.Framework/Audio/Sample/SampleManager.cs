﻿// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System.Collections.Concurrent;
using System.IO;
using osu.Framework.IO.Stores;
using osu.Framework.Statistics;
using System.Linq;

namespace osu.Framework.Audio.Sample
{
    public class SampleManager : AudioCollectionManager<SampleChannel>, IResourceStore<SampleChannel>
    {
        readonly IResourceStore<byte[]> store;

        ConcurrentDictionary<string, Sample> sampleCache = new ConcurrentDictionary<string, Sample>();

        public SampleManager(IResourceStore<byte[]> store)
        {
            this.store = store;
        }

        public SampleChannel Get(string name)
        {
            lock (sampleCache)
            {
                Sample sample;
                SampleChannel channel = null;
                if (!sampleCache.TryGetValue(name, out sample))
                {
                    byte[] data = store.Get(name);
                    if (data != null)
                        sample = sampleCache[name] = new SampleBass(data, PendingActions);
                }

                if (sample != null)
                {
                    channel = new SampleChannelBass(sample, AddItemToList);
                    RegisterItem(channel);
                }

                return channel;
            }
        }

        public override void UpdateDevice(int deviceIndex)
        {
            foreach (var sample in sampleCache.Values.OfType<IBassAudio>())
                sample.UpdateDevice(deviceIndex);

            base.UpdateDevice(deviceIndex);
        }

        public override void Update()
        {
            FrameStatistics.Increment(StatisticsCounterType.Samples, sampleCache.Count);
            base.Update();
        }

        public Stream GetStream(string name)
        {
            return store.GetStream(name);
        }
    }
}
