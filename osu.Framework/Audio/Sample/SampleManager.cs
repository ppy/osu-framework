// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using osu.Framework.IO.Stores;

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
                if (sampleCache.TryGetValue(name, out sample))
                {
                    //use existing bass sample (but provide a new audiosample instance).
                    channel = new SampleChannelBass(sample);
                }
                else
                {
                    byte[] data = store.Get(name);
                    if (data != null)
                    {
                        channel = new SampleChannelBass(sample = new SampleBass(data));
                        sampleCache[name] = sample;
                    }
                }

                if (channel != null)
                    AddItem(channel);
                return channel;
            }
        }

        public override void Update()
        {
            foreach (var s in sampleCache.Values)
                s.Update();

            base.Update();
        }

        public Stream GetStream(string name)
        {
            return store.GetStream(name);
        }
    }
}
